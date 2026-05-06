using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class HeroImplementationDiagnosticsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --hero-impl-diagnostics .\iw_workspace_vNext Temporalist");
            return;
        }

        var workspacePath = args[1];
        var heroName = args[2];

        var sourceRoot = Path.Combine(
            workspacePath,
            "raw_files",
            "Scripts",
            "Assembly-CSharp"
        );

        if (!Directory.Exists(sourceRoot))
        {
            Console.WriteLine($"Missing source folder: {sourceRoot}");
            return;
        }

        var files = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .OrderBy(x => x)
            .ToList();

        var mappings = FindHeroImplementationMappings(sourceRoot, files);

        Console.WriteLine("Hero implementation diagnostics");
        Console.WriteLine("-------------------------------");
        Console.WriteLine($"Workspace:    {workspacePath}");
        Console.WriteLine($"Hero:         {heroName}");
        Console.WriteLine($"Source files: {files.Count}");
        Console.WriteLine("");

        Console.WriteLine("Hero -> implementation mappings found:");
        foreach (var pair in mappings.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {pair.Key} -> {pair.Value.ImplementationClass}");
            Console.WriteLine($"    Source: {pair.Value.SourceFile}");
        }

        Console.WriteLine("");

        if (!mappings.TryGetValue(heroName, out var mapping))
        {
            Console.WriteLine($"No implementation mapping found for hero: {heroName}");
            Console.WriteLine("");
            Console.WriteLine("Next action:");
            Console.WriteLine("  Inspect HeroChoosePanel.cs manually or run --class-source-diagnostics again.");
            return;
        }

        Console.WriteLine($"Resolved implementation: {heroName} -> {mapping.ImplementationClass}");
        Console.WriteLine("");

        var implFiles = FindFilesContainingClass(files, mapping.ImplementationClass);

        Console.WriteLine($"Files containing class {mapping.ImplementationClass}:");

        if (implFiles.Count == 0)
        {
            Console.WriteLine("  none");
            return;
        }

        foreach (var file in implFiles)
        {
            Console.WriteLine($"  {Path.GetRelativePath(sourceRoot, file).Replace("\\", "/")}");
        }

        Console.WriteLine("");

        foreach (var file in implFiles)
        {
            DiagnoseImplementationFile(sourceRoot, file, mapping.ImplementationClass);
        }

        Console.WriteLine("");
        Console.WriteLine("Notes:");
        Console.WriteLine("  Temporalist appears to resolve through HeroChoosePanel to an implementation class.");
        Console.WriteLine("  The next scanner should use this resolved implementation class instead of the display hero name.");
        Console.WriteLine("  If spell references are present but not SpellList.Add, we will map the real pattern next.");
    }

    private static Dictionary<string, HeroImplMapping> FindHeroImplementationMappings(
        string sourceRoot,
        IReadOnlyList<string> files)
    {
        var result = new Dictionary<string, HeroImplMapping>(StringComparer.OrdinalIgnoreCase);

        // Matches dictionary-style entries:
        // { HeroesNames.Temporalist, new ProdT2() }
        var pairPattern = new Regex(
            @"HeroesNames\.([A-Za-z0-9_]+)\s*,\s*new\s+([A-Za-z0-9_]+)\s*\(",
            RegexOptions.Compiled
        );

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(sourceRoot, file).Replace("\\", "/");

            foreach (Match match in pairPattern.Matches(text))
            {
                var hero = match.Groups[1].Value;
                var implementation = match.Groups[2].Value;

                if (!string.IsNullOrWhiteSpace(hero)
                    && !string.IsNullOrWhiteSpace(implementation))
                {
                    result[hero] = new HeroImplMapping(
                        hero,
                        implementation,
                        relative
                    );
                }
            }
        }

        return result;
    }

    private static List<string> FindFilesContainingClass(
        IReadOnlyList<string> files,
        string implementationClass)
    {
        var classPattern = new Regex(
            @"\bclass\s+" + Regex.Escape(implementationClass) + @"\b",
            RegexOptions.Compiled
        );

        return files
            .Where(file => classPattern.IsMatch(File.ReadAllText(file)))
            .OrderBy(file => file)
            .ToList();
    }

    private static void DiagnoseImplementationFile(
        string sourceRoot,
        string file,
        string implementationClass)
    {
        var relative = Path.GetRelativePath(sourceRoot, file).Replace("\\", "/");
        var text = File.ReadAllText(file);
        var lines = File.ReadAllLines(file);

        Console.WriteLine("============================================================");
        Console.WriteLine(relative);
        Console.WriteLine("============================================================");

        var spellRefs = Regex
            .Matches(text, @"Spells\.([A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        Console.WriteLine($"Spells.X references: {spellRefs.Count}");

        foreach (var spell in spellRefs)
        {
            Console.WriteLine($"  {spell}");
        }

        Console.WriteLine("");

        PrintContextForPattern(lines, relative, @"class\s+" + Regex.Escape(implementationClass), "class declaration");
        PrintContextForPattern(lines, relative, @"SpellList", "SpellList");
        PrintContextForPattern(lines, relative, @"new\s+List\s*<\s*Spells\s*>", "new List<Spells>");
        PrintContextForPattern(lines, relative, @"spells\s*=", "spells =");
        PrintContextForPattern(lines, relative, @"\.Add\s*\(\s*Spells\.", ".Add(Spells.)");
        PrintContextForPattern(lines, relative, @"Skills\.Add", "Skills.Add");
        PrintContextForPattern(lines, relative, @"NameKey", "NameKey");
    }

    private static void PrintContextForPattern(
        string[] lines,
        string relative,
        string regex,
        string title)
    {
        var pattern = new Regex(regex, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var found = false;

        for (var i = 0; i < lines.Length; i++)
        {
            if (!pattern.IsMatch(lines[i]))
            {
                continue;
            }

            if (!found)
            {
                Console.WriteLine($"Context: {title}");
                found = true;
            }

            Console.WriteLine($"----- {relative}:{i + 1} -----");

            var start = Math.Max(0, i - 8);
            var end = Math.Min(lines.Length - 1, i + 16);

            for (var j = start; j <= end; j++)
            {
                Console.WriteLine($"{j + 1,5}: {lines[j]}");
            }

            Console.WriteLine("");
        }
    }

    private sealed record HeroImplMapping(
        string HeroName,
        string ImplementationClass,
        string SourceFile
    );
}
