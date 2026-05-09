using System.IO.Compression;
using System.Text;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MapParamnesicEffectsCommand
{
    private const string DataFileName = "Paramnesics.bytes";
    private const string OwnedParamnesicsPath = "$.Realm.Paramnesics";

    private static readonly string[] SaveSearchTerms =
    {
        "Paramnesic",
        "Paramnesics",
        "Quasi",
        "QuasiRealm",
        "QuasiRealms",
        "Quasi-Realm",
        "Quasi-Realms"
    };

    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --map-paramnesic-effects .\save_export.txt .\iw_workspace_vNext .\paramnesic_effect_map.json");
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
        var dataFilePath = Path.Combine(jsonFilesPath, DataFileName);

        if (!File.Exists(dataFilePath))
        {
            Console.WriteLine($"Paramnesics data file not found: {dataFilePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var saveDoc = JsonDocument.Parse(saveJson);
        var saveRoot = saveDoc.RootElement;

        var catalog = LoadCatalog(dataFilePath);
        var saveCandidates = FindSaveCandidates(saveRoot);
        var ownedParamnesics = ReadOwnedParamnesics(saveRoot);
        var activeEffects = BuildActiveEffects(catalog, ownedParamnesics);

        var quasiPresetFields = saveCandidates
            .Where(x => x.Path.Equals("$.ItemPresets.quasi", StringComparison.OrdinalIgnoreCase)
                || x.Path.Equals("$.SpellPresets.quasi", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var otherSaveCandidates = saveCandidates
            .Where(x => !x.Path.Equals(OwnedParamnesicsPath, StringComparison.OrdinalIgnoreCase)
                && !quasiPresetFields.Any(q => q.Path.Equals(x.Path, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var export = new ParamnesicEffectMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            JsonFiles: jsonFilesPath,
            DataFile: dataFilePath,
            CatalogCount: catalog.Count,
            Catalog: catalog,
            OwnedParamnesicsPath: OwnedParamnesicsPath,
            OwnedParamnesicCount: ownedParamnesics.Count,
            OwnedParamnesics: ownedParamnesics,
            ActiveEffects: activeEffects,
            QuasiPresetFields: quasiPresetFields,
            OtherSaveCandidates: otherSaveCandidates,
            Notes: new[]
            {
                "Phase 2 Paramnesic effect map. Paramnesics are intentionally mapped separately from Memories, Heritage, and Imprints.",
                "Catalog records include raw effect fields: Param plus A/M/P, where present.",
                "ActiveEffects are only populated from $.Realm.Paramnesics when owned levels are present in the save.",
                "Quasi preset fields are separated from owned Paramnesics so loadout presets are not confused with upgrade ownership."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Map Paramnesic effects");
        Console.WriteLine("----------------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"DataFile:  {dataFilePath}");
        Console.WriteLine($"Output:    {outputPath}");
        Console.WriteLine("");
        Console.WriteLine($"Catalog records:       {catalog.Count}");
        Console.WriteLine($"Owned Paramnesics:      {ownedParamnesics.Count}");
        Console.WriteLine($"Active effects:         {activeEffects.Count}");
        Console.WriteLine($"Quasi preset fields:    {quasiPresetFields.Count}");
        Console.WriteLine($"Other candidate fields: {otherSaveCandidates.Count}");
        Console.WriteLine("");

        foreach (var record in catalog.OrderBy(x => x.Id))
        {
            Console.WriteLine($"[{record.Id}] {record.Name}");
            Console.WriteLine($"  Param:       {record.Param}");
            Console.WriteLine($"  Add:         {record.Add}");
            Console.WriteLine($"  Mult:        {record.Mult}");
            Console.WriteLine($"  Power:       {record.Power}");
            Console.WriteLine($"  MaxLvl:      {record.MaxLevel}");
            Console.WriteLine($"  Reset:       {record.Reset}");
            Console.WriteLine("");
        }

        if (ownedParamnesics.Count > 0)
        {
            Console.WriteLine("Owned Paramnesics:");
            foreach (var owned in ownedParamnesics)
            {
                Console.WriteLine($"  {owned.Id}: level {owned.Level}");
            }
        }
        else
        {
            Console.WriteLine($"No owned Paramnesics found at {OwnedParamnesicsPath}.");
        }
    }

    private static IReadOnlyList<ParamnesicCatalogRecord> LoadCatalog(string dataFilePath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(dataFilePath));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ParamnesicCatalogRecord>();
        }

        var records = new List<ParamnesicCatalogRecord>();
        var index = 0;

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var raw = ReadRawFields(element);
            records.Add(new ParamnesicCatalogRecord(
                Index: index,
                Id: GetInt(element, "ID"),
                Name: FirstNonEmpty(raw, "Name"),
                Description: FirstNonEmpty(raw, "Description"),
                Sprite: FirstNonEmpty(raw, "Sprite"),
                Param: FirstNonEmpty(raw, "Param"),
                Add: FirstNonEmpty(raw, "A"),
                Mult: FirstNonEmpty(raw, "M"),
                Power: FirstNonEmpty(raw, "P"),
                MaxLevel: FirstNonEmpty(raw, "MaxLvl"),
                Reset: FirstNonEmpty(raw, "Reset"),
                ResourcePool: "ParamnesicPoint",
                RawFields: raw
            ));
            index++;
        }

        return records.OrderBy(x => x.Id).ThenBy(x => x.Index).ToList();
    }

    private static IReadOnlyList<OwnedParamnesicRecord> ReadOwnedParamnesics(JsonElement saveRoot)
    {
        if (!saveRoot.TryGetProperty("Realm", out var realm) || realm.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<OwnedParamnesicRecord>();
        }

        if (!realm.TryGetProperty("Paramnesics", out var paramnesics) || paramnesics.ValueKind != JsonValueKind.Object)
        {
            return Array.Empty<OwnedParamnesicRecord>();
        }

        var owned = new List<OwnedParamnesicRecord>();

        foreach (var property in paramnesics.EnumerateObject())
        {
            if (!int.TryParse(property.Name, out var id))
            {
                continue;
            }

            owned.Add(new OwnedParamnesicRecord(
                Id: id,
                Level: ReadIntValue(property.Value),
                RawValue: property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : property.Value.ToString()
            ));
        }

        return owned.OrderBy(x => x.Id).ToList();
    }

    private static IReadOnlyList<ParamnesicActiveEffectRecord> BuildActiveEffects(IReadOnlyList<ParamnesicCatalogRecord> catalog, IReadOnlyList<OwnedParamnesicRecord> ownedParamnesics)
    {
        var result = new List<ParamnesicActiveEffectRecord>();

        foreach (var owned in ownedParamnesics)
        {
            var record = catalog.FirstOrDefault(x => x.Id == owned.Id);
            if (record is null)
            {
                result.Add(new ParamnesicActiveEffectRecord(
                    Id: owned.Id,
                    Name: string.Empty,
                    Level: owned.Level,
                    Param: string.Empty,
                    Add: string.Empty,
                    Mult: string.Empty,
                    Power: string.Empty,
                    MaxLevel: string.Empty,
                    ResourcePool: "ParamnesicPoint",
                    MappingStatus: "OwnedButCatalogRecordNotFound"
                ));
                continue;
            }

            result.Add(new ParamnesicActiveEffectRecord(
                Id: owned.Id,
                Name: record.Name,
                Level: owned.Level,
                Param: record.Param,
                Add: record.Add,
                Mult: record.Mult,
                Power: record.Power,
                MaxLevel: record.MaxLevel,
                ResourcePool: record.ResourcePool,
                MappingStatus: "OwnedCatalogRecordFound"
            ));
        }

        return result.OrderBy(x => x.Id).ToList();
    }

    private static IReadOnlyList<SaveCandidateField> FindSaveCandidates(JsonElement root)
    {
        var results = new List<SaveCandidateField>();
        WalkSave(root, "$", results);
        return results.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void WalkSave(JsonElement element, string path, List<SaveCandidateField> results)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = path == "$" ? "$." + property.Name : path + "." + property.Name;
                    if (SaveSearchTerms.Any(term => property.Name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    {
                        results.Add(new SaveCandidateField(
                            Path: propertyPath,
                            Name: property.Name,
                            ValueKind: property.Value.ValueKind.ToString(),
                            ValuePreview: Preview(property.Value),
                            Reason: "Property name contains Paramnesic/Quasi-Realm search term."
                        ));
                    }

                    WalkSave(property.Value, propertyPath, results);
                }
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    WalkSave(item, $"{path}[{index}]", results);
                    index++;
                }
                break;
        }
    }

    private static string Preview(JsonElement value)
    {
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
        text = text.Replace("\r", " ").Replace("\n", " ");
        return text.Length > 240 ? text[..240] + "..." : text;
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

    private static string FirstNonEmpty(IReadOnlyDictionary<string, string> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static int GetInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        return ReadIntValue(value);
    }

    private static int ReadIntValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private sealed record ParamnesicEffectMapExport(string GeneratedAtUtc, string SaveFile, string Workspace, string JsonFiles, string DataFile, int CatalogCount, IReadOnlyList<ParamnesicCatalogRecord> Catalog, string OwnedParamnesicsPath, int OwnedParamnesicCount, IReadOnlyList<OwnedParamnesicRecord> OwnedParamnesics, IReadOnlyList<ParamnesicActiveEffectRecord> ActiveEffects, IReadOnlyList<SaveCandidateField> QuasiPresetFields, IReadOnlyList<SaveCandidateField> OtherSaveCandidates, IReadOnlyList<string> Notes);
    private sealed record ParamnesicCatalogRecord(int Index, int Id, string Name, string Description, string Sprite, string Param, string Add, string Mult, string Power, string MaxLevel, string Reset, string ResourcePool, IReadOnlyDictionary<string, string> RawFields);
    private sealed record OwnedParamnesicRecord(int Id, int Level, string RawValue);
    private sealed record ParamnesicActiveEffectRecord(int Id, string Name, int Level, string Param, string Add, string Mult, string Power, string MaxLevel, string ResourcePool, string MappingStatus);
    private sealed record SaveCandidateField(string Path, string Name, string ValueKind, string ValuePreview, string Reason);
}

// EOF - MapParamnesicEffectsCommand.cs
