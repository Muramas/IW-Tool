using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ScenarioOptionsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --scenario-options .\iw_workspace_vNext [output.json]");
            return;
        }

        var workspacePath = args[1];
        var outputPath = args.Length >= 3
            ? args[2]
            : ".\\scenario_options.json";

        var classes = LoadClasses(workspacePath);
        var items = LoadItems(workspacePath);
        var spells = LoadSpells(workspacePath);
        var familiars = LoadFamiliars(workspacePath);
        var pets = LoadPetsFromSource(workspacePath);
        var equipmentSlots = items
            .Select(x => x.Slot)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => GetSlotSortOrder(x))
            .ThenBy(x => x)
            .ToList();

        var equipmentPositions = BuildEquipmentPositions();

        var itemsBySlot = items
            .GroupBy(x => x.Slot, StringComparer.OrdinalIgnoreCase)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Name).ToList(),
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var position in equipmentPositions)
        {
            if (!itemsBySlot.ContainsKey(position.Slot))
            {
                itemsBySlot[position.Slot] = new List<ItemOption>();
            }
        }

        var spellsByClass = new Dictionary<string, List<SpellOption>>(StringComparer.OrdinalIgnoreCase);

        foreach (var classOption in classes)
        {
            var classSpellKeys = LoadClassSpellKeys(workspacePath, classOption.Name);
            var spellOptions = new List<SpellOption>();

            foreach (var key in classSpellKeys.OrderBy(x => x))
            {
                if (spells.TryGetValue(key, out var spell))
                {
                    spellOptions.Add(spell);
                }
            }

            spellsByClass[classOption.Name] = spellOptions;
        }

        var export = new ScenarioOptionsExport(
            DateTime.UtcNow.ToString("O"),
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            classes,
            new[]
            {
                "Intelligence",
                "Insight",
                "Spellcraft",
                "Wisdom",
                "Dominance",
                "Patience",
                "Mastery",
                "Empathy",
                "Versatility"
            },
            equipmentSlots,
            equipmentPositions,
            itemsBySlot,
            familiars,
            pets,
            spells.Values.OrderBy(x => x.Name).ToList(),
            spellsByClass,
            new[]
            {
                "Late-game limited realm classes are marked and should be listed last in the GUI.",
                "Spell options are source-scanned from class implementation SpellList where available.",
                "Enhanced spell variants are handled separately by spell validation."
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

        Console.WriteLine("Scenario options");
        Console.WriteLine("----------------");
        Console.WriteLine($"Workspace:       {workspacePath}");
        Console.WriteLine($"Export root:     {WorkspacePaths.ResolveExportRoot(workspacePath)}");
        Console.WriteLine($"Classes:         {classes.Count}");
        Console.WriteLine($"Items:           {items.Count}");
        Console.WriteLine($"Equipment slots: {equipmentSlots.Count}");
        Console.WriteLine($"Equipment pos.:  {equipmentPositions.Count}");
        Console.WriteLine($"Familiars:       {familiars.Count}");
        Console.WriteLine($"Pets:            {pets.Count}");
        Console.WriteLine($"Spells:          {spells.Count}");
        Console.WriteLine($"Output:          {Path.GetFullPath(outputPath)}");
    }

    private static List<ClassOption> LoadClasses(string workspacePath)
    {
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);
        var result = new Dictionary<string, ClassOption>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(sourceRoot))
        {
            return new List<ClassOption>();
        }

        var pattern = new Regex(
            @"HeroesNames\.([A-Za-z0-9_]+)\s*,\s*new\s+([A-Za-z0-9_]+)\s*\(",
            RegexOptions.Compiled
        );

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);

            foreach (Match match in pattern.Matches(text))
            {
                var name = match.Groups[1].Value;
                var implementation = match.Groups[2].Value;
                var limited = IsLateGameLimitedClass(name);

                result[name] = new ClassOption(
                    name,
                    implementation,
                    limited
                );
            }
        }

        return result.Values
            .OrderBy(x => x.LateGameLimited)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static bool IsLateGameLimitedClass(string name)
    {
        return name.Equals("Cryomancer", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Nosferatu", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Artificer", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Shapeshifter", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Ranger", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Archer", StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, SpellOption> LoadSpells(string workspacePath)
    {
        var result = new Dictionary<string, SpellOption>(StringComparer.OrdinalIgnoreCase);
        var file = WorkspacePaths.FindDataFile(workspacePath, "Spells");

        if (file is null)
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var spell in doc.RootElement.EnumerateArray())
        {
            var option = new SpellOption(
                Get(spell, "Key"),
                Get(spell, "Name"),
                Get(spell, "SpellType"),
                Get(spell, "TypeBehavior"),
                Get(spell, "Build"),
                Get(spell, "Duration"),
                Get(spell, "Requirements")
            );

            if (!string.IsNullOrWhiteSpace(option.Key))
            {
                result[option.Key] = option;
            }
        }

        return result;
    }

    private static List<string> LoadClassSpellKeys(string workspacePath, string className)
    {
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(sourceRoot))
        {
            return new List<string>();
        }

        var implementationClass = ResolveHeroImplementationClass(sourceRoot, className);
        var implementationFile = Directory
            .EnumerateFiles(sourceRoot, implementationClass + ".cs", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (implementationFile is null)
        {
            return new List<string>();
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
            return new List<string>();
        }

        var body = match.Groups["body"].Value;

        foreach (Match spellMatch in spellRefPattern.Matches(body))
        {
            result.Add(spellMatch.Groups[1].Value);
        }

        return result.OrderBy(x => x).ToList();
    }

    private static string ResolveHeroImplementationClass(string sourceRoot, string className)
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

    private static List<ItemOption> LoadItems(string workspacePath)
    {
        var result = new List<ItemOption>();
        var file = WorkspacePaths.FindDataFile(workspacePath, "Items");

        if (file is null)
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            result.Add(
                new ItemOption(
                    Get(item, "ID"),
                    Get(item, "Name"),
                    Get(item, "Slot"),
                    Get(item, "Quality"),
                    Get(item, "Set"),
                    Get(item, "Enchant"),
                    Get(item, "EBase")
                )
            );
        }

        return result
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .OrderBy(x => x.Slot)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private static List<PetOption> LoadPetsFromSource(string workspacePath)
    {
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);

        if (!Directory.Exists(sourceRoot))
        {
            return new List<PetOption>();
        }

        var petKeys = LoadPetKeys(workspacePath);
        var result = new List<PetOption>();

        foreach (var key in petKeys)
        {
            var found = FindPetImplementation(sourceRoot, key);

            result.Add(
                new PetOption(
                    key,
                    string.IsNullOrWhiteSpace(found.DisplayName) ? key : found.DisplayName,
                    found.SourceFile,
                    found.Found
                )
            );
        }

        return result
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static List<string> LoadPetKeys(string workspacePath)
    {
        var file = WorkspacePaths.FindSourceFile(workspacePath, "PetNames.cs");

        if (file is null)
        {
            return new List<string>();
        }

        var text = File.ReadAllText(file);

        var enumMatch = Regex.Match(
            text,
            @"enum\s+PetNames\s*\{(?<body>[\s\S]*?)\}",
            RegexOptions.IgnoreCase
        );

        if (!enumMatch.Success)
        {
            return new List<string>();
        }

        var body = enumMatch.Groups["body"].Value;
        var keys = new List<string>();

        foreach (var rawPart in body.Split(','))
        {
            var part = rawPart.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var key = part.Split('=')[0].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (key.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            keys.Add(key);
        }

        return keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static PetImplementation FindPetImplementation(
        string sourceRoot,
        string petKey)
    {
        var keyPattern = new Regex(
            @"NameKey\s*=\s*PetNames\." + Regex.Escape(petKey) + @"\b",
            RegexOptions.IgnoreCase
        );

        var namePattern = new Regex(
            @"(?:base\.)?Name\s*=\s*""(?<name>[^""]+)""",
            RegexOptions.IgnoreCase
        );

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var sourceText = File.ReadAllText(file);
            var keyMatch = keyPattern.Match(sourceText);

            if (!keyMatch.Success)
            {
                continue;
            }

            var windowStart = Math.Max(0, keyMatch.Index - 800);
            var windowLength = Math.Min(sourceText.Length - windowStart, 1800);
            var window = sourceText.Substring(windowStart, windowLength);

            var nameMatch = namePattern.Match(window);
            var displayName = nameMatch.Success
                ? nameMatch.Groups["name"].Value
                : petKey;

            return new PetImplementation(
                true,
                displayName,
                Path.GetRelativePath(sourceRoot, file).Replace("\\", "/")
            );
        }

        return new PetImplementation(
            false,
            petKey,
            ""
        );
    }
    private static List<FamiliarOption> LoadFamiliars(string workspacePath)
    {
        var result = new List<FamiliarOption>();

        foreach (var logicalName in new[] { "Familiars" })
        {
            var file = WorkspacePaths.FindDataFile(workspacePath, logicalName);

            if (file is null)
            {
                continue;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(file));

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var pet in doc.RootElement.EnumerateArray())
            {
                var id = FirstNonEmpty(Get(pet, "ID"), Get(pet, "Id"), Get(pet, "Key"));
                var name = FirstNonEmpty(Get(pet, "Name"), Get(pet, "Key"), id);

                if (!string.IsNullOrWhiteSpace(name))
                {
                    result.Add(new FamiliarOption(id, name, logicalName));
                }
            }
        }

        return result
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static List<EquipmentPositionOption> BuildEquipmentPositions()
    {
        return new List<EquipmentPositionOption>
        {
            new("Ring 1", "Ring", 0, false, "Ring"),
            new("Ring 2", "Ring", 1, false, "Ring"),
            new("Head", "Head", 0, false, "Head"),
            new("Chest", "Chest", 0, false, "Chest"),
            new("Hands", "Hands", 0, false, "Hands"),
            new("Feet", "Boots", 0, false, "Boots"),
            new("Shoulder", "Shoulder", 0, false, "Shoulder"),
            new("Waist", "Waist", 0, false, "Waist"),
            new("Neck", "Neck", 0, false, "Neck"),
            new("Back", "Back", 0, false, "Back"),
            new("Wrist", "Wrist", 0, false, "Wrist"),
            new("Weapon", "Weapon", 0, false, "Weapon"),
            new("Offhand", "Offhand", 0, false, "Offhand"),
            new("Research", "Research", 0, false, "Research"),
            new("Trophy 1", "Misc", 0, false, "Misc"),
            new("Trophy 2", "Misc", 1, false, "Misc"),
            new("Legs", "Pants", 0, false, "Pants"),
            new("Mount", "Mount", 0, false, "Mount"),
            new("Phylactery", "Phylactery", 0, false, "Phylactery"),
            new("Accessory", "Accessory", 0, false, "Accessory")
        };
    }
    private static int GetSlotSortOrder(string slot)
    {
        var order = new[]
        {
            "Head",
            "Chest",
            "Shoulder",
            "Hand",
            "Feet",
            "Neck",
            "Finger",
            "Waist",
            "Back",
            "Wrist",
            "Weapon",
            "Offhand",
            "Legs",
            "Research",
            "Trophy",
            "Mount",
            "Phylactery"
        };

        for (var i = 0; i < order.Length; i++)
        {
            if (slot.Equals(order[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 999;
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
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

    private sealed record ScenarioOptionsExport(
        string GeneratedAtUtc,
        string Workspace,
        string ExportRoot,
        IReadOnlyList<ClassOption> Classes,
        IReadOnlyList<string> Attributes,
        IReadOnlyList<string> EquipmentSlots,
        IReadOnlyList<EquipmentPositionOption> EquipmentPositions,
        IReadOnlyDictionary<string, List<ItemOption>> ItemsBySlot,
        IReadOnlyList<FamiliarOption> Familiars,
        IReadOnlyList<PetOption> Pets,
        IReadOnlyList<SpellOption> Spells,
        IReadOnlyDictionary<string, List<SpellOption>> SpellsByClass,
        IReadOnlyList<string> Warnings
    );

    private sealed record ClassOption(
        string Name,
        string ImplementationClass,
        bool LateGameLimited
    );

    private sealed record EquipmentPositionOption(
        string Name,
        string Slot,
        int PositionIndex,
        bool AllowDuplicateItem,
        string DuplicateGroup
    );

    private sealed record ItemOption(
        string Id,
        string Name,
        string Slot,
        string Quality,
        string Set,
        string EnchantKey,
        string EnchantBase
    );

    private sealed record SpellOption(
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        string Build,
        string Duration,
        string LevelRequirement
    );

    private sealed record PetOption(
        string Key,
        string Name,
        string SourceFile,
        bool SourceBacked
    );

    private sealed record PetImplementation(
        bool Found,
        string DisplayName,
        string SourceFile
    );

    private sealed record FamiliarOption(
        string Id,
        string Name,
        string Source
    );
}





