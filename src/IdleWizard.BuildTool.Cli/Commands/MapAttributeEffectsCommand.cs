using System.IO.Compression;
using System.Text;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MapAttributeEffectsCommand
{
    private static readonly AttributeDefinition[] AttributeDefinitions =
    {
        new("Int", "Intelligence", "Intelligence.bytes"),
        new("Ins", "Insight", "Insight.bytes"),
        new("Scr", "Spellcraft", "Spellcraft.bytes"),
        new("Wis", "Wisdom", "Wisdom.bytes"),
        new("Dom", "Dominance", "Dominance.bytes"),
        new("Pat", "Patience", "Patience.bytes"),
        new("Mas", "Mastery", "Mastery.bytes"),
        new("Emp", "Empathy", "Empathy.bytes"),
        new("Ver", "Versatility", string.Empty),
    };

    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --map-attribute-effects .\save_export.txt .\iw_workspace_vNext .\attribute_effect_map.json");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = Path.GetFullPath(args[3]);

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var jsonFilesPath = WorkspacePaths.ResolveAssetJsonRoot(workspacePath);
        if (!Directory.Exists(jsonFilesPath))
        {
            Console.WriteLine($"jsonfiles folder not found: {jsonFilesPath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var saveDoc = JsonDocument.Parse(saveJson);
        var saveRoot = saveDoc.RootElement;

        var attributes = new List<AttributeEffectMap>();

        foreach (var definition in AttributeDefinitions)
        {
            attributes.Add(MapAttribute(saveRoot, jsonFilesPath, definition));
        }

        var export = new AttributeEffectMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            JsonFiles: jsonFilesPath,
            SaveAttributeSummary: new SaveAttributeSummary(
                AttTotal: GetInt(saveRoot, "AttTotal"),
                AttSearched: GetInt(saveRoot, "AttSearched"),
                AttFree: GetInt(saveRoot, "AttFree"),
                AttResets: GetInt(saveRoot, "AttResets"),
                AttProgress: GetDouble(saveRoot, "AttProgress", 0.0)
            ),
            Attributes: attributes,
            Notes: new[]
            {
                "Phase 2 attribute effect map. This command maps save attribute levels to raw attribute data records.",
                "Records with Level less than or equal to the save attribute value are listed as active/unlocked records.",
                "This command does not apply final VM/Burst calculations yet. It only produces source-confirmed attribute targets and raw formula fields."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Map attribute effects");
        Console.WriteLine("---------------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"JsonFiles: {jsonFilesPath}");
        Console.WriteLine($"Output:    {outputPath}");
        Console.WriteLine("");

        foreach (var attribute in attributes)
        {
            Console.WriteLine($"{attribute.DisplayName} [{attribute.SaveKey}]");
            Console.WriteLine($"  Save value:      {attribute.SaveValue}");
            Console.WriteLine($"  Data file:       {attribute.DataFile}");
            Console.WriteLine($"  File found:      {attribute.FileFound}");
            Console.WriteLine($"  Records:         {attribute.RecordCount}");
            Console.WriteLine($"  Active records:  {attribute.ActiveRecordCount}");
            Console.WriteLine($"  Targets:         {string.Join(", ", attribute.Targets)}");
            if (!string.IsNullOrWhiteSpace(attribute.Warning))
            {
                Console.WriteLine($"  Warning:         {attribute.Warning}");
            }
            Console.WriteLine("");
        }
    }

    private static AttributeEffectMap MapAttribute(JsonElement saveRoot, string jsonFilesPath, AttributeDefinition definition)
    {
        var saveValue = GetInt(saveRoot, definition.SaveKey);
        if (definition.SaveKey.Equals("Ver", StringComparison.OrdinalIgnoreCase))
        {
            var record = new AttributeEffectRecord(
                Level: 0,
                Target: "Base.AllBuildingsProfit",
                Description: "Versatility: increases profits by 1.80% per point; no perks.",
                Add: null,
                Mult: "1.018",
                Effect: null,
                Weight: "Char.Versatility",
                RawFields: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Level"] = "0",
                    ["Description"] = "Versatility: increases profits by 1.80% per point; no perks.",
                    ["Target"] = "Base.AllBuildingsProfit",
                    ["m"] = "1.018",
                    ["w"] = "Char.Versatility",
                    ["Source"] = "Manual special-case; no Versatility.bytes.txt file exists."
                }
            );

            return new AttributeEffectMap(
                SaveKey: definition.SaveKey,
                DisplayName: definition.DisplayName,
                DataFile: "No data file; special-case base attribute",
                FileFound: false,
                SaveValue: saveValue,
                RecordCount: 1,
                ActiveRecordCount: saveValue > 0 ? 1 : 0,
                Targets: saveValue > 0 ? new[] { "Base.AllBuildingsProfit" } : Array.Empty<string>(),
                Records: new[] { record },
                ActiveRecords: saveValue > 0 ? new[] { record } : Array.Empty<AttributeEffectRecord>(),
                Warning: string.Empty
            );
        }

        var filePath = Path.Combine(jsonFilesPath, definition.FileName);

        if (!File.Exists(filePath))
        {
            return new AttributeEffectMap(
                SaveKey: definition.SaveKey,
                DisplayName: definition.DisplayName,
                DataFile: definition.FileName,
                FileFound: false,
                SaveValue: saveValue,
                RecordCount: 0,
                ActiveRecordCount: 0,
                Targets: Array.Empty<string>(),
                Records: Array.Empty<AttributeEffectRecord>(),
                ActiveRecords: Array.Empty<AttributeEffectRecord>(),
                Warning: $"Attribute data file not found: {filePath}"
            );
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
        {
            return new AttributeEffectMap(
                SaveKey: definition.SaveKey,
                DisplayName: definition.DisplayName,
                DataFile: definition.FileName,
                FileFound: true,
                SaveValue: saveValue,
                RecordCount: 0,
                ActiveRecordCount: 0,
                Targets: Array.Empty<string>(),
                Records: Array.Empty<AttributeEffectRecord>(),
                ActiveRecords: Array.Empty<AttributeEffectRecord>(),
                Warning: "Attribute data file root was not an array."
            );
        }

        var records = new List<AttributeEffectRecord>();

        foreach (var element in root.EnumerateArray())
        {
            var record = new AttributeEffectRecord(
                Level: GetInt(element, "Level"),
                Target: GetString(element, "Target", string.Empty),
                Description: GetString(element, "Description", string.Empty),
                Add: GetNullableString(element, "a"),
                Mult: GetNullableString(element, "m"),
                Effect: GetNullableString(element, "e"),
                Weight: GetNullableString(element, "w"),
                RawFields: ReadRawFields(element)
            );

            records.Add(record);
        }

        var activeRecords = records
            .Where(x => x.Level <= saveValue)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var targets = activeRecords
            .Where(x => !string.IsNullOrWhiteSpace(x.Target))
            .Select(x => x.Target)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AttributeEffectMap(
            SaveKey: definition.SaveKey,
            DisplayName: definition.DisplayName,
            DataFile: definition.FileName,
            FileFound: true,
            SaveValue: saveValue,
            RecordCount: records.Count,
            ActiveRecordCount: activeRecords.Count,
            Targets: targets,
            Records: records.OrderBy(x => x.Level).ThenBy(x => x.Target, StringComparer.OrdinalIgnoreCase).ToList(),
            ActiveRecords: activeRecords,
            Warning: string.Empty
        );
    }

    private static IReadOnlyDictionary<string, string> ReadRawFields(JsonElement element)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return fields;
        }

        foreach (var property in element.EnumerateObject())
        {
            fields[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString();
        }

        return fields;
    }

    private static string DecodeSaveString(string input)
    {
        var clean = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        var bytes = Convert.FromBase64String(clean);
        using var inputStream = new MemoryStream(bytes);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static string GetString(JsonElement root, string propertyName, string defaultValue)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? defaultValue
            : defaultValue;
    }

    private static string? GetNullableString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.ToString();
    }

    private static int GetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static double GetDouble(JsonElement root, string propertyName, double defaultValue)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble();
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private sealed record AttributeDefinition(string SaveKey, string DisplayName, string FileName);
    private sealed record AttributeEffectMapExport(string GeneratedAtUtc, string SaveFile, string Workspace, string JsonFiles, SaveAttributeSummary SaveAttributeSummary, IReadOnlyList<AttributeEffectMap> Attributes, IReadOnlyList<string> Notes);
    private sealed record SaveAttributeSummary(int AttTotal, int AttSearched, int AttFree, int AttResets, double AttProgress);
    private sealed record AttributeEffectMap(string SaveKey, string DisplayName, string DataFile, bool FileFound, int SaveValue, int RecordCount, int ActiveRecordCount, IReadOnlyList<string> Targets, IReadOnlyList<AttributeEffectRecord> Records, IReadOnlyList<AttributeEffectRecord> ActiveRecords, string Warning);
    private sealed record AttributeEffectRecord(int Level, string Target, string Description, string? Add, string? Mult, string? Effect, string? Weight, IReadOnlyDictionary<string, string> RawFields);
}

// EOF - MapAttributeEffectsCommand.cs

