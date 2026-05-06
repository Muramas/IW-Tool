using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ApplyPhasePresetsToScenarioCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --apply-phase-presets-to-scenario .\save_item_presets_temporalist.json .\scenario_in.json .\scenario_out.json Burst=7 ""Void Mana=4""");
            return;
        }

        var presetPath = Path.GetFullPath(args[1]);
        var scenarioInPath = Path.GetFullPath(args[2]);
        var scenarioOutPath = Path.GetFullPath(args[3]);
        var mappings = args.Skip(4).ToList();

        if (!File.Exists(presetPath))
        {
            Console.WriteLine($"Preset JSON not found: {presetPath}");
            return;
        }

        if (!File.Exists(scenarioInPath))
        {
            Console.WriteLine($"Scenario JSON not found: {scenarioInPath}");
            return;
        }

        var phaseToPreset = ParseMappings(mappings);

        using var presetDoc = JsonDocument.Parse(File.ReadAllText(presetPath));
        var presetRoot = presetDoc.RootElement;

        if (!presetRoot.TryGetProperty("Presets", out var presets)
            || presets.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Preset JSON does not contain Presets[].");
            return;
        }

        var presetIndex = BuildPresetIndex(presets);

        var scenarioNode = JsonNode.Parse(File.ReadAllText(scenarioInPath));

        if (scenarioNode is not JsonObject scenarioObject)
        {
            Console.WriteLine("Scenario root is not a JSON object.");
            return;
        }

        if (!scenarioObject.TryGetPropertyValue("phases", out var phasesNode)
            || phasesNode is not JsonArray phasesArray)
        {
            Console.WriteLine("Scenario does not contain phases[].");
            return;
        }

        var appliedCount = 0;
        var warnings = new List<string>();

        foreach (var phaseNode in phasesArray)
        {
            if (phaseNode is not JsonObject phaseObject)
            {
                continue;
            }

            var phaseName = phaseObject["name"]?.GetValue<string>() ?? "";

            var mapping = FindMappingForPhase(phaseName, phaseToPreset);

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

            var presetName = GetString(preset, "Name");

            if (!preset.TryGetProperty("Items", out var presetItems)
                || presetItems.ValueKind != JsonValueKind.Array)
            {
                warnings.Add($"Preset index {mapping.Value.PresetIndex} has no Items[] for phase: {phaseName}");
                continue;
            }

            var equipmentObject = BuildEquipmentObject(presetItems);

            phaseObject["equipment"] = equipmentObject;
            phaseObject["equipmentPresetIndex"] = mapping.Value.PresetIndex;
            phaseObject["equipmentPresetName"] = presetName;

            appliedCount++;
        }

        scenarioObject["equipmentPresetAppliedAtUtc"] = DateTime.UtcNow.ToString("O");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(scenarioOutPath, scenarioObject.ToJsonString(options));

        Console.WriteLine("Apply phase presets to scenario");
        Console.WriteLine("-------------------------------");
        Console.WriteLine($"Preset file:    {presetPath}");
        Console.WriteLine($"Scenario in:    {scenarioInPath}");
        Console.WriteLine($"Scenario out:   {scenarioOutPath}");
        Console.WriteLine($"Mappings:       {phaseToPreset.Count}");
        Console.WriteLine($"Phases updated: {appliedCount}");

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

            var phase = split[0].Trim();
            var rawPreset = split[1].Trim();

            if (string.IsNullOrWhiteSpace(phase))
            {
                continue;
            }

            if (!int.TryParse(rawPreset, out var presetIndex))
            {
                continue;
            }

            result.Add(new PhasePresetMapping(phase, presetIndex));
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

    private static Dictionary<int, JsonElement> BuildPresetIndex(JsonElement presets)
    {
        var result = new Dictionary<int, JsonElement>();

        foreach (var preset in presets.EnumerateArray())
        {
            var index = GetInt(preset, "Index");

            if (!result.ContainsKey(index))
            {
                result[index] = preset;
            }
        }

        return result;
    }

    private static JsonObject BuildEquipmentObject(JsonElement presetItems)
    {
        var equipment = new JsonObject();

        foreach (var item in presetItems.EnumerateArray())
        {
            var positionName = GetString(item, "PositionName");
            var expectedSlot = GetString(item, "ExpectedSlot");
            var actualSlot = GetString(item, "ActualSlot");
            var itemId = GetString(item, "ItemId");
            var itemName = GetString(item, "ItemName");
            var tier = GetInt(item, "Tier");
            var enchant = GetInt(item, "Enchant");
            var hasItem = GetBool(item, "HasItem");

            if (string.IsNullOrWhiteSpace(positionName))
            {
                continue;
            }

            if (!hasItem || itemId == "-1")
            {
                equipment[positionName] = new JsonObject
                {
                    ["slot"] = string.IsNullOrWhiteSpace(actualSlot) ? expectedSlot : actualSlot,
                    ["expectedSlot"] = expectedSlot,
                    ["itemId"] = "",
                    ["itemName"] = "",
                    ["tier"] = 0,
                    ["enchantLevel"] = 0,
                    ["bonusEnchantLevel"] = 0
                };

                continue;
            }

            equipment[positionName] = new JsonObject
            {
                ["slot"] = string.IsNullOrWhiteSpace(actualSlot) ? expectedSlot : actualSlot,
                ["expectedSlot"] = expectedSlot,
                ["itemId"] = itemId,
                ["itemName"] = itemName,
                ["tier"] = tier,
                ["enchantLevel"] = enchant,
                ["bonusEnchantLevel"] = 0
            };
        }

        return equipment;
    }

    private static string GetString(JsonElement element, string propertyName)
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

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private readonly record struct PhasePresetMapping(
        string PhaseName,
        int PresetIndex
    );
}
