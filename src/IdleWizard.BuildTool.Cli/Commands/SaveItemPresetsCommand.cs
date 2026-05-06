using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveItemPresetsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --save-item-presets .\save_export.txt .\iw_workspace_vNext Temporalist [save_item_presets.json]");
            return;
        }

        var savePath = args[1];
        var workspacePath = args[2];
        var className = args[3];
        var outputPath = args.Length >= 5
            ? args[4]
            : ".\\save_item_presets.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var itemIndex = LoadItemIndex(workspacePath);
        var craftIndex = LoadCraftItemIndex(root);

        var presets = ParseClassPresets(root, className, itemIndex, craftIndex);

        var export = new
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            SaveFile = Path.GetFullPath(savePath),
            Workspace = workspacePath,
            ExportRoot = WorkspacePaths.ResolveExportRoot(workspacePath),
            ClassName = className,
            PresetCount = presets.Count,
            EquipmentPositionOrder = EquipmentPositions.Select(x => new
            {
                x.Position,
                x.DisplayName,
                x.ItemSlot
            }).ToList(),
            Presets = presets,
            Notes = new[]
            {
                "ItemPresets are parsed from save.ItemPresets.prime.<ClassName>.",
                "Each preset appears to contain 20 equipment positions.",
                "Tier and enchant are read from save.Craft.Items when the item exists there.",
                "A value of -1 means empty/no item in that position."
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

        Console.WriteLine("Save item presets");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Save:       {Path.GetFullPath(savePath)}");
        Console.WriteLine($"Workspace:  {workspacePath}");
        Console.WriteLine($"Class:      {className}");
        Console.WriteLine($"Presets:    {presets.Count}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
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

        if (string.IsNullOrWhiteSpace(raw))
        {
            return result;
        }

        var pattern = new Regex(
            @"#(?<index>\d+)#(?<name>[^@#]*)@(?<items>[^#]*)",
            RegexOptions.Compiled
        );

        foreach (Match match in pattern.Matches(raw))
        {
            var index = int.Parse(match.Groups["index"].Value);
            var name = match.Groups["name"].Value;
            var itemListRaw = match.Groups["items"].Value;

            var ids = itemListRaw
                .Split(';', StringSplitOptions.None)
                .Select(x => x.Trim())
                .ToList();

            var entries = new List<PresetItemExport>();

            for (var i = 0; i < EquipmentPositions.Count; i++)
            {
                var position = EquipmentPositions[i];
                var itemId = i < ids.Count ? ids[i] : "-1";

                itemIndex.TryGetValue(itemId, out var item);
                craftIndex.TryGetValue(itemId, out var craft);

                entries.Add(
                    new PresetItemExport(
                        i,
                        position.DisplayName,
                        position.ItemSlot,
                        itemId,
                        itemId != "-1",
                        item?.Name ?? "",
                        item?.Slot ?? "",
                        item?.Quality ?? "",
                        item?.Set ?? "",
                        craft?.Tier ?? 0,
                        craft?.Enchant ?? 0,
                        craft?.Favorite ?? false
                    )
                );
            }

            result.Add(
                new PresetExport(
                    index,
                    string.IsNullOrWhiteSpace(name) ? $"Preset {index}" : name,
                    ids.Count,
                    entries
                )
            );
        }

        return result
            .OrderBy(x => x.Index)
            .ToList();
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
            var id = Get(item, "ID");

            if (string.IsNullOrWhiteSpace(id))
            {
                id = Get(item, "Id");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                id = Get(item, "id");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var name = Get(item, "Name");

            if (string.IsNullOrWhiteSpace(name))
            {
                name = id;
            }

            var slot = forcedSlot ?? Get(item, "Slot");

            result[id] = new ItemOption(
                id,
                name,
                slot,
                Get(item, "Quality"),
                Get(item, "Set")
            );
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
                GetInt(item, "enchant"),
                GetBool(item, "fav")
            );
        }

        return result;
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
        int RawItemCount,
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
        string Quality,
        string Set,
        int Tier,
        int Enchant,
        bool Favorite
    );

    private sealed record ItemOption(
        string Id,
        string Name,
        string Slot,
        string Quality,
        string Set
    );

    private sealed record CraftItemInfo(
        int Tier,
        int Enchant,
        bool Favorite
    );
}


