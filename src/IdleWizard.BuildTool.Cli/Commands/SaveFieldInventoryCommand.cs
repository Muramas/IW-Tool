using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveFieldInventoryCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --save-field-inventory .\save_export.txt [save_field_inventory.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var outputPath = args.Length >= 3
            ? args[2]
            : ".\\save_field_inventory.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine("Decoded save root is not a JSON object.");
            return;
        }

        var fields = new List<SaveFieldInventoryEntry>();

        foreach (var property in root.EnumerateObject())
        {
            var value = property.Value;
            var name = property.Name;

            var jsonKind = value.ValueKind.ToString();
            var arrayLength = value.ValueKind == JsonValueKind.Array
                ? value.GetArrayLength()
                : 0;

            var objectPropertyCount = value.ValueKind == JsonValueKind.Object
                ? value.EnumerateObject().Count()
                : 0;

            var preview = BuildPreview(value);
            var category = CategorizeField(name, value);
            var mappingStatus = GetMappingStatus(name);
            var priority = GetPriority(name, category, mappingStatus);
            var notes = GetNotes(name, value, category, mappingStatus);

            fields.Add(
                new SaveFieldInventoryEntry(
                    name,
                    jsonKind,
                    arrayLength,
                    objectPropertyCount,
                    category,
                    mappingStatus,
                    priority,
                    preview,
                    notes
                )
            );
        }

        var export = new SaveFieldInventoryExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            fields.Count,
            fields
                .OrderByDescending(x => SortPriority(x.Priority))
                .ThenBy(x => x.Category)
                .ThenBy(x => x.Name)
                .ToList(),
            new[]
            {
                "This inventory is for developer mapping, not live import UI.",
                "Mapped fields are already used or partially used by scenario generation.",
                "High-priority unmapped fields should be researched against game source next."
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

        Console.WriteLine("Save field inventory");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Output:    {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Fields:    {fields.Count}");
        Console.WriteLine("");

        foreach (var group in fields.GroupBy(x => x.Category).OrderBy(x => x.Key))
        {
            Console.WriteLine($"{group.Key}: {group.Count()}");
        }

        Console.WriteLine("");
        Console.WriteLine("High priority fields:");

        foreach (var field in fields.Where(x => x.Priority == "High").OrderBy(x => x.Name))
        {
            Console.WriteLine($"  {field.Name} [{field.MappingStatus}] - {field.Notes}");
        }
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

    private static string BuildPreview(JsonElement value)
    {
        try
        {
            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString() ?? "";
                return text.Length <= 120 ? text : text.Substring(0, 120) + "...";
            }

            if (value.ValueKind == JsonValueKind.Number
                || value.ValueKind == JsonValueKind.True
                || value.ValueKind == JsonValueKind.False
                || value.ValueKind == JsonValueKind.Null)
            {
                return value.GetRawText();
            }

            if (value.ValueKind == JsonValueKind.Array)
            {
                return "Array length " + value.GetArrayLength();
            }

            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = value.EnumerateObject()
                    .Select(x => x.Name)
                    .Take(8)
                    .ToList();

                return "Object properties: " + string.Join(", ", names);
            }

            return value.GetRawText();
        }
        catch
        {
            return "";
        }
    }

    private static string CategorizeField(string name, JsonElement value)
    {
        if (name.Equals("Hero", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Pet", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Hero", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Pet", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Class", StringComparison.OrdinalIgnoreCase))
        {
            return "Character/Pet";
        }

        if (name.Contains("Mana", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Soul", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Shard", StringComparison.OrdinalIgnoreCase)
            || name.Equals("EDE", StringComparison.OrdinalIgnoreCase))
        {
            return "Resources";
        }

        if (name.Contains("Spell", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ChoosenSpells", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Autocast", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Stance", StringComparison.OrdinalIgnoreCase))
        {
            return "Spells";
        }

        if (name.Contains("Item", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Craft", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Enchant", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Craft", StringComparison.OrdinalIgnoreCase))
        {
            return "Equipment/Crafting";
        }

        if (name.Contains("Upgrade", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Catalyst", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Challenge", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Trial", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Paragon", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Legacy", StringComparison.OrdinalIgnoreCase))
        {
            return "Progress/Unlocks";
        }

        if (name.Contains("Time", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Ascend", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Realm", StringComparison.OrdinalIgnoreCase))
        {
            return "Progress/Stats";
        }

        if (name.Contains("Sound", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Music", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Volume", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Particles", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Cursor", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Language", StringComparison.OrdinalIgnoreCase))
        {
            return "Settings/UI";
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return "Array/Unknown";
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return "Object/Unknown";
        }

        return "Scalar/Unknown";
    }

    private static string GetMappingStatus(string name)
    {
        var mapped = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Hero",
            "Pet",
            "PetMaxLevel",
            "PetMaxLevelAllTime",
            "HeroMaxLevelAllTime",
            "ManaAllTime",
            "Ascends",
            "AscendsRealm",
            "SaveTime",
            "ChoosenSpells",
            "ItemPresets",
            "Craft"
        };

        if (mapped.Contains(name))
        {
            return "MappedOrPartiallyMapped";
        }

        var likelyFormulaRelevant = new[]
        {
            "EDE",
            "Upgrades",
            "Catalysts",
            "BuildingLevels",
            "ClassTime",
            "ManaRealm",
            "ManaSession",
            "VoidManaRealm",
            "VoidManaSession",
            "SpellUses",
            "SpellUsesTR",
            "AccumCasts",
            "ShardsPool",
            "SpellShards",
            "OtherSpellShards"
        };

        if (likelyFormulaRelevant.Any(x => name.Equals(x, StringComparison.OrdinalIgnoreCase)))
        {
            return "NeedsMapping";
        }

        return "Unmapped";
    }

    private static string GetPriority(string name, string category, string mappingStatus)
    {
        if (mappingStatus == "NeedsMapping")
        {
            return "High";
        }

        if (category == "Equipment/Crafting"
            || category == "Progress/Unlocks"
            || category == "Spells")
        {
            return "Medium";
        }

        if (mappingStatus == "MappedOrPartiallyMapped")
        {
            return "Medium";
        }

        return "Low";
    }

    private static int SortPriority(string priority)
    {
        return priority switch
        {
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
    }

    private static string GetNotes(
        string name,
        JsonElement value,
        string category,
        string mappingStatus)
    {
        if (name.Equals("Upgrades", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely important for unlocked modifiers. Needs upgrade ID lookup.";
        }

        if (name.Equals("Catalysts", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely progression/formula relevant. Needs source mapping.";
        }

        if (name.Equals("EDE", StringComparison.OrdinalIgnoreCase))
        {
            return "Possibly experiment/dust/efficiency related. Needs exact source mapping.";
        }

        if (name.Equals("Craft", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains item tier/enchant inventory and crafting progression.";
        }

        if (name.Equals("ItemPresets", StringComparison.OrdinalIgnoreCase))
        {
            return "Contains user-defined equipment presets by class.";
        }

        if (name.Equals("ChoosenSpells", StringComparison.OrdinalIgnoreCase))
        {
            return "Current spellbar. Phase assignment should remain user-controlled.";
        }

        if (mappingStatus == "MappedOrPartiallyMapped")
        {
            return "Already mapped or partially mapped by import tooling.";
        }

        if (category == "Settings/UI")
        {
            return "Probably not relevant to calculation.";
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return "Array field. Needs shape inspection if category is formula relevant.";
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            return "Object field. Needs property-level mapping if category is formula relevant.";
        }

        return "No mapping yet.";
    }

    private sealed record SaveFieldInventoryExport(
        string GeneratedAtUtc,
        string SaveFile,
        int FieldCount,
        IReadOnlyList<SaveFieldInventoryEntry> Fields,
        IReadOnlyList<string> Notes
    );

    private sealed record SaveFieldInventoryEntry(
        string Name,
        string JsonKind,
        int ArrayLength,
        int ObjectPropertyCount,
        string Category,
        string MappingStatus,
        string Priority,
        string Preview,
        string Notes
    );
}
