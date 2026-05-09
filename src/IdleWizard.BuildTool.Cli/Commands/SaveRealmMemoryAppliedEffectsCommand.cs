using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveRealmMemoryAppliedEffectsCommand
{
    private const string RealmUpgradesDataFileName = "RealmUpgrades.bytes";

    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --save-realm-memory-applied-effects .\save_export.txt .\iw_workspace_vNext [.\realm_memory_applied_effects.json] [.\realm_memory_owned_target_summary.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];

        var detailOutputPath = args.Length >= 4
            ? Path.GetFullPath(args[3])
            : Path.GetFullPath(@".\realm_memory_applied_effects.json");

        var summaryOutputPath = args.Length >= 5
            ? Path.GetFullPath(args[4])
            : Path.GetFullPath(@".\realm_memory_owned_target_summary.json");

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var realmUpgradesPath = FindRealmUpgradesFile(workspacePath);

        if (string.IsNullOrWhiteSpace(realmUpgradesPath) || !File.Exists(realmUpgradesPath))
        {
            Console.WriteLine($"RealmUpgrades data file not found under workspace: {workspacePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var saveDoc = JsonDocument.Parse(saveJson);
        using var realmDoc = JsonDocument.Parse(File.ReadAllText(realmUpgradesPath));

        var saveRoot = saveDoc.RootElement;
        var realmRoot = realmDoc.RootElement;

        var ownedLevels = ReadOwnedMemoryUpgradeLevels(saveRoot);

        var allEffects = new List<RealmMemoryAppliedEffect>();

        if (realmRoot.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in realmRoot.EnumerateArray())
            {
                var param = ReadString(record, "Param");

                if (string.IsNullOrWhiteSpace(param))
                {
                    continue;
                }

                var id = ReadInt(record, "ID");
                var isOwned = ownedLevels.TryGetValue(id, out var ownedLevel);
                var level = isOwned ? ownedLevel : 0;

                allEffects.Add(BuildAppliedEffect(record, level, isOwned));
            }
        }

        allEffects = allEffects
            .OrderBy(x => x.Id)
            .ToList();

        var ownedEffects = allEffects
            .Where(x => x.IsOwned)
            .OrderBy(x => x.Id)
            .ToList();

        var targetAggregates = BuildTargetAggregates(allEffects);
        var ownedTargetAggregates = targetAggregates
            .Where(x => x.OwnedEffectCount > 0)
            .OrderBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var baseProfit = ownedTargetAggregates
            .FirstOrDefault(x => x.Target.Equals("Base.AllBuildingsProfit", StringComparison.OrdinalIgnoreCase));

        var realmIncome = ownedTargetAggregates
            .FirstOrDefault(x => x.Target.Equals("Realm.Income", StringComparison.OrdinalIgnoreCase));

        var detailExport = new RealmMemoryAppliedEffectsExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            RealmUpgradesFile: realmUpgradesPath,
            OutputFile: detailOutputPath,
            SourceEvidence: new RealmMemorySourceEvidence(
                RealmUpgrade: "RealmUpgrade maps A to SimpleEffect add=A mult=0; M to add=0 mult=M; P to PowIntW add=1 mult=P; parameter is RealmUpgrade.Level.",
                EffectFactoryLinear: "Linear with parameter w applies v.Change(a * w.Value, 1.0 + m * w.Value).",
                EffectFactoryPowIntW: "PowIntW applies v.Change(0.0, a * (m + 1.0).Pow(num))."
            ),
            Summary: new RealmMemoryAppliedEffectsSummary(
                CatalogEffectCount: allEffects.Count,
                OwnedEffectCount: ownedEffects.Count,
                TargetCount: targetAggregates.Count,
                OwnedTargetCount: ownedTargetAggregates.Count,
                BaseAllBuildingsProfitOwnedMultiplier: baseProfit?.OwnedMultiplierProduct ?? 1m,
                BaseAllBuildingsProfitOwnedBonusPercent: baseProfit?.OwnedBonusPercent ?? 0m,
                RealmIncomeOwnedMultiplier: realmIncome?.OwnedMultiplierProduct ?? 1m,
                RealmIncomeOwnedBonusPercent: realmIncome?.OwnedBonusPercent ?? 0m,
                FormulaStatus: "SourceConfirmedApplied"
            ),
            AllAppliedEffects: allEffects,
            OwnedAppliedEffects: ownedEffects,
            TargetAggregates: targetAggregates,
            OwnedTargetAggregates: ownedTargetAggregates,
            Notes: new[]
            {
                "This file applies source-confirmed RealmUpgrade formulas for all RealmUpgrades with non-empty Param.",
                "A uses Linear parameterized additive formula: AppliedAdd = A * Level.",
                "M uses Linear parameterized multiplier formula: AppliedMultiplier = 1 + M * Level.",
                "P uses PowIntW formula: AppliedMultiplier = (1 + P) ^ Level.",
                "Aggregates multiply owned multipliers and sum owned additive effects by target.",
                "Req is retained but unlock recommendation logic is not applied here.",
                "This file includes all catalog effects and marks which ones are owned by the imported save."
            }
        );

        var compactTargets = ownedTargetAggregates
            .Select(x => new RealmMemoryOwnedTargetSummaryEntry(
                Target: x.Target,
                OwnedEffectCount: x.OwnedEffectCount,
                OwnedAdditiveSum: x.OwnedAdditiveSum,
                OwnedMultiplierProduct: x.OwnedMultiplierProduct,
                OwnedBonusPercent: x.OwnedBonusPercent,
                OwnedOperations: x.OwnedOperations,
                OwnedGroups: x.OwnedGroups
            ))
            .OrderBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var compactExport = new RealmMemoryOwnedTargetSummaryExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SourceFile: detailOutputPath,
            OutputFile: summaryOutputPath,
            Summary: new RealmMemoryOwnedTargetSummary(
                OwnedTargetCount: compactTargets.Count,
                BaseAllBuildingsProfitOwnedMultiplier: baseProfit?.OwnedMultiplierProduct ?? 1m,
                BaseAllBuildingsProfitOwnedBonusPercent: baseProfit?.OwnedBonusPercent ?? 0m,
                RealmIncomeOwnedMultiplier: realmIncome?.OwnedMultiplierProduct ?? 1m,
                RealmIncomeOwnedBonusPercent: realmIncome?.OwnedBonusPercent ?? 0m,
                FormulaStatus: "SourceConfirmedApplied"
            ),
            OwnedTargetAggregates: compactTargets,
            Notes: new[]
            {
                "Compact calculator-facing summary of owned Realm memory applied effects.",
                "Source data comes from the realm memory applied effects detail output file.",
                "OwnedAdditiveSum is summed by target.",
                "OwnedMultiplierProduct is multiplied by target.",
                "A/M/P formulas are source-confirmed through RealmUpgrade and EffectFactory.",
                "This file intentionally omits per-upgrade diagnostic details."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(detailOutputPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(summaryOutputPath)!);

        var options = new JsonSerializerOptions { WriteIndented = true };

        File.WriteAllText(detailOutputPath, JsonSerializer.Serialize(detailExport, options));
        File.WriteAllText(summaryOutputPath, JsonSerializer.Serialize(compactExport, options));

        Console.WriteLine("Realm memory applied effects");
        Console.WriteLine("----------------------------");
        Console.WriteLine($"Save:          {savePath}");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"RealmUpgrades: {realmUpgradesPath}");
        Console.WriteLine($"DetailOutput:  {detailOutputPath}");
        Console.WriteLine($"SummaryOutput: {summaryOutputPath}");
        Console.WriteLine("");
        Console.WriteLine($"CatalogEffectCount: {detailExport.Summary.CatalogEffectCount}");
        Console.WriteLine($"OwnedEffectCount:   {detailExport.Summary.OwnedEffectCount}");
        Console.WriteLine($"TargetCount:        {detailExport.Summary.TargetCount}");
        Console.WriteLine($"OwnedTargetCount:   {detailExport.Summary.OwnedTargetCount}");
        Console.WriteLine("");
        Console.WriteLine($"BaseAllBuildingsProfitOwnedMultiplier:   {detailExport.Summary.BaseAllBuildingsProfitOwnedMultiplier}");
        Console.WriteLine($"BaseAllBuildingsProfitOwnedBonusPercent: {detailExport.Summary.BaseAllBuildingsProfitOwnedBonusPercent}");
        Console.WriteLine("");
        Console.WriteLine($"RealmIncomeOwnedMultiplier:              {detailExport.Summary.RealmIncomeOwnedMultiplier}");
        Console.WriteLine($"RealmIncomeOwnedBonusPercent:            {detailExport.Summary.RealmIncomeOwnedBonusPercent}");
        Console.WriteLine("");
        Console.WriteLine($"FormulaStatus: {detailExport.Summary.FormulaStatus}");
    }

    private static RealmMemoryAppliedEffect BuildAppliedEffect(JsonElement record, int level, bool isOwned)
    {
        var id = ReadInt(record, "ID");
        var name = ReadString(record, "Name");
        var group = ReadString(record, "Group");
        var target = ReadString(record, "Param");
        var description = ReadString(record, "Description");

        var addText = ReadString(record, "A");
        var multText = ReadString(record, "M");
        var powerText = ReadString(record, "P");

        var maxLevelText = ReadString(record, "MaxLvl");
        var cost = ReadDecimal(record, "Cost");
        var costD = ReadDecimal(record, "CostD");
        var req = ReadString(record, "Req");
        var reset = ReadString(record, "Reset");
        var switchValue = ReadString(record, "Switch");

        var operation = "None";
        var rawValue = "";
        var runtimeEffectName = "None";
        var appliedAdd = 0m;
        var appliedMultiplier = 1m;
        var formula = "No supported A/M/P field found.";
        var formulaSource = "No formula applied.";
        var formulaStatus = "NoSupportedEffectField";

        if (!string.IsNullOrWhiteSpace(addText))
        {
            operation = "Add";
            rawValue = addText;
            runtimeEffectName = "Linear";

            var add = ParseDecimal(addText);
            appliedAdd = add * level;
            appliedMultiplier = 1m;

            formula = "AppliedAdd = A * Level; AppliedMultiplier = 1";
            formulaSource = "RealmUpgrade maps A to SimpleEffect add=A, mult=0, parameter=Level. EffectFactory.Linear applies v.Change(a * w.Value, 1.0 + m * w.Value).";
            formulaStatus = "SourceConfirmedApplied";
        }
        else if (!string.IsNullOrWhiteSpace(multText))
        {
            operation = "Mult";
            rawValue = multText;
            runtimeEffectName = "Linear";

            var mult = ParseDecimal(multText);
            appliedAdd = 0m;
            appliedMultiplier = 1m + (mult * level);

            formula = "AppliedAdd = 0; AppliedMultiplier = 1 + M * Level";
            formulaSource = "RealmUpgrade maps M to SimpleEffect add=0, mult=M, parameter=Level. EffectFactory.Linear applies v.Change(a * w.Value, 1.0 + m * w.Value).";
            formulaStatus = "SourceConfirmedApplied";
        }
        else if (!string.IsNullOrWhiteSpace(powerText))
        {
            operation = "Power";
            rawValue = powerText;
            runtimeEffectName = "PowIntW";

            var power = ParseDouble(powerText);
            appliedAdd = 0m;
            appliedMultiplier = (decimal)Math.Pow(1d + power, level);

            formula = "AppliedAdd = 0; AppliedMultiplier = (1 + P) ^ Level";
            formulaSource = "RealmUpgrade maps P to PowIntW SimpleEffect add=1, mult=P, parameter=Level. EffectFactory.PowerIntW applies v.Change(0.0, a * (m + 1.0).Pow(num)).";
            formulaStatus = "SourceConfirmedApplied";
        }

        var bonusPercent = (appliedMultiplier - 1m) * 100m;
        var spend = GetSpendForLevel(cost, costD, level);
        var nextCost = GetNextCost(cost, costD, level);

        var maxLevel = ParseInt(maxLevelText);
        int? levelsToMax = maxLevel > 0 ? Math.Max(0, maxLevel - level) : null;
        var isMaxed = maxLevel > 0 && level >= maxLevel;

        return new RealmMemoryAppliedEffect(
            Id: id,
            Name: name,
            Group: group,
            Description: description,
            Target: target,
            Operation: operation,
            RawValue: rawValue,
            Level: level,
            MaxLevel: maxLevelText,
            LevelsToMax: levelsToMax,
            IsMaxed: isMaxed,
            IsOwned: isOwned,
            Cost: cost,
            CostD: costD,
            Spend: spend,
            NextCost: nextCost,
            Req: req,
            Reset: reset,
            Switch: switchValue,
            RuntimeSourceClass: "RealmUpgrade",
            RuntimeEffectClass: "SimpleEffect",
            RuntimeEffectName: runtimeEffectName,
            RuntimeParameterSource: "RealmUpgrade.Level",
            AppliedAdd: appliedAdd,
            AppliedMultiplier: appliedMultiplier,
            AppliedBonusPercent: bonusPercent,
            Formula: formula,
            FormulaSource: formulaSource,
            FormulaStatus: formulaStatus
        );
    }

    private static List<RealmMemoryTargetAggregate> BuildTargetAggregates(IReadOnlyList<RealmMemoryAppliedEffect> effects)
    {
        return effects
            .GroupBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var items = group.OrderBy(x => x.Id).ToList();
                var ownedItems = items.Where(x => x.IsOwned).ToList();

                var ownedAdditiveSum = ownedItems.Sum(x => x.AppliedAdd);
                var ownedMultiplierProduct = 1m;

                foreach (var item in ownedItems)
                {
                    ownedMultiplierProduct *= item.AppliedMultiplier;
                }

                return new RealmMemoryTargetAggregate(
                    Target: group.Key,
                    EffectCount: items.Count,
                    OwnedEffectCount: ownedItems.Count,
                    OwnedAdditiveSum: ownedAdditiveSum,
                    OwnedMultiplierProduct: ownedMultiplierProduct,
                    OwnedBonusPercent: (ownedMultiplierProduct - 1m) * 100m,
                    Groups: items.Select(x => x.Group).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    OwnedGroups: ownedItems.Select(x => x.Group).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    Operations: items.Select(x => x.Operation).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    OwnedOperations: ownedItems.Select(x => x.Operation).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    FormulaStatus: "SourceConfirmedApplied",
                    Effects: items
                );
            })
            .ToList();
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

    private static decimal GetSpendForLevel(decimal cost, decimal costD, int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        var n = (decimal)level;
        return (n * ((2m * cost) + ((n - 1m) * costD))) / 2m;
    }

    private static decimal GetNextCost(decimal cost, decimal costD, int level)
    {
        if (level < 0)
        {
            level = 0;
        }

        return cost + ((decimal)level * costD);
    }

    private static string FindRealmUpgradesFile(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "raw_files", "Assets", "Resources", "jsonfiles", RealmUpgradesDataFileName),
            Path.Combine(workspacePath, "Assets", "Resources", "jsonfiles", RealmUpgradesDataFileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
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

    private static decimal ParseDecimal(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0m;
    }

    private static double ParseDouble(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0d;
    }

    private static int ParseInt(string value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private sealed record RealmMemoryAppliedEffectsExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string RealmUpgradesFile,
        string OutputFile,
        RealmMemorySourceEvidence SourceEvidence,
        RealmMemoryAppliedEffectsSummary Summary,
        IReadOnlyList<RealmMemoryAppliedEffect> AllAppliedEffects,
        IReadOnlyList<RealmMemoryAppliedEffect> OwnedAppliedEffects,
        IReadOnlyList<RealmMemoryTargetAggregate> TargetAggregates,
        IReadOnlyList<RealmMemoryTargetAggregate> OwnedTargetAggregates,
        IReadOnlyList<string> Notes
    );

    private sealed record RealmMemoryOwnedTargetSummaryExport(
        string GeneratedAtUtc,
        string SourceFile,
        string OutputFile,
        RealmMemoryOwnedTargetSummary Summary,
        IReadOnlyList<RealmMemoryOwnedTargetSummaryEntry> OwnedTargetAggregates,
        IReadOnlyList<string> Notes
    );

    private sealed record RealmMemorySourceEvidence(
        string RealmUpgrade,
        string EffectFactoryLinear,
        string EffectFactoryPowIntW
    );

    private sealed record RealmMemoryAppliedEffectsSummary(
        int CatalogEffectCount,
        int OwnedEffectCount,
        int TargetCount,
        int OwnedTargetCount,
        decimal BaseAllBuildingsProfitOwnedMultiplier,
        decimal BaseAllBuildingsProfitOwnedBonusPercent,
        decimal RealmIncomeOwnedMultiplier,
        decimal RealmIncomeOwnedBonusPercent,
        string FormulaStatus
    );

    private sealed record RealmMemoryOwnedTargetSummary(
        int OwnedTargetCount,
        decimal BaseAllBuildingsProfitOwnedMultiplier,
        decimal BaseAllBuildingsProfitOwnedBonusPercent,
        decimal RealmIncomeOwnedMultiplier,
        decimal RealmIncomeOwnedBonusPercent,
        string FormulaStatus
    );

    private sealed record RealmMemoryAppliedEffect(
        int Id,
        string Name,
        string Group,
        string Description,
        string Target,
        string Operation,
        string RawValue,
        int Level,
        string MaxLevel,
        int? LevelsToMax,
        bool IsMaxed,
        bool IsOwned,
        decimal Cost,
        decimal CostD,
        decimal Spend,
        decimal NextCost,
        string Req,
        string Reset,
        string Switch,
        string RuntimeSourceClass,
        string RuntimeEffectClass,
        string RuntimeEffectName,
        string RuntimeParameterSource,
        decimal AppliedAdd,
        decimal AppliedMultiplier,
        decimal AppliedBonusPercent,
        string Formula,
        string FormulaSource,
        string FormulaStatus
    );

    private sealed record RealmMemoryTargetAggregate(
        string Target,
        int EffectCount,
        int OwnedEffectCount,
        decimal OwnedAdditiveSum,
        decimal OwnedMultiplierProduct,
        decimal OwnedBonusPercent,
        IReadOnlyList<string> Groups,
        IReadOnlyList<string> OwnedGroups,
        IReadOnlyList<string> Operations,
        IReadOnlyList<string> OwnedOperations,
        string FormulaStatus,
        IReadOnlyList<RealmMemoryAppliedEffect> Effects
    );

    private sealed record RealmMemoryOwnedTargetSummaryEntry(
        string Target,
        int OwnedEffectCount,
        decimal OwnedAdditiveSum,
        decimal OwnedMultiplierProduct,
        decimal OwnedBonusPercent,
        IReadOnlyList<string> OwnedOperations,
        IReadOnlyList<string> OwnedGroups
    );
}

// EOF - SaveRealmMemoryAppliedEffectsCommand.cs
