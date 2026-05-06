using System.Text.Json;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ClassSpellPhaseCommand
{
    public static void RunSpellCatalog(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --spell-catalog .\iw_workspace_vNext [optional-filter]");
            return;
        }

        var workspacePath = args[1];
        var filter = args.Length >= 3 ? args[2] : "";

        var spells = LoadSpells(workspacePath);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            spells = spells
                .Where(spell =>
                    spell.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || spell.Key.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || spell.SpellType.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || spell.TypeBehavior.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        Console.WriteLine("Spell catalog");
        Console.WriteLine("-------------");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Filter:    {(string.IsNullOrWhiteSpace(filter) ? "(none)" : filter)}");
        Console.WriteLine($"Spells:    {spells.Count}");
        Console.WriteLine("");

        foreach (var group in spells.GroupBy(x => x.SpellType).OrderBy(x => x.Key))
        {
            Console.WriteLine(group.Key);
            Console.WriteLine(new string('-', group.Key.Length));

            foreach (var spell in group.OrderBy(x => x.Name))
            {
                Console.WriteLine($"  {spell.Name}");
                Console.WriteLine($"    Key:          {spell.Key}");
                Console.WriteLine($"    Behavior:     {spell.TypeBehavior}");
                Console.WriteLine($"    Build:        {spell.Build}");
                Console.WriteLine($"    Duration:     {spell.Duration}");
                Console.WriteLine($"    Requirements: {spell.Requirements}");
            }

            Console.WriteLine("");
        }

        Console.WriteLine("Note:");
        Console.WriteLine("  This catalog is source/data-backed from Spells.bytes.");
        Console.WriteLine("  Class eligibility is not enforced here yet.");
    }

    public static void RunPhaseClassSummary(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --phase-class-summary .\scenario_head_hero.json");
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

        var spells = LoadSpells(workspace);
        var spellIndex = BuildSpellIndex(spells);

        var globalClass = "";

        if (scenario.TryGetProperty("globalContext", out var global)
            && global.ValueKind == JsonValueKind.Object)
        {
            globalClass = Get(global, "class");
        }

        var phaseReports = new List<PhaseSpellReport>();

        if (!scenario.TryGetProperty("phases", out var phases)
            || phases.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Scenario does not contain phases[].");
            return;
        }

        foreach (var phase in phases.EnumerateArray())
        {
            var phaseName = Get(phase, "name");
            var phaseClass = Get(phase, "class");

            if (string.IsNullOrWhiteSpace(phaseClass))
            {
                phaseClass = globalClass;
            }

            var spellNames = ReadStringArray(phase, "spells");

            var spellReports = new List<PhaseSpellEntry>();

            foreach (var configuredSpell in spellNames)
            {
                if (spellIndex.TryGetValue(configuredSpell, out var spell))
                {
                    spellReports.Add(
                        new PhaseSpellEntry(
                            configuredSpell,
                            true,
                            spell.Name,
                            spell.Key,
                            spell.SpellType,
                            spell.TypeBehavior,
                            spell.Build,
                            spell.Duration,
                            spell.Requirements,
                            ""
                        )
                    );
                }
                else
                {
                    spellReports.Add(
                        new PhaseSpellEntry(
                            configuredSpell,
                            false,
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "",
                            "Spell was not found in Spells.bytes by Name or Key."
                        )
                    );
                }
            }

            phaseReports.Add(
                new PhaseSpellReport(
                    phaseName,
                    phaseClass,
                    spellReports
                )
            );
        }

        Console.WriteLine("Phase class/spell summary");
        Console.WriteLine("-------------------------");
        Console.WriteLine($"Scenario:      {scenarioPath}");
        Console.WriteLine($"Workspace:     {workspace}");
        Console.WriteLine($"Global class:  {(string.IsNullOrWhiteSpace(globalClass) ? "(not set)" : globalClass)}");
        Console.WriteLine($"Spell catalog: {spells.Count} spells");
        Console.WriteLine("");

        foreach (var phase in phaseReports)
        {
            Console.WriteLine($"Phase: {phase.PhaseName}");
            Console.WriteLine($"Class: {(string.IsNullOrWhiteSpace(phase.ClassName) ? "(not set)" : phase.ClassName)}");
            Console.WriteLine($"Configured spells: {phase.Spells.Count}");
            Console.WriteLine("");

            foreach (var spell in phase.Spells)
            {
                if (!spell.Found)
                {
                    Console.WriteLine($"  MISSING: {spell.ConfiguredName}");
                    Console.WriteLine($"    Warning: {spell.Warning}");
                    continue;
                }

                Console.WriteLine($"  {spell.Name}");
                Console.WriteLine($"    Configured as: {spell.ConfiguredName}");
                Console.WriteLine($"    Key:           {spell.Key}");
                Console.WriteLine($"    SpellType:     {spell.SpellType}");
                Console.WriteLine($"    Behavior:      {spell.TypeBehavior}");
                Console.WriteLine($"    Build:         {spell.Build}");
                Console.WriteLine($"    Duration:      {spell.Duration}");
                Console.WriteLine($"    Requirements:  {spell.Requirements}");
            }

            Console.WriteLine("");
        }

        Console.WriteLine("Class gating status:");
        Console.WriteLine("  Class context is now modeled at scenario/phase level.");
        Console.WriteLine("  Spell metadata is loaded from Spells.bytes.");
        Console.WriteLine("  Exact class-to-spell eligibility is not enforced yet.");
        Console.WriteLine("  Next step: inspect/map SpellBook.cs and class unlock logic to turn Requirements into verified eligibility.");

        if (TryGetString(scenario, "phaseSpellOutputJson", out var outputJson))
        {
            var outputPath = ResolvePathRelativeToFile(scenarioPath, outputJson);

            var export = new
            {
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                scenario = scenarioPath,
                workspace,
                globalClass,
                spellCatalogCount = spells.Count,
                phases = phaseReports,
                warnings = new[]
                {
                    "Class context is modeled.",
                    "Spell metadata is loaded from Spells.bytes.",
                    "Class-to-spell eligibility is not enforced yet.",
                    "Spell effect formulas are not implemented yet."
                }
            };

            var json = JsonSerializer.Serialize(
                export,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            );

            File.WriteAllText(outputPath, json);

            Console.WriteLine("");
            Console.WriteLine($"Wrote phase spell JSON: {outputPath}");
        }
    }

    private static List<SpellInfo> LoadSpells(string workspacePath)
    {
        var rawRoot = Path.Combine(workspacePath, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            throw new DirectoryNotFoundException($"Missing raw_files directory: {rawRoot}");
        }

        var spellsFile = FindDataFile(rawRoot, "Spells");

        if (spellsFile is null)
        {
            return new List<SpellInfo>();
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(spellsFile));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return new List<SpellInfo>();
        }

        var result = new List<SpellInfo>();

        foreach (var spell in doc.RootElement.EnumerateArray())
        {
            result.Add(
                new SpellInfo(
                    Get(spell, "Name"),
                    Get(spell, "Key"),
                    Get(spell, "SpellType"),
                    Get(spell, "TypeBehavior"),
                    Get(spell, "Build"),
                    Get(spell, "Duration"),
                    Get(spell, "Requirements"),
                    Get(spell, "Description")
                )
            );
        }

        return result;
    }

    private static Dictionary<string, SpellInfo> BuildSpellIndex(
        IReadOnlyList<SpellInfo> spells)
    {
        var index = new Dictionary<string, SpellInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var spell in spells)
        {
            if (!string.IsNullOrWhiteSpace(spell.Name))
            {
                index[spell.Name] = spell;
            }

            if (!string.IsNullOrWhiteSpace(spell.Key))
            {
                index[spell.Key] = spell;
            }
        }

        return index;
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

    private static bool TryGetString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = Get(root, propertyName);
        return !string.IsNullOrWhiteSpace(value);
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

    private sealed record SpellInfo(
        string Name,
        string Key,
        string SpellType,
        string TypeBehavior,
        string Build,
        string Duration,
        string Requirements,
        string Description
    );

    private sealed record PhaseSpellReport(
        string PhaseName,
        string ClassName,
        IReadOnlyList<PhaseSpellEntry> Spells
    );

    private sealed record PhaseSpellEntry(
        string ConfiguredName,
        bool Found,
        string Name,
        string Key,
        string SpellType,
        string TypeBehavior,
        string Build,
        string Duration,
        string Requirements,
        string Warning
    );
}
