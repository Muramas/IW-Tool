using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ClassSpellMapCommand
{
    public static void RunClassSpellMap(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --class-spell-map .\iw_workspace_vNext Temporalist");
            return;
        }

        var workspacePath = args[1];
        var className = args[2];

        var spellCatalog = LoadSpellCatalog(workspacePath);
        var map = BuildClassSpellMap(workspacePath, className, spellCatalog);

        Console.WriteLine("Class spell map");
        Console.WriteLine("---------------");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Class:     {className}");
        Console.WriteLine($"Sources:   {map.SourceFiles.Count}");
        Console.WriteLine($"Spells:    {map.Spells.Count}");
        Console.WriteLine("");

        if (map.SourceFiles.Count > 0)
        {
            Console.WriteLine("Source files:");
            foreach (var source in map.SourceFiles.OrderBy(x => x))
            {
                Console.WriteLine($"  {source}");
            }

            Console.WriteLine("");
        }

        if (map.Spells.Count == 0)
        {
            Console.WriteLine("No base class SpellList.Add entries found.");
            Console.WriteLine("");
            Console.WriteLine("This may mean:");
            Console.WriteLine("  - class spell list is built in a different pattern");
            Console.WriteLine("  - the class name maps to another implementation class");
            Console.WriteLine("  - the snippets/source need a wider scan");
            return;
        }

        Console.WriteLine("Base class spells:");

        foreach (var spellKey in map.Spells.OrderBy(x => x))
        {
            spellCatalog.TryGetValue(spellKey, out var spell);

            Console.WriteLine($"  {spellKey}");

            if (spell is not null)
            {
                Console.WriteLine($"    Name:         {spell.Name}");
                Console.WriteLine($"    Type:         {spell.SpellType}");
                Console.WriteLine($"    Behavior:     {spell.TypeBehavior}");
                Console.WriteLine($"    Level req:    {spell.Requirements}");
            }
        }

        Console.WriteLine("");
        Console.WriteLine("Notes:");
        Console.WriteLine("  This is a source scan of base class SpellList.Add entries.");
        Console.WriteLine("  Challenge-added spells are not treated as normal class spells.");
        Console.WriteLine("  Requirements is treated as character/class level requirement from Spells.bytes.");
    }

    public static void RunValidatePhaseSpells(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --validate-phase-spells .\scenario_head_hero.json");
            return;
        }

        var scenarioPath = Path.GetFullPath(args[1]);

        if (!File.Exists(scenarioPath))
        {
            Console.WriteLine($"Scenario file not found: {scenarioPath}");
            return;
        }

        using var scenarioDoc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var scenario = scenarioDoc.RootElement;

        var workspace = GetRequiredString(scenario, "workspace");
        workspace = ResolvePathRelativeToFile(scenarioPath, workspace);

        var globalClass = "";

        if (scenario.TryGetProperty("globalContext", out var global)
            && global.ValueKind == JsonValueKind.Object)
        {
            globalClass = Get(global, "class");
        }

        var characterLevel = 0;

        if (scenario.TryGetProperty("globalContext", out var global2)
            && global2.ValueKind == JsonValueKind.Object)
        {
            int.TryParse(Get(global2, "characterLevel"), out characterLevel);
        }

        var spellCatalog = LoadSpellCatalog(workspace);
        var spellNameIndex = BuildSpellNameIndex(spellCatalog);

        Console.WriteLine("Validate phase spells");
        Console.WriteLine("---------------------");
        Console.WriteLine($"Scenario:        {scenarioPath}");
        Console.WriteLine($"Workspace:       {workspace}");
        Console.WriteLine($"Global class:    {(string.IsNullOrWhiteSpace(globalClass) ? "(not set)" : globalClass)}");
        Console.WriteLine($"Character level: {characterLevel}");
        Console.WriteLine($"Spell catalog:   {spellCatalog.Count}");
        Console.WriteLine("");

        if (!scenario.TryGetProperty("phases", out var phases)
            || phases.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Scenario does not contain phases[].");
            return;
        }

        var totalWarnings = 0;
        var phaseValidationExports = new List<PhaseValidationExport>();

        foreach (var phase in phases.EnumerateArray())
        {
            var phaseName = Get(phase, "name");
            var phaseClass = Get(phase, "class");

            if (string.IsNullOrWhiteSpace(phaseClass))
            {
                phaseClass = globalClass;
            }

            var classMap = string.IsNullOrWhiteSpace(phaseClass)
                ? ClassSpellMap.Empty(phaseClass)
                : BuildClassSpellMap(workspace, phaseClass, spellCatalog);

            var spellNames = ReadStringArray(phase, "spells");
            var spellValidationEntries = new List<SpellValidationEntryExport>();

            Console.WriteLine($"Phase: {phaseName}");
            Console.WriteLine($"Class: {(string.IsNullOrWhiteSpace(phaseClass) ? "(not set)" : phaseClass)}");
            Console.WriteLine($"Mapped base class spells: {classMap.Spells.Count}");
            Console.WriteLine("");

            foreach (var configuredSpell in spellNames)
            {
                if (!spellNameIndex.TryGetValue(configuredSpell, out var spell))
                {
                    Console.WriteLine($"  MISSING: {configuredSpell}");
                    Console.WriteLine("    Warning: Spell not found in Spells.bytes by Name or Key.");
                    totalWarnings++;

                    spellValidationEntries.Add(
                        new SpellValidationEntryExport(
                            configuredSpell,
                            false,
                            "",
                            "",
                            "",
                            "",
                            false,
                            "",
                            "",
                            0,
                            false,
                            null,
                            "Spell not found in Spells.bytes by Name or Key."
                        )
                    );

                    continue;
                }

                var allowedByClass =
                    classMap.Spells.Count == 0
                        ? (bool?)null
                        : IsSpellAllowedByClassMapIncludingEnhanced(classMap.Spells, spell.Key);

                var levelReq = 0;
                int.TryParse(spell.Requirements, out levelReq);

                var levelOk = characterLevel <= 0 || characterLevel >= levelReq;

                Console.WriteLine($"  {spell.Name}");
                var isEnhanced = IsEnhancedSpellVariant(spell.Key, spellCatalog);
                var baseSpellKey = GetBaseSpellKeyForVariant(spell.Key, spellCatalog);

                Console.WriteLine($"    Key:              {spell.Key}");
                Console.WriteLine($"    Variant:          {(isEnhanced ? "Enhanced" : "Base")}");
                Console.WriteLine($"    Base key:         {baseSpellKey}");
                Console.WriteLine($"    Enhanced selected:{isEnhanced}");
                Console.WriteLine($"    Type:             {spell.SpellType}");
                Console.WriteLine($"    Behavior:         {spell.TypeBehavior}");
                Console.WriteLine($"    Level req:        {levelReq}");
                Console.WriteLine($"    Level satisfied:  {levelOk}");

                if (allowedByClass is null)
                {
                    Console.WriteLine("    Class allowed:    unknown - no class map found");
                    totalWarnings++;
                }
                else
                {
                    Console.WriteLine($"    Class allowed:    {allowedByClass}");

                    if (!allowedByClass.Value)
                    {
                        totalWarnings++;
                    }
                }

                if (!levelOk)
                {
                    totalWarnings++;
                }

                var warning = "";

                if (allowedByClass is null)
                {
                    warning = "Class spell map not found.";
                }
                else if (!allowedByClass.Value)
                {
                    warning = "Spell is not allowed by the source-scanned base class spell list.";
                }
                else if (!levelOk)
                {
                    warning = "Character level does not satisfy spell level requirement.";
                }

                spellValidationEntries.Add(
                    new SpellValidationEntryExport(
                        configuredSpell,
                        true,
                        spell.Name,
                        spell.Key,
                        isEnhanced ? "Enhanced" : "Base",
                        baseSpellKey,
                        isEnhanced,
                        spell.SpellType,
                        spell.TypeBehavior,
                        levelReq,
                        levelOk,
                        allowedByClass,
                        warning
                    )
                );
            }

            phaseValidationExports.Add(
                new PhaseValidationExport(
                    phaseName,
                    phaseClass,
                    classMap.Spells.Count,
                    spellValidationEntries
                )
            );

            Console.WriteLine("");
        }

        if (scenario.TryGetProperty("spellValidationOutputJson", out var spellValidationOutputElement)
            && spellValidationOutputElement.ValueKind == JsonValueKind.String)
        {
            var outputRaw = spellValidationOutputElement.GetString();

            if (!string.IsNullOrWhiteSpace(outputRaw))
            {
                var outputPath = ResolvePathRelativeToFile(scenarioPath, outputRaw);

                var export = new SpellValidationExport(
                    DateTime.UtcNow.ToString("O"),
                    scenarioPath,
                    workspace,
                    globalClass,
                    characterLevel,
                    spellCatalog.Count,
                    phaseValidationExports,
                    totalWarnings,
                    new[]
                    {
                        "Class allowed uses source-scanned base SpellList entries.",
                        "Enhanced spell variants validate against their base spell for class ownership.",
                        "Spell effect formulas are not implemented yet."
                    }
                );

                var json = JsonSerializer.Serialize(
                    export,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    }
                );

                File.WriteAllText(outputPath, json);

                Console.WriteLine($"Wrote spell validation JSON: {outputPath}");
                Console.WriteLine("");
            }
        }

        Console.WriteLine("Validation summary:");
        Console.WriteLine($"  Warnings: {totalWarnings}");
        Console.WriteLine("");
        Console.WriteLine("Notes:");
        Console.WriteLine("  Class allowed uses source-scanned base SpellList.Add entries.");
        Console.WriteLine("  Challenge-added or temporarily modified spells are not counted as normal class spells yet.");
        Console.WriteLine("  Level requirement uses Spells.bytes Requirements parsed as LevelReq.");
    }


    private static string ResolveHeroImplementationClass(
        string sourceRoot,
        string className)
    {
        var files = Directory
            .EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories)
            .ToList();

        var pattern = new Regex(
            @"HeroesNames\." + Regex.Escape(className) + @"\s*,\s*new\s+([A-Za-z0-9_]+)\s*\(",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        foreach (var file in files)
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


    private static bool IsEnhancedSpellVariant(
        string spellKey,
        IReadOnlyDictionary<string, SpellInfo> spellCatalog)
    {
        if (string.IsNullOrWhiteSpace(spellKey))
        {
            return false;
        }

        if (!spellKey.StartsWith("E", StringComparison.OrdinalIgnoreCase) || spellKey.Length <= 1)
        {
            return false;
        }

        var baseKey = spellKey.Substring(1);
        return spellCatalog.ContainsKey(baseKey);
    }

    private static string GetBaseSpellKeyForVariant(
        string spellKey,
        IReadOnlyDictionary<string, SpellInfo> spellCatalog)
    {
        if (IsEnhancedSpellVariant(spellKey, spellCatalog))
        {
            return spellKey.Substring(1);
        }

        return spellKey;
    }
    private static bool IsSpellAllowedByClassMapIncludingEnhanced(
        IReadOnlySet<string> classSpells,
        string spellKey)
    {
        if (classSpells.Contains(spellKey, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var baseSpellKey = GetBaseSpellKeyForClassGate(spellKey);

        if (!baseSpellKey.Equals(spellKey, StringComparison.OrdinalIgnoreCase)
            && classSpells.Contains(baseSpellKey, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string GetBaseSpellKeyForClassGate(string spellKey)
    {
        // Enhanced spells are represented by a leading E in several data keys:
        //   EStabilizeTheFlow -> StabilizeTheFlow
        //   EVoidLure         -> VoidLure
        //
        // Class SpellList contains the base spell key, while the scenario/data may
        // refer to the enhanced variant. For class eligibility, enhanced variants
        // are allowed when their base spell is allowed.
        if (spellKey.Length > 1 && spellKey.StartsWith("E", StringComparison.OrdinalIgnoreCase))
        {
            return spellKey.Substring(1);
        }

        return spellKey;
    }
    private static ClassSpellMap BuildClassSpellMap(
        string workspacePath,
        string className,
        IReadOnlyDictionary<string, SpellInfo> spellCatalog)
    {
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);

        if (!Directory.Exists(sourceRoot))
        {
            return ClassSpellMap.Empty(className);
        }

        var implementationClass = ResolveHeroImplementationClass(sourceRoot, className);

        var spells = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sourceFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var listPattern = new Regex(
            @"SpellList\s*=\s*new\s+List\s*<\s*Spells\s*>\s*\{(?<body>[\s\S]*?)\};",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var spellRefPattern = new Regex(
            @"Spells\.([A-Za-z0-9_]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var implementationFile = Directory
            .EnumerateFiles(sourceRoot, implementationClass + ".cs", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (implementationFile is not null)
        {
            var text = File.ReadAllText(implementationFile);
            var match = listPattern.Match(text);

            if (match.Success)
            {
                var body = match.Groups["body"].Value;

                foreach (Match spellMatch in spellRefPattern.Matches(body))
                {
                    spells.Add(spellMatch.Groups[1].Value);
                }

                sourceFiles.Add(Path.GetRelativePath(sourceRoot, implementationFile).Replace("\\", "/"));
            }
        }

        // Fallback: scan files containing both the implementation class name and SpellList assignment.
        if (spells.Count == 0)
        {
            foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);

                if (!text.Contains(implementationClass, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var match = listPattern.Match(text);

                if (!match.Success)
                {
                    continue;
                }

                var body = match.Groups["body"].Value;

                foreach (Match spellMatch in spellRefPattern.Matches(body))
                {
                    spells.Add(spellMatch.Groups[1].Value);
                }

                sourceFiles.Add(Path.GetRelativePath(sourceRoot, file).Replace("\\", "/"));
            }
        }

        return new ClassSpellMap(className, spells, sourceFiles);
    }
    private static Dictionary<string, SpellInfo> LoadSpellCatalog(string workspacePath)
    {
        var spellsFile = WorkspacePaths.FindDataFile(workspacePath, "Spells");

        if (spellsFile is null)
        {
            return new Dictionary<string, SpellInfo>(StringComparer.OrdinalIgnoreCase);
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(spellsFile));

        var result = new Dictionary<string, SpellInfo>(StringComparer.OrdinalIgnoreCase);

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

    private static Dictionary<string, SpellInfo> BuildSpellNameIndex(
        IReadOnlyDictionary<string, SpellInfo> spellCatalog)
    {
        var result = new Dictionary<string, SpellInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var spell in spellCatalog.Values)
        {
            if (!string.IsNullOrWhiteSpace(spell.Key))
            {
                result[spell.Key] = spell;
            }

            if (!string.IsNullOrWhiteSpace(spell.Name))
            {
                result[spell.Name] = spell;
            }
        }

        return result;
    }

    private static string? FindDataFile(string rawRoot, string logicalName)
    {
        var candidates = new[]
        {
            logicalName + ".bytes",
            logicalName + ".bytes.txt",
            logicalName + ".json"
        };

        return Directory
            .EnumerateFiles(rawRoot, "*", SearchOption.AllDirectories)
            .Where(path => candidates.Any(
                candidate => Path.GetFileName(path).Equals(candidate, StringComparison.OrdinalIgnoreCase)
            ))
            .OrderBy(path => path)
            .FirstOrDefault();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        var result = new List<string>();

        if (!root.TryGetProperty(propertyName, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in array.EnumerateArray())
        {
            var value = GetScalar(item);

            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        var value = Get(root, propertyName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required scenario property: {propertyName}");
        }

        return value;
    }

    private static string ResolvePathRelativeToFile(string baseFile, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var dir = Path.GetDirectoryName(baseFile) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(dir, path));
    }

    private static string Get(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return GetScalar(value);
    }

    private static string GetScalar(JsonElement value)
    {
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


    private sealed record SpellValidationExport(
        string GeneratedAtUtc,
        string Scenario,
        string Workspace,
        string GlobalClass,
        int CharacterLevel,
        int SpellCatalogCount,
        IReadOnlyList<PhaseValidationExport> Phases,
        int WarningCount,
        IReadOnlyList<string> Warnings
    );

    private sealed record PhaseValidationExport(
        string PhaseName,
        string ClassName,
        int MappedBaseClassSpellCount,
        IReadOnlyList<SpellValidationEntryExport> Spells
    );

    private sealed record SpellValidationEntryExport(
        string ConfiguredName,
        bool Found,
        string Name,
        string Key,
        string Variant,
        string BaseKey,
        bool EnhancedSelected,
        string SpellType,
        string TypeBehavior,
        int LevelRequirement,
        bool LevelSatisfied,
        bool? ClassAllowed,
        string Warning
    );
    private sealed record SpellInfo(
        string Name,
        string Key,
        string SpellType,
        string TypeBehavior,
        string Build,
        string Duration,
        string Requirements
    );

    private sealed record ClassSpellMap(
        string ClassName,
        IReadOnlySet<string> Spells,
        IReadOnlySet<string> SourceFiles)
    {
        public static ClassSpellMap Empty(string className)
        {
            return new ClassSpellMap(
                className,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            );
        }
    }
}





