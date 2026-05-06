using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SpellVariantsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --spell-variants .\iw_workspace_vNext Temporalist");
            return;
        }

        var workspacePath = args[1];
        var className = args[2];

        var spellCatalog = LoadSpellCatalog(workspacePath);
        var classSpells = BuildClassSpellSet(workspacePath, className);

        Console.WriteLine("Spell variants");
        Console.WriteLine("--------------");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"Class:         {className}");
        Console.WriteLine($"Spell catalog: {spellCatalog.Count}");
        Console.WriteLine($"Class spells:  {classSpells.Count}");
        Console.WriteLine("");

        var variants = FindEnhancedVariants(spellCatalog);

        Console.WriteLine("Enhanced spell variants:");
        Console.WriteLine("");

        foreach (var variant in variants.OrderBy(x => x.BaseKey))
        {
            var baseAllowed = classSpells.Contains(variant.BaseKey, StringComparer.OrdinalIgnoreCase);
            var enhancedDirectAllowed = classSpells.Contains(variant.EnhancedKey, StringComparer.OrdinalIgnoreCase);
            var enhancedAllowedViaBase = baseAllowed;

            Console.WriteLine($"{variant.BaseName}");
            Console.WriteLine($"  Base key:              {variant.BaseKey}");
            Console.WriteLine($"  Enhanced key:          {variant.EnhancedKey}");
            Console.WriteLine($"  Enhanced name:         {variant.EnhancedName}");
            Console.WriteLine($"  Base exists:           {variant.BaseExists}");
            Console.WriteLine($"  Enhanced exists:       {variant.EnhancedExists}");
            Console.WriteLine($"  Base class allowed:    {baseAllowed}");
            Console.WriteLine($"  Enhanced direct class: {enhancedDirectAllowed}");
            Console.WriteLine($"  Enhanced via base:     {enhancedAllowedViaBase}");
            Console.WriteLine($"  Base level req:        {variant.BaseLevelReq}");
            Console.WriteLine($"  Enhanced level req:    {variant.EnhancedLevelReq}");
            Console.WriteLine("");
        }

        Console.WriteLine("Notes:");
        Console.WriteLine("  Enhanced variants are spells with keys starting with E where the base spell key also exists in Spells.bytes.");
        Console.WriteLine("  Class gating should use the base key.");
        Console.WriteLine("  Spell effect calculation should use the actual selected key.");
        Console.WriteLine("  Later: add unlock rules to decide whether enhanced variant should be suggested.");
    }

    private static List<SpellVariant> FindEnhancedVariants(
        Dictionary<string, SpellInfo> spells)
    {
        var results = new List<SpellVariant>();

        foreach (var spell in spells.Values)
        {
            if (string.IsNullOrWhiteSpace(spell.Key))
            {
                continue;
            }

            if (!spell.Key.StartsWith("E", StringComparison.OrdinalIgnoreCase) || spell.Key.Length <= 1)
            {
                continue;
            }

            var baseKey = spell.Key.Substring(1);

            if (!spells.TryGetValue(baseKey, out var baseSpell))
            {
                continue;
            }

            results.Add(
                new SpellVariant(
                    baseKey,
                    baseSpell.Name,
                    spell.Key,
                    spell.Name,
                    true,
                    true,
                    baseSpell.Requirements,
                    spell.Requirements
                )
            );
        }

        return results;
    }

    private static HashSet<string> BuildClassSpellSet(
        string workspacePath,
        string className)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);

        if (!Directory.Exists(sourceRoot))
        {
            return result;
        }

        var implementationClass = ResolveHeroImplementationClass(sourceRoot, className);
        var implementationFile = Directory
            .EnumerateFiles(sourceRoot, implementationClass + ".cs", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (implementationFile is null)
        {
            return result;
        }

        var text = File.ReadAllText(implementationFile);

        var listPattern = new Regex(
            @"SpellList\s*=\s*new\s+List\s*<\s*Spells\s*>\s*\{(?<body>[\s\S]*?)\};",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var spellRefPattern = new Regex(
            @"Spells\.([A-Za-z0-9_]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var match = listPattern.Match(text);

        if (!match.Success)
        {
            return result;
        }

        var body = match.Groups["body"].Value;

        foreach (Match spellMatch in spellRefPattern.Matches(body))
        {
            result.Add(spellMatch.Groups[1].Value);
        }

        return result;
    }

    private static string ResolveHeroImplementationClass(
        string sourceRoot,
        string className)
    {
        var pattern = new Regex(
            @"HeroesNames\." + Regex.Escape(className) + @"\s*,\s*new\s+([A-Za-z0-9_]+)\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var match = pattern.Match(text);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return className;
    }

    private static Dictionary<string, SpellInfo> LoadSpellCatalog(string workspacePath)
    {
        var spellsFile = WorkspacePaths.FindDataFile(workspacePath, "Spells");

        var result = new Dictionary<string, SpellInfo>(StringComparer.OrdinalIgnoreCase);

        if (spellsFile is null)
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(spellsFile));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var spell in doc.RootElement.EnumerateArray())
        {
            var info = new SpellInfo(
                Get(spell, "Name"),
                Get(spell, "Key"),
                Get(spell, "SpellType"),
                Get(spell, "TypeBehavior"),
                Get(spell, "Build"),
                Get(spell, "Duration"),
                Get(spell, "Requirements")
            );

            if (!string.IsNullOrWhiteSpace(info.Key))
            {
                result[info.Key] = info;
            }
        }

        return result;
    }

    private static string Get(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => value.GetRawText()
        };
    }

    private sealed record SpellInfo(
        string Name,
        string Key,
        string SpellType,
        string TypeBehavior,
        string Build,
        string Duration,
        string Requirements
    );

    private sealed record SpellVariant(
        string BaseKey,
        string BaseName,
        string EnhancedKey,
        string EnhancedName,
        bool BaseExists,
        bool EnhancedExists,
        string BaseLevelReq,
        string EnhancedLevelReq
    );
}

