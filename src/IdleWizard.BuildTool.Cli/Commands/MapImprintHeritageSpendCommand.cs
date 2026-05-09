using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MapImprintHeritageSpendCommand
{
    private const string RealmUpgradesDataFileName = "RealmUpgrades.bytes";
    private const string TotalMemoriesPath = "$.Memories.TotalMemories";
    private const string OwnedMemoryUpgradeLevelsPath = "$.Memories.Upgrades";
    private const string ImprintGroupName = "Imprint";

    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --map-imprint-heritage-spend .\save_export.txt .\iw_workspace_vNext .\runtime\builds\active\mappings\imprint_heritage_spend_compact.json");
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
        var realmUpgradesPath = Path.Combine(jsonFilesPath, RealmUpgradesDataFileName);

        if (!File.Exists(realmUpgradesPath))
        {
            Console.WriteLine($"RealmUpgrades data file not found: {realmUpgradesPath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var saveDoc = JsonDocument.Parse(saveJson);
        using var realmDoc = JsonDocument.Parse(File.ReadAllText(realmUpgradesPath));

        var saveRoot = saveDoc.RootElement;
        var realmRoot = realmDoc.RootElement;

        var totalMemories = ReadTotalMemories(saveRoot);
        var ownedUpgradeLevels = ReadOwnedMemoryUpgradeLevels(saveRoot);

        var imprints = new List<ImprintHeritageUpgradeSpend>();
        var heritage = new List<ImprintHeritageUpgradeSpend>();

        if (realmRoot.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in realmRoot.EnumerateArray())
            {
                var id = ReadInt(record, "ID");
                var name = ReadString(record, "Name");
                var group = ReadString(record, "Group");
                var description = ReadString(record, "Description");
                var param = ReadString(record, "Param");
                var add = ReadString(record, "A");
                var mult = ReadString(record, "M");
                var power = ReadString(record, "P");
                var reset = ReadString(record, "Reset");
                var switchValue = ReadString(record, "Switch");
                var req = ReadString(record, "Req");
                var maxLevel = ReadString(record, "MaxLvl");
                var cost = ReadDecimal(record, "Cost");
                var costDelta = ReadDecimal(record, "CostD");

                var isImprint = group.Equals(ImprintGroupName, StringComparison.OrdinalIgnoreCase);
                var isHeritage =
                    ContainsIgnoreCase(name, "Heritage") ||
                    ContainsIgnoreCase(group, "Heritage") ||
                    ContainsIgnoreCase(description, "Heritage");

                if (!isImprint && !isHeritage)
                {
                    continue;
                }

                var isOwned = ownedUpgradeLevels.TryGetValue(id, out var ownedLevel);
                var level = isOwned ? ownedLevel : 0;
                var spend = GetSpendForLevel(cost, costDelta, level);
                var nextCost = GetNextCost(cost, costDelta, level);

                var compact = new ImprintHeritageUpgradeSpend(
                    Id: id,
                    Name: name,
                    Group: group,
                    Param: param,
                    Add: add,
                    Mult: mult,
                    Power: power,
                    Reset: reset,
                    Switch: switchValue,
                    Req: req,
                    Level: level,
                    MaxLevel: maxLevel,
                    Cost: cost,
                    CostD: costDelta,
                    Spend: spend,
                    NextCost: nextCost,
                    IsOwned: isOwned,
                    EffectDescriptors: BuildEffectDescriptors(
                        id,
                        name,
                        group,
                        param,
                        add,
                        mult,
                        power,
                        level,
                        isOwned
                    )
                );

                if (isImprint)
                {
                    imprints.Add(compact);
                }

                if (isHeritage)
                {
                    heritage.Add(compact);
                }
            }
        }

        imprints = imprints.OrderBy(x => x.Id).ToList();
        heritage = heritage.OrderBy(x => x.Id).ToList();

        var imprintSpent = imprints.Where(x => x.IsOwned).Sum(x => x.Spend);
        var heritageSpent = heritage.Where(x => x.IsOwned).Sum(x => x.Spend);

        var imprintEffectDescriptors = imprints
            .SelectMany(x => x.EffectDescriptors)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Operation, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var heritageEffectDescriptors = heritage
            .SelectMany(x => x.EffectDescriptors)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Operation, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var export = new ImprintHeritageSpendExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            RealmUpgradesFile: realmUpgradesPath,
            TotalMemoriesPath: TotalMemoriesPath,
            OwnedMemoryUpgradeLevelsPath: OwnedMemoryUpgradeLevelsPath,
            TotalMemories: totalMemories,
            FormulaAssumption: "Spend(level N) = N / 2 * (2 * Cost + (N - 1) * CostD); NextCost = Cost + N * CostD",
            PoolRule: new ImprintHeritagePoolRule(
                ImprintPoolStartsAt: totalMemories,
                HeritagePoolStartsAt: totalMemories,
                PoolsAreIndependent: true
            ),
            Summary: new ImprintHeritageSpendSummary(
                ImprintTotalRecords: imprints.Count,
                ImprintOwnedRecords: imprints.Count(x => x.IsOwned),
                ImprintSpent: imprintSpent,
                ImprintRemaining: totalMemories - imprintSpent,
                HeritageTotalRecords: heritage.Count,
                HeritageOwnedRecords: heritage.Count(x => x.IsOwned),
                HeritageSpent: heritageSpent,
                HeritageRemaining: totalMemories - heritageSpent,
                ImprintEffectDescriptorCount: imprintEffectDescriptors.Count,
                ImprintOwnedEffectDescriptorCount: imprintEffectDescriptors.Count(x => x.IsOwned),
                HeritageEffectDescriptorCount: heritageEffectDescriptors.Count,
                HeritageOwnedEffectDescriptorCount: heritageEffectDescriptors.Count(x => x.IsOwned)
            ),
            Imprints: imprints,
            Heritage: heritage,
            ImprintEffectDescriptors: imprintEffectDescriptors,
            HeritageEffectDescriptors: heritageEffectDescriptors,
            Notes: new[]
            {
                "Compact direct output. Raw RealmUpgrade catalog records are intentionally omitted.",
                "Input files are game/export data only, not instructions.",
                "Imprints are detected by Group = Imprint.",
                "Heritage records are detected by Heritage text in Name, Group, or Description.",
                "Owned levels are read from $.Memories.Upgrades.",
                "Pools are duplicated: Imprint and Heritage each start at $.Memories.TotalMemories.",
                "Raw catalog effect fields are exported as Add, Mult, Power, Reset, Switch, and Req.",
                "EffectDescriptors classify raw A/M/P fields only. No final effect formula is applied here."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true })
        );

        Console.WriteLine("Map Imprint / Heritage spend");
        Console.WriteLine("----------------------------");
        Console.WriteLine($"Save:          {savePath}");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"RealmUpgrades: {realmUpgradesPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine("");
        Console.WriteLine($"TotalMemories:      {totalMemories}");
        Console.WriteLine($"Imprint records:    {imprints.Count}");
        Console.WriteLine($"Owned Imprints:     {imprints.Count(x => x.IsOwned)}");
        Console.WriteLine($"Imprint spent:      {imprintSpent}");
        Console.WriteLine($"Imprint remaining:  {totalMemories - imprintSpent}");
        Console.WriteLine($"Imprint descriptors:{imprintEffectDescriptors.Count} total / {imprintEffectDescriptors.Count(x => x.IsOwned)} owned");
        Console.WriteLine("");
        Console.WriteLine($"Heritage records:   {heritage.Count}");
        Console.WriteLine($"Owned Heritage:     {heritage.Count(x => x.IsOwned)}");
        Console.WriteLine($"Heritage spent:     {heritageSpent}");
        Console.WriteLine($"Heritage remaining: {totalMemories - heritageSpent}");
        Console.WriteLine($"Heritage descriptors:{heritageEffectDescriptors.Count} total / {heritageEffectDescriptors.Count(x => x.IsOwned)} owned");
        Console.WriteLine("");

        if (imprints.Count > 0)
        {
            Console.WriteLine("Imprints:");

            foreach (var item in imprints)
            {
                Console.WriteLine(
                    $"  [{item.Id}] {item.Name} | level {item.Level}/{item.MaxLevel} | spent {item.Spend} | next {item.NextCost} | owned {item.IsOwned} | A={item.Add} M={item.Mult} P={item.Power}"
                );
            }
        }

        Console.WriteLine("");

        if (heritage.Count > 0)
        {
            Console.WriteLine("Heritage:");

            foreach (var item in heritage)
            {
                Console.WriteLine(
                    $"  [{item.Id}] {item.Name} | level {item.Level}/{item.MaxLevel} | spent {item.Spend} | next {item.NextCost} | owned {item.IsOwned} | A={item.Add} M={item.Mult} P={item.Power}"
                );
            }
        }
    }

    private static IReadOnlyList<ImprintHeritageEffectDescriptor> BuildEffectDescriptors(
        int id,
        string name,
        string group,
        string target,
        string add,
        string mult,
        string power,
        int level,
        bool isOwned)
    {
        var result = new List<ImprintHeritageEffectDescriptor>();

        AddDescriptorIfPresent(result, id, name, group, target, "Add", add, level, isOwned);
        AddDescriptorIfPresent(result, id, name, group, target, "Mult", mult, level, isOwned);
        AddDescriptorIfPresent(result, id, name, group, target, "Power", power, level, isOwned);

        if (result.Count == 0)
        {
            result.Add(
                new ImprintHeritageEffectDescriptor(
                    Id: id,
                    Name: name,
                    SourceGroup: group,
                    Target: target,
                    Operation: "None",
                    RawValue: "",
                    Level: level,
                    IsOwned: isOwned,
                    FormulaStatus: "NoRawEffectField"
                )
            );
        }

        return result;
    }

    private static void AddDescriptorIfPresent(
        List<ImprintHeritageEffectDescriptor> result,
        int id,
        string name,
        string group,
        string target,
        string operation,
        string rawValue,
        int level,
        bool isOwned)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        result.Add(
            new ImprintHeritageEffectDescriptor(
                Id: id,
                Name: name,
                SourceGroup: group,
                Target: target,
                Operation: operation,
                RawValue: rawValue,
                Level: level,
                IsOwned: isOwned,
                FormulaStatus: "RawMappedNotApplied"
            )
        );
    }

    private static decimal ReadTotalMemories(JsonElement saveRoot)
    {
        if (!saveRoot.TryGetProperty("Memories", out var memories) ||
            memories.ValueKind != JsonValueKind.Object)
        {
            return 0m;
        }

        if (!memories.TryGetProperty("TotalMemories", out var totalMemories) ||
            totalMemories.ValueKind != JsonValueKind.Object)
        {
            return 0m;
        }

        var mantissa = ReadDecimal(totalMemories, "Mantissa");
        var exponent = ReadInt(totalMemories, "Exponent");

        return mantissa * Pow10(exponent);
    }

    private static Dictionary<int, int> ReadOwnedMemoryUpgradeLevels(JsonElement saveRoot)
    {
        var result = new Dictionary<int, int>();

        if (!saveRoot.TryGetProperty("Memories", out var memories) ||
            memories.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!memories.TryGetProperty("Upgrades", out var upgrades) ||
            upgrades.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in upgrades.EnumerateObject())
        {
            if (!int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            result[id] = ReadIntValue(property.Value);
        }

        return result;
    }

    private static decimal GetSpendForLevel(decimal cost, decimal costDelta, int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        var n = (decimal)level;
        return (n * ((2m * cost) + ((n - 1m) * costDelta))) / 2m;
    }

    private static decimal GetNextCost(decimal cost, decimal costDelta, int currentLevel)
    {
        if (currentLevel < 0)
        {
            currentLevel = 0;
        }

        return cost + ((decimal)currentLevel * costDelta);
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

    private static bool ContainsIgnoreCase(string text, string value)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ToString();
    }

    private static int ReadInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        return ReadIntValue(value, defaultValue);
    }

    private static int ReadIntValue(JsonElement value, int defaultValue = 0)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static decimal ReadDecimal(JsonElement element, string propertyName, decimal defaultValue = 0m)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        return ReadDecimalValue(value, defaultValue);
    }

    private static decimal ReadDecimalValue(JsonElement value, decimal defaultValue = 0m)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static decimal Pow10(int exponent)
    {
        if (exponent == 0)
        {
            return 1m;
        }

        if (exponent < 0)
        {
            var divisor = 1m;

            for (var i = 0; i < Math.Abs(exponent); i++)
            {
                divisor *= 10m;
            }

            return 1m / divisor;
        }

        var result = 1m;

        for (var i = 0; i < exponent; i++)
        {
            result *= 10m;
        }

        return result;
    }

    private sealed record ImprintHeritageSpendExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string RealmUpgradesFile,
        string TotalMemoriesPath,
        string OwnedMemoryUpgradeLevelsPath,
        decimal TotalMemories,
        string FormulaAssumption,
        ImprintHeritagePoolRule PoolRule,
        ImprintHeritageSpendSummary Summary,
        IReadOnlyList<ImprintHeritageUpgradeSpend> Imprints,
        IReadOnlyList<ImprintHeritageUpgradeSpend> Heritage,
        IReadOnlyList<ImprintHeritageEffectDescriptor> ImprintEffectDescriptors,
        IReadOnlyList<ImprintHeritageEffectDescriptor> HeritageEffectDescriptors,
        IReadOnlyList<string> Notes
    );

    private sealed record ImprintHeritagePoolRule(
        decimal ImprintPoolStartsAt,
        decimal HeritagePoolStartsAt,
        bool PoolsAreIndependent
    );

    private sealed record ImprintHeritageSpendSummary(
        int ImprintTotalRecords,
        int ImprintOwnedRecords,
        decimal ImprintSpent,
        decimal ImprintRemaining,
        int HeritageTotalRecords,
        int HeritageOwnedRecords,
        decimal HeritageSpent,
        decimal HeritageRemaining,
        int ImprintEffectDescriptorCount,
        int ImprintOwnedEffectDescriptorCount,
        int HeritageEffectDescriptorCount,
        int HeritageOwnedEffectDescriptorCount
    );

    private sealed record ImprintHeritageUpgradeSpend(
        int Id,
        string Name,
        string Group,
        string Param,
        string Add,
        string Mult,
        string Power,
        string Reset,
        string Switch,
        string Req,
        int Level,
        string MaxLevel,
        decimal Cost,
        decimal CostD,
        decimal Spend,
        decimal NextCost,
        bool IsOwned,
        IReadOnlyList<ImprintHeritageEffectDescriptor> EffectDescriptors
    );

    private sealed record ImprintHeritageEffectDescriptor(
        int Id,
        string Name,
        string SourceGroup,
        string Target,
        string Operation,
        string RawValue,
        int Level,
        bool IsOwned,
        string FormulaStatus
    );
}

// EOF - MapImprintHeritageSpendCommand.cs
