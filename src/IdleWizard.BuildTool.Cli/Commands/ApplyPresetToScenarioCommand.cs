using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ApplyPresetToScenarioCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine(@"Usage: --apply-preset-to-scenario .\save_item_presets_temporalist.json PRESET_INDEX .\scenario_in.json .\scenario_out.json [phaseName]");
            Console.WriteLine("");
            Console.WriteLine(@"Examples:");
            Console.WriteLine(@"  --apply-preset-to-scenario .\save_item_presets_temporalist.json 7 .\scenario_head_hero.json .\scenario_from_save.json");
            Console.WriteLine(@"  --apply-preset-to-scenario .\save_item_presets_temporalist.json 7 .\scenario_head_hero.json .\scenario_from_save.json Burst");
            return;
        }

        var presetPath = Path.GetFullPath(args[1]);
        var presetIndex = int.Parse(args[2]);
        var scenarioInPath = Path.GetFullPath(args[3]);
        var scenarioOutPath = Path.GetFullPath(args[4]);
        var phaseFilter = args.Length >= 6 ? args[5] : "";

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

        using var presetDoc = JsonDocument.Parse(File.ReadAllText(presetPath));
        var presetRoot = presetDoc.RootElement;

        if (!presetRoot.TryGetProperty("Presets", out var presets)
            || presets.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Preset JSON does not contain Presets[].");
            return;
        }

        JsonElement selectedPreset = default;
        var foundPreset = false;

        foreach (var preset in presets.EnumerateArray())
        {
            if (GetInt(preset, "Index") == presetIndex)
            {
                selectedPreset = preset;
                foundPreset = true;
                break;
            }
        }

        if (!foundPreset)
        {
            Console.WriteLine($"Preset index not found: {presetIndex}");
            return;
        }

        var presetName = GetString(selectedPreset, "Name");

        if (!selectedPreset.TryGetProperty("Items", out var presetItems)
            || presetItems.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"Preset {presetIndex} does not contain Items[].");
            return;
        }

        var equipmentObject = BuildEquipmentObject(presetItems);

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

        foreach (var phaseNode in phasesArray)
        {
            if (phaseNode is not JsonObject phaseObject)
            {
                continue;
            }

            var phaseName = phaseObject["name"]?.GetValue<string>() ?? "";

            if (!string.IsNullOrWhiteSpace(phaseFilter)
                && !phaseName.Contains(phaseFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            phaseObject["equipment"] = equipmentObject.DeepClone();
            phaseObject["equipmentPresetIndex"] = presetIndex;
            phaseObject["equipmentPresetName"] = presetName;

            appliedCount++;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(scenarioOutPath, scenarioObject.ToJsonString(options));

        Console.WriteLine("Apply preset to scenario");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Preset file:   {presetPath}");
        Console.WriteLine($"Preset index:  {presetIndex}");
        Console.WriteLine($"Preset name:   {presetName}");
        Console.WriteLine($"Scenario in:   {scenarioInPath}");
        Console.WriteLine($"Scenario out:  {scenarioOutPath}");
        Console.WriteLine($"Phase filter:  {(string.IsNullOrWhiteSpace(phaseFilter) ? "(all phases)" : phaseFilter)}");
        Console.WriteLine($"Phases updated:{appliedCount}");
        Console.WriteLine($"Items applied: {equipmentObject.Count}");
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
}
