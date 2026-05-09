using System.IO.Compression;
using System.Text;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MapMemoryEffectsCommand
{
    private static readonly string[] CandidateDataFiles =
    {
        "RealmUpgrades.bytes"
    };

    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --map-memory-effects .\save_export.txt .\iw_workspace_vNext .\memory_effect_map.json");
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

        var memoryState = ReadMemoryState(saveRoot);
        var candidateIndex = LoadCandidateRecords(jsonFilesPath);
        var mappedUpgrades = MapMemoryUpgrades(memoryState.Upgrades, candidateIndex);

        var export = new MemoryEffectMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            JsonFiles: jsonFilesPath,
            State: memoryState,
            UpgradeMaps: mappedUpgrades,
            DataFilesSearched: CandidateDataFiles,
            Notes: new[]
            {
                "Phase 2 memory effect map. This command maps the save Memories object and finds source/data candidate records for memory upgrade IDs.",
                "Candidate records are not treated as confirmed formulas until the source file and field meaning are verified.",
                "CarryOver.persist, CarryOver.catas, and CarryOver.echoExp are exported as raw dictionaries for later effect binding."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Map memory effects");
        Console.WriteLine("------------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"JsonFiles: {jsonFilesPath}");
        Console.WriteLine($"Output:    {outputPath}");
        Console.WriteLine("");
        Console.WriteLine($"Realms:          {memoryState.Realms}");
        Console.WriteLine($"Memories:        {memoryState.Memories}");
        Console.WriteLine($"SwitchMemories:  {memoryState.SwitchMemories}");
        Console.WriteLine($"TotalMemories:   {memoryState.TotalMemories}");
        Console.WriteLine($"Upgrades:        {memoryState.Upgrades.Count}");
        Console.WriteLine($"Memory sets:     {memoryState.MemorySets.Count}");
        Console.WriteLine($"CarryOver persist: {memoryState.CarryOver.Persist.Count}");
        Console.WriteLine($"CarryOver catas:   {memoryState.CarryOver.Catas.Count}");
        Console.WriteLine($"CarryOver echoExp: {memoryState.CarryOver.EchoExp.Count}");
        Console.WriteLine("");

        foreach (var map in mappedUpgrades.OrderBy(x => x.UpgradeId))
        {
            Console.WriteLine($"Upgrade {map.UpgradeId}: level {map.Level}, candidates {map.Candidates.Count}");
        }
    }

    private static MemoryState ReadMemoryState(JsonElement saveRoot)
    {
        if (!saveRoot.TryGetProperty("Memories", out var memories) || memories.ValueKind != JsonValueKind.Object)
        {
            return MemoryState.Empty("Memories object missing from save.");
        }

        var upgrades = new Dictionary<int, int>();
        if (memories.TryGetProperty("Upgrades", out var upgradesElement) && upgradesElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in upgradesElement.EnumerateObject())
            {
                if (int.TryParse(property.Name, out var id))
                {
                    upgrades[id] = ReadIntValue(property.Value);
                }
            }
        }

        var memorySets = new List<MemorySetSummary>();
        if (memories.TryGetProperty("MemorySets", out var setsElement) && setsElement.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var set in setsElement.EnumerateArray())
            {
                memorySets.Add(new MemorySetSummary(index, set.ValueKind.ToString(), set.ToString()));
                index++;
            }
        }

        var carryOver = memories.TryGetProperty("CarryOver", out var carryElement) && carryElement.ValueKind == JsonValueKind.Object
            ? ReadCarryOver(carryElement)
            : CarryOverMap.Empty();

        return new MemoryState(
            Realms: GetInt(memories, "Realms"),
            Memories: ReadBigNumberScientific(memories, "Memories"),
            SwitchMemories: ReadBigNumberScientific(memories, "SwitchMemories"),
            TotalMemories: ReadBigNumberScientific(memories, "TotalMemories"),
            HeritageMemoryPool: ReadBigNumberScientific(memories, "TotalMemories"),
            ImprintMemoryPool: ReadBigNumberScientific(memories, "TotalMemories"),
            Upgrades: upgrades.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value),
            MemorySets: memorySets,
            CarryOver: carryOver,
            Warning: string.Empty
        );
    }

    private static CarryOverMap ReadCarryOver(JsonElement carryElement)
    {
        return new CarryOverMap(
            Breakthroughs: GetInt(carryElement, "breakthroughs"),
            Trophies: GetInt(carryElement, "trophies"),
            Attributes: GetInt(carryElement, "attributes"),
            Persist: ReadObjectDictionary(carryElement, "persist"),
            Catas: ReadObjectDictionary(carryElement, "catas"),
            EchoExp: ReadObjectDictionary(carryElement, "echoExp"),
            RawFields: ReadRawFields(carryElement)
        );
    }

    private static IReadOnlyDictionary<string, string> ReadObjectDictionary(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(propertyName, out var obj) || obj.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in obj.EnumerateObject())
        {
            result[property.Name] = ReadElementAsString(property.Value);
        }

        return result.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<CandidateDataRecord> LoadCandidateRecords(string jsonFilesPath)
    {
        var candidates = new List<CandidateDataRecord>();

        foreach (var fileName in CandidateDataFiles)
        {
            var path = Path.Combine(jsonFilesPath, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var index = 0;
            foreach (var record in doc.RootElement.EnumerateArray())
            {
                var raw = ReadRawFields(record);
                var id = TryReadId(record);

                if (id is not null)
                {
                    candidates.Add(new CandidateDataRecord(
                        DataFile: fileName,
                        Index: index,
                        Id: id.Value,
                        Name: FirstNonEmpty(raw, "Name", "name", "Key", "Description"),
                        Group: FirstNonEmpty(raw, "Group", "group", "Class", "Switch"),
                        Target: FirstNonEmpty(raw, "Param", "V", "Target", "T"),
                        Add: FirstNonEmpty(raw, "A", "Addendum", "a", "RewardA"),
                        Mult: FirstNonEmpty(raw, "M", "Multiplier", "m", "RewardM"),
                        Param: FirstNonEmpty(raw, "Param", "P", "ReqParam"),
                        MaxLevel: FirstNonEmpty(raw, "MaxLvl", "Level"),
                        Description: FirstNonEmpty(raw, "Description", "RewardDescr", "ReqDescr"),
                        CandidateKind: GetCandidateKind(fileName),
                        ResourcePool: GetResourcePool(fileName, raw),
                        RealmUpgradeGroup: GetRealmUpgradeGroup(fileName, raw),
                        IsHeritage: IsHeritage(fileName, raw),
                        IsImprint: IsImprint(fileName, raw),
                        IsParamnesic: IsParamnesic(fileName),
                        RawFields: raw
                    ));
                }

                index++;
            }
        }

        return candidates;
    }

    private static IReadOnlyList<MemoryUpgradeMap> MapMemoryUpgrades(IReadOnlyDictionary<int, int> upgrades, IReadOnlyList<CandidateDataRecord> candidates)
    {
        var result = new List<MemoryUpgradeMap>();

        foreach (var upgrade in upgrades.OrderBy(x => x.Key))
        {
            var matchedCandidates = candidates
                .Where(x => x.Id == upgrade.Key)
                .OrderBy(x => x.DataFile, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Index)
                .ToList();

            result.Add(new MemoryUpgradeMap(
                UpgradeId: upgrade.Key,
                Level: upgrade.Value,
                Candidates: matchedCandidates,
                MappingStatus: matchedCandidates.Count == 0 ? "NoCandidateRecordFound" : "CandidateRecordFound"
            ));
        }

        return result;
    }

    private static string GetCandidateKind(string dataFile)
    {
        if (IsParamnesic(dataFile))
        {
            return "Paramnesic";
        }

        if (dataFile.Equals("RealmUpgrades.bytes", StringComparison.OrdinalIgnoreCase))
        {
            return "RealmUpgrade";
        }

        return "Unknown";
    }

    private static string GetResourcePool(string dataFile, IReadOnlyDictionary<string, string> fields)
    {
        if (IsParamnesic(dataFile))
        {
            return "ParamnesicPoint";
        }

        if (IsImprint(dataFile, fields))
        {
            return "ImprintMemories";
        }

        if (IsHeritage(dataFile, fields))
        {
            return "HeritageMemories";
        }

        return "Unknown";
    }

    private static string GetRealmUpgradeGroup(string dataFile, IReadOnlyDictionary<string, string> fields)
    {
        if (!dataFile.Equals("RealmUpgrades.bytes", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return FirstNonEmpty(fields, "Group", "group");
    }

    private static bool IsParamnesic(string dataFile)
    {
        return dataFile.Equals("Paramnesics.bytes", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImprint(string dataFile, IReadOnlyDictionary<string, string> fields)
    {
        if (!dataFile.Equals("RealmUpgrades.bytes", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var group = GetRealmUpgradeGroup(dataFile, fields);

        return group.Equals("Imprint", StringComparison.OrdinalIgnoreCase)
            || fields.ContainsKey("Switch");
    }

    private static bool IsHeritage(string dataFile, IReadOnlyDictionary<string, string> fields)
    {
        if (!dataFile.Equals("RealmUpgrades.bytes", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !IsImprint(dataFile, fields);
    }
    private static int? TryReadId(JsonElement record)
    {
        foreach (var key in new[] { "ID", "Id", "id", "Key", "Level" })
        {
            if (!record.TryGetProperty(key, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
            {
                return intValue;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
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

    private static IReadOnlyDictionary<string, string> ReadRawFields(JsonElement element)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (element.ValueKind != JsonValueKind.Object)
        {
            return fields;
        }

        foreach (var property in element.EnumerateObject())
        {
            fields[property.Name] = ReadElementAsString(property.Value);
        }

        return fields;
    }

    private static string ReadElementAsString(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : value.ToString();
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

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return string.Empty;
        if (value.ValueKind == JsonValueKind.Number) return value.GetDouble().ToString("G17");
        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out var mantissa)
            && value.TryGetProperty("Exponent", out var exponent))
        {
            return $"{mantissa.GetDouble():G17}e{exponent.GetInt32()}";
        }
        return value.ToString();
    }

    private static int GetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return defaultValue;
        return ReadIntValue(value);
    }

    private sealed record MemoryEffectMapExport(string GeneratedAtUtc, string SaveFile, string Workspace, string JsonFiles, MemoryState State, IReadOnlyList<MemoryUpgradeMap> UpgradeMaps, IReadOnlyList<string> DataFilesSearched, IReadOnlyList<string> Notes);
    private sealed record MemoryState(int Realms, string Memories, string SwitchMemories, string TotalMemories, string HeritageMemoryPool, string ImprintMemoryPool, IReadOnlyDictionary<int, int> Upgrades, IReadOnlyList<MemorySetSummary> MemorySets, CarryOverMap CarryOver, string Warning)
    {
        public static MemoryState Empty(string warning) => new(0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, new Dictionary<int, int>(), Array.Empty<MemorySetSummary>(), CarryOverMap.Empty(), warning);
    }
    private sealed record MemorySetSummary(int Index, string ValueKind, string RawValue);
    private sealed record CarryOverMap(int Breakthroughs, int Trophies, int Attributes, IReadOnlyDictionary<string, string> Persist, IReadOnlyDictionary<string, string> Catas, IReadOnlyDictionary<string, string> EchoExp, IReadOnlyDictionary<string, string> RawFields)
    {
        public static CarryOverMap Empty() => new(0, 0, 0, new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>(), new Dictionary<string, string>());
    }
    private sealed record MemoryUpgradeMap(int UpgradeId, int Level, IReadOnlyList<CandidateDataRecord> Candidates, string MappingStatus);
    private sealed record CandidateDataRecord(string DataFile, int Index, int Id, string Name, string Group, string Target, string Add, string Mult, string Param, string MaxLevel, string Description, string CandidateKind, string ResourcePool, string RealmUpgradeGroup, bool IsHeritage, bool IsImprint, bool IsParamnesic, IReadOnlyDictionary<string, string> RawFields);
}

// EOF - MapMemoryEffectsCommand.cs



