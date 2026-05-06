using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class GenerateScenarioFromSaveCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 6)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --generate-scenario-from-save .\save_export.txt .\iw_workspace_vNext .\scenario_base.json .\scenario_out.json Burst=7 ""Void Mana=4""");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var scenarioBasePath = Path.GetFullPath(args[3]);
        var scenarioOutPath = Path.GetFullPath(args[4]);
        var mappings = ParseMappings(args.Skip(5).ToList());

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        if (!File.Exists(scenarioBasePath))
        {
            Console.WriteLine($"Scenario file not found: {scenarioBasePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var saveDoc = JsonDocument.Parse(saveJson);
        var saveRoot = saveDoc.RootElement;

        var heroEnum = LoadEnumMap(workspacePath, "HeroesNames.cs", "HeroesNames");
        var heroId = GetInt(saveRoot, "Hero");
        var className = heroEnum.TryGetValue(heroId, out var heroName)
            ? heroName
            : heroId.ToString();

        var petMaxLevel = GetInt(saveRoot, "PetMaxLevel");

        var itemIndex = LoadItemIndex(workspacePath);
        var craftIndex = LoadCraftItemIndex(saveRoot);
        var presets = ParseClassPresets(saveRoot, className, itemIndex, craftIndex);

        var presetIndex = presets.ToDictionary(x => x.Index, x => x);

        var scenarioNode = JsonNode.Parse(File.ReadAllText(scenarioBasePath));

        if (scenarioNode is not JsonObject scenarioObject)
        {
            Console.WriteLine("Scenario root is not an object.");
            return;
        }

                var heroMaxLevelAllTime = GetInt(saveRoot, "HeroMaxLevelAllTime");
        var totalManaLog = ReadBigNumberExponent(saveRoot, "ManaAllTime");

        ApplyGlobalContext(
            scenarioObject,
            className,
            petMaxLevel,
            heroMaxLevelAllTime,
            totalManaLog
        );

        if (!scenarioObject.TryGetPropertyValue("phases", out var phasesNode)
            || phasesNode is not JsonArray phasesArray)
        {
            Console.WriteLine("Scenario does not contain phases[].");
            return;
        }

        var warnings = new List<string>();
        var updatedPhases = 0;

        foreach (var phaseNode in phasesArray)
        {
            if (phaseNode is not JsonObject phaseObject)
            {
                continue;
            }

            var phaseName = phaseObject["name"]?.GetValue<string>() ?? "";
            var mapping = FindMappingForPhase(phaseName, mappings);

            if (mapping is null)
            {
                warnings.Add($"No preset mapping supplied for phase: {phaseName}");
                continue;
            }

            if (!presetIndex.TryGetValue(mapping.Value.PresetIndex, out var preset))
            {
                warnings.Add($"Preset index {mapping.Value.PresetIndex} not found for phase: {phaseName}");
                continue;
            }

            phaseObject["equipment"] = BuildEquipmentObject(preset.Items);
            phaseObject["equipmentPresetIndex"] = preset.Index;
            phaseObject["equipmentPresetName"] = preset.Name;

            updatedPhases++;
        }

        scenarioObject["generatedFromSaveAtUtc"] = DateTime.UtcNow.ToString("O");
        scenarioObject["generatedFromSaveFile"] = savePath;
        scenarioObject["generatedFromSaveClass"] = className;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(scenarioOutPath, scenarioObject.ToJsonString(options));

        Console.WriteLine("Generate scenario from save");
        Console.WriteLine("---------------------------");
        Console.WriteLine($"Save:            {savePath}");
        Console.WriteLine($"Workspace:       {workspacePath}");
        Console.WriteLine($"Base scenario:   {scenarioBasePath}");
        Console.WriteLine($"Output scenario: {scenarioOutPath}");
        Console.WriteLine($"Detected class:  {className} ({heroId})");
        Console.WriteLine($"Pet max level:   {petMaxLevel}");
        Console.WriteLine($"Class presets:   {presets.Count}");
        Console.WriteLine($"Mappings:        {mappings.Count}");
        Console.WriteLine($"Phases updated:  {updatedPhases}");

        if (warnings.Count > 0)
        {
            Console.WriteLine("");
            Console.WriteLine("Warnings:");
            foreach (var warning in warnings)
            {
                Console.WriteLine($"  {warning}");
            }
        }
    }

    private static void ApplyGlobalContext(
        JsonObject scenarioObject,
        string className,
        int petMaxLevel,
        int heroMaxLevelAllTime,
        int totalManaLog)
    {
        if (!scenarioObject.TryGetPropertyValue("globalContext", out var globalNode)
            || globalNode is not JsonObject globalObject)
        {
            globalObject = new JsonObject();
            scenarioObject["globalContext"] = globalObject;
        }

        if (!string.IsNullOrWhiteSpace(className))
        {
            globalObject["class"] = className;
        }

        if (petMaxLevel > 0)
        {
            globalObject["maxPetLevel"] = petMaxLevel.ToString();
        }

        if (heroMaxLevelAllTime > 0)
        {
            globalObject["heroMaxLevelAllTime"] = heroMaxLevelAllTime.ToString();
        }

        if (totalManaLog > 0)
        {
            globalObject["totalManaLog"] = totalManaLog.ToString();
        }

        globalObject["saveImportNote"] =
            "Imported from save. HeroMaxLevelAllTime is stored separately; current run character level is not mapped yet.";
    }
    private static List<PhasePresetMapping> ParseMappings(IReadOnlyList<string> mappings)
    {
        var result = new List<PhasePresetMapping>();

        foreach (var mapping in mappings)
        {
            var split = mapping.Split('=', 2);

            if (split.Length != 2)
            {
                continue;
            }

            var phaseName = split[0].Trim();

            if (!int.TryParse(split[1].Trim(), out var presetIndex))
            {
                continue;
            }

            result.Add(new PhasePresetMapping(phaseName, presetIndex));
        }

        return result;
    }

    private static PhasePresetMapping? FindMappingForPhase(
        string phaseName,
        IReadOnlyList<PhasePresetMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (phaseName.Equals(mapping.PhaseName, StringComparison.OrdinalIgnoreCase)
                || phaseName.Contains(mapping.PhaseName, StringComparison.OrdinalIgnoreCase)
                || mapping.PhaseName.Contains(phaseName, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return null;
    }

    private static List<PresetExport> ParseClassPresets(
        JsonElement root,
        string className,
        Dictionary<string, ItemOption> itemIndex,
        Dictionary<string, CraftItemInfo> craftIndex)
    {
        var result = new List<PresetExport>();

        if (!root.TryGetProperty("ItemPresets", out var itemPresets)
            || itemPresets.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!itemPresets.TryGetProperty("prime", out var prime)
            || prime.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!prime.TryGetProperty(className, out var classPresetElement)
            || classPresetElement.ValueKind != JsonValueKind.String)
        {
            return result;
        }

        var raw = classPresetElement.GetString() ?? "";

        var pattern = new Regex(
            @"#(?<index>\d+)#(?<name>[^@#]*)@(?<items>[^#]*)",
            RegexOptions.Compiled
        );

        foreach (Match match in pattern.Matches(raw))
        {
            var index = int.Parse(match.Groups["index"].Value);
            var name = match.Groups["name"].Value;
            var itemIds = match.Groups["items"].Value
                .Split(';', StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToList();

            var items = new List<PresetItemExport>();

            for (var i = 0; i < EquipmentPositions.Count; i++)
            {
                var position = EquipmentPositions[i];
                var itemId = i < itemIds.Count ? itemIds[i] : "-1";

                itemIndex.TryGetValue(itemId, out var item);
                craftIndex.TryGetValue(itemId, out var craft);

                items.Add(
                    new PresetItemExport(
                        i,
                        position.DisplayName,
                        position.ItemSlot,
                        itemId,
                        itemId != "-1",
                        item?.Name ?? "",
                        item?.Slot ?? "",
                        craft?.Tier ?? 0,
                        craft?.Enchant ?? 0
                    )
                );
            }

            result.Add(
                new PresetExport(
                    index,
                    string.IsNullOrWhiteSpace(name) ? $"Preset {index}" : name,
                    items
                )
            );
        }

        return result
            .OrderBy(x => x.Index)
            .ToList();
    }

    private static JsonObject BuildEquipmentObject(IReadOnlyList<PresetItemExport> presetItems)
    {
        var equipment = new JsonObject();

        foreach (var item in presetItems)
        {
            if (!item.HasItem || item.ItemId == "-1")
            {
                equipment[item.PositionName] = new JsonObject
                {
                    ["slot"] = item.ExpectedSlot,
                    ["expectedSlot"] = item.ExpectedSlot,
                    ["itemId"] = "",
                    ["itemName"] = "",
                    ["tier"] = 0,
                    ["enchantLevel"] = 0,
                    ["bonusEnchantLevel"] = 0
                };

                continue;
            }

            equipment[item.PositionName] = new JsonObject
            {
                ["slot"] = string.IsNullOrWhiteSpace(item.ActualSlot) ? item.ExpectedSlot : item.ActualSlot,
                ["expectedSlot"] = item.ExpectedSlot,
                ["itemId"] = item.ItemId,
                ["itemName"] = item.ItemName,
                ["tier"] = item.Tier,
                ["enchantLevel"] = item.Enchant,
                ["bonusEnchantLevel"] = 0
            };
        }

        return equipment;
    }

    private static Dictionary<string, ItemOption> LoadItemIndex(string workspacePath)
    {
        var result = new Dictionary<string, ItemOption>(StringComparer.OrdinalIgnoreCase);

        AddItemDataFile(result, workspacePath, new[] { "Items" }, forcedSlot: null);
        AddItemDataFile(result, workspacePath, new[] { "Weapons", "Weapon" }, forcedSlot: "Weapon");
        AddItemDataFile(result, workspacePath, new[] { "Mounts", "Mount" }, forcedSlot: "Mount");
        AddItemDataFile(result, workspacePath, new[] { "Phylactery", "Phylacteries" }, forcedSlot: "Phylactery");

        return result;
    }

    private static void AddItemDataFile(
        Dictionary<string, ItemOption> result,
        string workspacePath,
        IReadOnlyList<string> logicalNames,
        string? forcedSlot)
    {
        string? file = null;

        foreach (var logicalName in logicalNames)
        {
            file = WorkspacePaths.FindDataFile(workspacePath, logicalName);

            if (file is not null)
            {
                break;
            }
        }

        if (file is null)
        {
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var id = FirstNonEmpty(Get(item, "ID"), Get(item, "Id"), Get(item, "id"));

            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var name = FirstNonEmpty(Get(item, "Name"), id);
            var slot = forcedSlot ?? Get(item, "Slot");

            result[id] = new ItemOption(id, name, slot);
        }
    }

    private static Dictionary<string, CraftItemInfo> LoadCraftItemIndex(JsonElement root)
    {
        var result = new Dictionary<string, CraftItemInfo>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("Craft", out var craft)
            || craft.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!craft.TryGetProperty("Items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in items.EnumerateArray())
        {
            var id = Get(item, "id");

            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            result[id] = new CraftItemInfo(
                GetInt(item, "tier"),
                GetInt(item, "enchant")
            );
        }

        return result;
    }

    private static Dictionary<int, string> LoadEnumMap(
        string workspacePath,
        string fileName,
        string enumName)
    {
        var result = new Dictionary<int, string>();
        var file = WorkspacePaths.FindSourceFile(workspacePath, fileName);

        if (file is null)
        {
            return result;
        }

        var text = File.ReadAllText(file);
        var match = Regex.Match(
            text,
            @"enum\s+" + Regex.Escape(enumName) + @"\s*\{(?<body>[\s\S]*?)\}",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            return result;
        }

        var current = 0;

        foreach (var raw in match.Groups["body"].Value.Split(','))
        {
            var part = raw.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var pieces = part.Split('=');
            var key = pieces[0].Trim();

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
    }

    private static int ReadBigNumberExponent(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        return GetInt(value, "Exponent");
    }
    private static string DecodeSaveString(string input)
    {
        var cleaned = Regex.Replace(input, @"\s+", "");
        var bytes = Convert.FromBase64String(cleaned);

        using var inputStream = new MemoryStream(bytes);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
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

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0;
    }

    private static readonly IReadOnlyList<EquipmentPosition> EquipmentPositions =
        new List<EquipmentPosition>
        {
            new(0, "Ring 1", "Ring"),
            new(1, "Ring 2", "Ring"),
            new(2, "Head", "Head"),
            new(3, "Chest", "Chest"),
            new(4, "Hands", "Hands"),
            new(5, "Feet", "Boots"),
            new(6, "Shoulder", "Shoulder"),
            new(7, "Waist", "Waist"),
            new(8, "Neck", "Neck"),
            new(9, "Back", "Back"),
            new(10, "Wrist", "Wrist"),
            new(11, "Weapon", "Weapon"),
            new(12, "Offhand", "Offhand"),
            new(13, "Research", "Research"),
            new(14, "Trophy 1", "Misc"),
            new(15, "Trophy 2", "Misc"),
            new(16, "Legs", "Pants"),
            new(17, "Mount", "Mount"),
            new(18, "Phylactery", "Phylactery"),
            new(19, "Accessory", "Accessory")
        };

    private sealed record EquipmentPosition(
        int Position,
        string DisplayName,
        string ItemSlot
    );

    private sealed record PresetExport(
        int Index,
        string Name,
        IReadOnlyList<PresetItemExport> Items
    );

    private sealed record PresetItemExport(
        int Position,
        string PositionName,
        string ExpectedSlot,
        string ItemId,
        bool HasItem,
        string ItemName,
        string ActualSlot,
        int Tier,
        int Enchant
    );

    private sealed record ItemOption(
        string Id,
        string Name,
        string Slot
    );

    private sealed record CraftItemInfo(
        int Tier,
        int Enchant
    );

    private readonly record struct PhasePresetMapping(
        string PhaseName,
        int PresetIndex
    );
}



