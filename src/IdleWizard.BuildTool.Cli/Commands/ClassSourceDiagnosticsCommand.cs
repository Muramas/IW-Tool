using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ClassSourceDiagnosticsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --class-source-diagnostics .\iw_workspace_vNext Temporalist");
            return;
        }

        var workspacePath = args[1];
        var className = args[2];

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

        var classTextPattern = new Regex(
            Regex.Escape(className),
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var heroEnumPattern = new Regex(
            @"HeroesNames\." + Regex.Escape(className) + @"\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var spellListPattern = new Regex(
            @"SpellList",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var spellAddPattern = new Regex(
            @"SpellList\s*\.\s*Add\s*\(\s*Spells\.([A-Za-z0-9_]+)\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var anySpellAddPattern = new Regex(
            @"\.Add\s*\(\s*Spells\.([A-Za-z0-9_]+)\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var classFiles = new List<string>();
        var heroEnumFiles = new List<string>();
        var spellListFiles = new List<string>();
        var bothClassAndSpellListFiles = new List<string>();
        var filesWithSpellAdds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var filesWithAnySpellAdds = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var relative = Path.GetRelativePath(sourceRoot, file).Replace("\\", "/");

            var hasClassText = classTextPattern.IsMatch(text);
            var hasHeroEnum = heroEnumPattern.IsMatch(text);
            var hasSpellList = spellListPattern.IsMatch(text);

            if (hasClassText)
            {
                classFiles.Add(relative);
            }

            if (hasHeroEnum)
            {
                heroEnumFiles.Add(relative);
            }

            if (hasSpellList)
            {
                spellListFiles.Add(relative);
            }

            if (hasClassText && hasSpellList)
            {
                bothClassAndSpellListFiles.Add(relative);
            }

            var spellAdds = spellAddPattern
                .Matches(text)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (spellAdds.Count > 0)
            {
                filesWithSpellAdds[relative] = spellAdds;
            }

            var anyAdds = anySpellAddPattern
                .Matches(text)
                .Select(m => m.Groups[1].Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            if (anyAdds.Count > 0)
            {
                filesWithAnySpellAdds[relative] = anyAdds;
            }
        }

        Console.WriteLine("Class source diagnostics");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"Class:         {className}");
        Console.WriteLine($"Source files:  {files.Count}");
        Console.WriteLine("");

        PrintList("Files containing class text", classFiles);
        PrintList("Files containing HeroesNames.<class>", heroEnumFiles);
        PrintList("Files containing SpellList", spellListFiles);
        PrintList("Files containing both class text and SpellList", bothClassAndSpellListFiles);

        Console.WriteLine("");
        Console.WriteLine("Files with SpellList.Add(Spells.X):");

        if (filesWithSpellAdds.Count == 0)
        {
            Console.WriteLine("  none");
        }
        else
        {
            foreach (var pair in filesWithSpellAdds.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  {pair.Key}");
                foreach (var spell in pair.Value)
                {
                    Console.WriteLine($"    {spell}");
                }
            }
        }

        Console.WriteLine("");
        Console.WriteLine("Files with any .Add(Spells.X):");

        if (filesWithAnySpellAdds.Count == 0)
        {
            Console.WriteLine("  none");
        }
        else
        {
            foreach (var pair in filesWithAnySpellAdds.OrderBy(x => x.Key).Take(80))
            {
                Console.WriteLine($"  {pair.Key}");
                foreach (var spell in pair.Value.Take(80))
                {
                    Console.WriteLine($"    {spell}");
                }
            }

            if (filesWithAnySpellAdds.Count > 80)
            {
                Console.WriteLine($"  ... more files omitted: {filesWithAnySpellAdds.Count - 80}");
            }
        }

        Console.WriteLine("");
        Console.WriteLine("Context around class references:");
        Console.WriteLine("");

        foreach (var relative in classFiles.OrderBy(x => x))
        {
            var file = Path.Combine(sourceRoot, relative.Replace("/", "\\"));
            PrintContext(file, relative, className);
        }

        Console.WriteLine("");
        Console.WriteLine("Notes:");
        Console.WriteLine("  We are looking for the real hero/class construction pattern.");
        Console.WriteLine("  If Temporalist does not appear near SpellList.Add, class spells may be data-driven, inherited, generated, or assigned through another factory.");
    }

    private static void PrintList(string title, IReadOnlyList<string> values)
    {
        Console.WriteLine(title + ":");

        if (values.Count == 0)
        {
            Console.WriteLine("  none");
            Console.WriteLine("");
            return;
        }

        foreach (var value in values.OrderBy(x => x))
        {
            Console.WriteLine($"  {value}");
        }

        Console.WriteLine("");
    }

    private static void PrintContext(string file, string relative, string pattern)
    {
        var lines = File.ReadAllLines(file);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Console.WriteLine($"===== {relative}:{i + 1} =====");

            var start = Math.Max(0, i - 8);
            var end = Math.Min(lines.Length - 1, i + 14);

            for (var j = start; j <= end; j++)
            {
                Console.WriteLine($"{j + 1,5}: {lines[j]}");
            }

            Console.WriteLine("");
        }
    }
}
