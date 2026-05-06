using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveSpellMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-spell-map .\save_export.txt .\iw_workspace_vNext [save_spell_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_spell_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var spellEnum = LoadEnumMap(workspacePath, "Spells.cs", "Spells");
        var spellCatalog = LoadSpellCatalog(workspacePath);

        var chosenSpellIds = ReadIntArray(root, "ChoosenSpells");
        var spellShards = ReadDoubleArray(root, "SpellShards");

        var otherSpellShards = ReadKeyValueArray(root, "OtherSpellShards");
        var spellUses = ReadKeyValueAverageArray(root, "SpellUses");
        var spellUsesTR = ReadKeyValueAverageArray(root, "SpellUsesTR");

        var allSpellIds = new HashSet<int>();

        foreach (var id in chosenSpellIds)
        {
            allSpellIds.Add(id);
        }

        foreach (var id in otherSpellShards.Keys)
        {
            allSpellIds.Add(id);
        }

        foreach (var id in spellUses.Keys)
        {
            allSpellIds.Add(id);
        }

        foreach (var id in spellUsesTR.Keys)
        {
            allSpellIds.Add(id);
        }

        var mappedSpells = new List<SaveSpellMapEntry>();

        foreach (var spellId in allSpellIds.OrderBy(x => x))
        {
            var enumKey = spellEnum.TryGetValue(spellId, out var key)
                ? key
                : spellId.ToString();

            spellCatalog.TryGetValue(enumKey, out var catalog);

            var spellbarPosition = chosenSpellIds.IndexOf(spellId);
            var isOnSpellbar = spellbarPosition >= 0;

            var spellbarShardValue = "";

            if (isOnSpellbar && spellbarPosition < spellShards.Count)
            {
                spellbarShardValue = spellShards[spellbarPosition].ToString("0.########");
            }

            var hasOtherShard = otherSpellShards.TryGetValue(spellId, out var otherShardValue);
            var hasUses = spellUses.TryGetValue(spellId, out var usesValue);
            var hasUsesTR = spellUsesTR.TryGetValue(spellId, out var usesTRValue);

            mappedSpells.Add(
                new SaveSpellMapEntry(
                    spellId,
                    enumKey,
                    catalog?.Name ?? enumKey,
                    catalog?.SpellType ?? "",
                    catalog?.TypeBehavior ?? "",
                    catalog?.LevelRequirement ?? "",
                    isOnSpellbar,
                    spellbarPosition,
                    spellbarShardValue,
                    hasOtherShard ? otherShardValue.ToString("0.########") : "",
                    hasUses ? usesValue.Value.ToString("0.########") : "",
                    hasUses ? usesValue.Average.ToString("0.########") : "",
                    hasUsesTR ? usesTRValue.Value.ToString("0.########") : "",
                    hasUsesTR ? usesTRValue.Average.ToString("0.########") : ""
                )
            );
        }

        var export = new SaveSpellMapExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            chosenSpellIds,
            mappedSpells,
            new[]
            {
                "ChoosenSpells is the current spellbar by Spells enum ID, confirmed by SaveData.prepare_shards().",
                "SpellShards aligns with ChoosenSpells by spellbar position, confirmed by SaveData.prepare_shards().",
                "OtherSpellShards is keyed by Spells enum ID, confirmed by SaveData.prepare_shards(). SpellUses and SpellUsesTR still need prepare_spells() confirmation.",
                "This map is developer/calculation data and should not be shown directly in the live import UI."
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

        Console.WriteLine("Save spell map");
        Console.WriteLine("--------------");
        Console.WriteLine($"Save:          {savePath}");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"Output:        {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Spellbar:      {chosenSpellIds.Count}");
        Console.WriteLine($"Mapped spells: {mappedSpells.Count}");
        Console.WriteLine("");
        Console.WriteLine("Spellbar:");

        foreach (var spell in mappedSpells.Where(x => x.IsOnSpellbar).OrderBy(x => x.SpellbarPosition))
        {
            Console.WriteLine(
                $"  [{spell.SpellbarPosition}] {spell.Name} ({spell.Key}) shards={spell.SpellbarShardValue} uses={spell.UsesValue}"
            );
        }
    }

    private static List<int> ReadIntArray(JsonElement root, string propertyName)
    {
        var result = new List<int>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number))
            {
                result.Add(number);
            }
            else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out number))
            {
                result.Add(number);
            }
        }

        return result;
    }

    private static List<double> ReadDoubleArray(JsonElement root, string propertyName)
    {
        var result = new List<double>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetDouble(out var number))
            {
                result.Add(number);
            }
            else if (item.ValueKind == JsonValueKind.String && double.TryParse(item.GetString(), out number))
            {
                result.Add(number);
            }
        }

        return result;
    }

    private static Dictionary<int, double> ReadKeyValueArray(JsonElement root, string propertyName)
    {
        var result = new Dictionary<int, double>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            var key = GetInt(item, "key");
            var amount = GetDouble(item, "value");

            result[key] = amount;
        }

        return result;
    }

    private static Dictionary<int, SaveSpellUseValue> ReadKeyValueAverageArray(JsonElement root, string propertyName)
    {
        var result = new Dictionary<int, SaveSpellUseValue>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            var key = GetInt(item, "key");
            var amount = GetDouble(item, "value");
            var average = GetDouble(item, "aver");

            result[key] = new SaveSpellUseValue(amount, average);
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

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
    }

    private static Dictionary<string, SpellCatalogEntry> LoadSpellCatalog(string workspacePath)
    {
        var result = new Dictionary<string, SpellCatalogEntry>(StringComparer.OrdinalIgnoreCase);
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
            var key = GetString(spell, "Key");

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = new SpellCatalogEntry(
                key,
                GetString(spell, "Name"),
                GetString(spell, "SpellType"),
                GetString(spell, "TypeBehavior"),
                GetString(spell, "Requirements")
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

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0.0;
    }

    private sealed record SaveSpellMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        IReadOnlyList<int> SpellbarIds,
        IReadOnlyList<SaveSpellMapEntry> Spells,
        IReadOnlyList<string> Notes
    );

    private sealed record SaveSpellMapEntry(
        int Id,
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        string LevelRequirement,
        bool IsOnSpellbar,
        int SpellbarPosition,
        string SpellbarShardValue,
        string OtherShardValue,
        string UsesValue,
        string UsesAverage,
        string UsesTRValue,
        string UsesTRAverage
    );

    private readonly record struct SaveSpellUseValue(
        double Value,
        double Average
    );

    private sealed record SpellCatalogEntry(
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        string LevelRequirement
    );
}



