using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MappingCoverageReportCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@" --mapping-coverage-report active [H:\IdleWizard\Working\IW_Optimizer]");
            Console.WriteLine(@" --mapping-coverage-report arcanist_risengiant_main [H:\IdleWizard\Working\IW_Optimizer]");
            return;
        }

        var requestedBuildId = args[1];
        var root = args.Length >= 3
            ? Path.GetFullPath(args[2])
            : Directory.GetCurrentDirectory();

        var buildId = requestedBuildId.Equals("active", StringComparison.OrdinalIgnoreCase)
            ? ReadActiveBuildId(root)
            : requestedBuildId;

        if (string.IsNullOrWhiteSpace(buildId))
        {
            Console.WriteLine("No build ID was provided and no active build could be resolved.");
            return;
        }

        var buildRoot = Path.Combine(root, "runtime", "builds", buildId);
        var mappingsDir = Path.Combine(buildRoot, "mappings");
        var contextsDir = Path.Combine(buildRoot, "contexts");
        var calculationsDir = Path.Combine(buildRoot, "calculations");

        Directory.CreateDirectory(mappingsDir);

        var progressionEffectsPath = Path.Combine(mappingsDir, "progression_effects.json");
        var attributeEffectsPath = Path.Combine(mappingsDir, "attribute_effects.json");
        var memoryEffectsPath = Path.Combine(mappingsDir, "memory_effects.json");
        var paramnesicEffectsPath = Path.Combine(mappingsDir, "paramnesic_effects.json");
        var imprintHeritageSpendPath = Path.Combine(mappingsDir, "imprint_heritage_spend.json");
        var progressionContextPath = Path.Combine(contextsDir, "progression_context.json");
        var realmMemoryOwnedTargetSummaryPath = Path.Combine(calculationsDir, "realm_memory_owned_target_summary.json");
        var attributeProfitEffectsPath = Path.Combine(calculationsDir, "attribute_profit_effects.json");
        var attributeTargetEffectsPath = Path.Combine(calculationsDir, "attribute_target_effects.json");
        var outputPath = Path.Combine(mappingsDir, "mapping_coverage_report.json");

        var attributeAppliedIndex = MergeAppliedIndexes(
            LoadAttributeAppliedIndex(attributeProfitEffectsPath),
            LoadAttributeAppliedIndex(attributeTargetEffectsPath)
        );

        var realmMemoryAppliedIndex = LoadRealmMemoryAppliedIndex(realmMemoryOwnedTargetSummaryPath);

        var items = new List<MappingCoverageItem>();

        AddProgressionCoverage(
            items,
            progressionEffectsPath
        );

        AddAttributeCoverage(
            items,
            attributeEffectsPath,
            attributeAppliedIndex
        );

        AddMemoryCoverage(
            items,
            memoryEffectsPath,
            realmMemoryAppliedIndex
        );

        AddParamnesicCoverage(
            items,
            paramnesicEffectsPath
        );

        AddImprintHeritageCoverage(
            items,
            imprintHeritageSpendPath,
            realmMemoryAppliedIndex
        );

        AddProgressionContextCoverage(
            items,
            progressionContextPath
        );

        AddRealmMemoryAppliedCoverage(
            items,
            realmMemoryOwnedTargetSummaryPath
        );

        var summary = BuildSummary(items);

        var report = new MappingCoverageReport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            BuildId: buildId,
            BuildRoot: buildRoot,
            OutputFile: outputPath,
            SourceFiles: new MappingCoverageSourceFiles(
                ProgressionEffects: progressionEffectsPath,
                AttributeEffects: attributeEffectsPath,
                MemoryEffects: memoryEffectsPath,
                ParamnesicEffects: paramnesicEffectsPath,
                ImprintHeritageSpend: imprintHeritageSpendPath,
                ProgressionContext: progressionContextPath,
                RealmMemoryOwnedTargetSummary: realmMemoryOwnedTargetSummaryPath,
                AttributeProfitEffects: attributeProfitEffectsPath,
                AttributeTargetEffects: attributeTargetEffectsPath
            ),
            Summary: summary,
            Items: items
                .OrderBy(x => x.SourceCategory, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.SourceId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.EffectKey, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Notes: new[]
            {
                "Phase 2B mapping coverage report.",
                "This report reads existing Phase 2A mapping outputs and calculation summaries.",
                "This report does not rescan raw game files.",
                "Formula behavior is not invented. Unknown formula wiring is marked UnknownFormula.",
                "Applied means a source-confirmed calculation output already exists for the item or target.",
                "MappedNotApplied means a target/raw effect is present but formula wiring is not yet confirmed in the final calculation pipeline.",
                "Unmapped means owned save state exists but no candidate catalog/source target was found.",
                "SpecialCase means the source is intentionally modeled outside a normal data file.",
                "MappedNotOwned means a mapped effect exists in source data but is not owned by the current save.",
                "ContextMapped means a progression context state/diagnostic entry exists but is not itself an effect formula row.",
                "Paramnesic Power records use source-confirmed PowIntW formula: add * (mult + 1)^level."
            }
        );

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        File.WriteAllText(outputPath, JsonSerializer.Serialize(report, options));

        Console.WriteLine("IW Optimizer mapping coverage report");
        Console.WriteLine("------------------------------------");
        Console.WriteLine($"BuildId: {buildId}");
        Console.WriteLine($"BuildRoot: {buildRoot}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine("");
        Console.WriteLine("Summary:");
        Console.WriteLine($" TotalEffects: {summary.TotalEffects}");
        Console.WriteLine($" OwnedEffects: {summary.OwnedEffects}");
        Console.WriteLine($" MappedApplied: {summary.MappedApplied}");
        Console.WriteLine($" MappedNotApplied: {summary.MappedNotApplied}");
        Console.WriteLine($" MappedNotOwned: {summary.MappedNotOwned}");
        Console.WriteLine($" Unmapped: {summary.Unmapped}");
        Console.WriteLine($" SpecialCase: {summary.SpecialCase}");
        Console.WriteLine($" UnknownFormula: {summary.UnknownFormula}");
        Console.WriteLine($" SourceMissing: {summary.SourceMissing}");
    }

    private static string ReadActiveBuildId(string root)
    {
        var activeBuildPath = Path.Combine(root, "runtime", "active_build.json");

        if (!File.Exists(activeBuildPath))
        {
            return string.Empty;
        }

        var activeBuild = JsonNode.Parse(File.ReadAllText(activeBuildPath))?.AsObject();

        if (activeBuild is null)
        {
            return string.Empty;
        }

        return GetString(activeBuild, "ActiveBuildId");
    }

    private static IReadOnlyDictionary<string, AppliedCalculation> LoadAttributeAppliedIndex(string path)
    {
        var result = new Dictionary<string, AppliedCalculation>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return result;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            var effects = root?["Effects"]?.AsArray();

            if (effects is null)
            {
                return result;
            }

            foreach (var node in effects)
            {
                if (node is not JsonObject effect)
                {
                    continue;
                }

                var attributeKey = GetString(effect, "AttributeKey");
                var requiredLevel = GetInt(effect, "RequiredLevel");
                var target = GetString(effect, "Target");
                var formulaStatus = GetString(effect, "FormulaStatus");

                if (string.IsNullOrWhiteSpace(attributeKey)
                    || string.IsNullOrWhiteSpace(target)
                    || requiredLevel < 0)
                {
                    continue;
                }

                if (!formulaStatus.StartsWith("SourceConfirmedPreview", StringComparison.OrdinalIgnoreCase)
                    && !formulaStatus.Equals("Applied", StringComparison.OrdinalIgnoreCase)
                    && !formulaStatus.Equals("SourceConfirmedApplied", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var contribution = FirstNonEmpty(
                    GetString(effect, "AppliedMultiplierText"),
                    GetString(effect, "AppliedBonusPercent"),
                    GetString(effect, "AppliedAdd")
                );

                result[MakeAttributeAppliedKey(attributeKey, requiredLevel, target)] = new AppliedCalculation(
                    FormulaStatus: "Applied",
                    ComputedContribution: contribution,
                    Notes: formulaStatus
                );
            }
        }
        catch
        {
            return result;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, AppliedCalculation> LoadRealmMemoryAppliedIndex(string path)
    {
        var result = new Dictionary<string, AppliedCalculation>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return result;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            var summary = root?["Summary"]?.AsObject();
            var aggregates = root?["OwnedTargetAggregates"]?.AsArray();

            if (aggregates is null)
            {
                return result;
            }

            var globalFormulaStatus = summary is null
                ? "Applied"
                : FirstNonEmpty(GetString(summary, "FormulaStatus"), "Applied");

            foreach (var node in aggregates)
            {
                if (node is not JsonObject aggregate)
                {
                    continue;
                }

                var target = GetString(aggregate, "Target");

                if (string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                var contribution = FirstNonEmpty(
                    GetString(aggregate, "OwnedMultiplierProduct"),
                    GetString(aggregate, "OwnedAdditiveSum"),
                    GetString(aggregate, "OwnedBonusPercent")
                );

                result[target] = new AppliedCalculation(
                    FormulaStatus: "Applied",
                    ComputedContribution: contribution,
                    Notes: globalFormulaStatus
                );
            }
        }
        catch
        {
            return result;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, AppliedCalculation> MergeAppliedIndexes(
        IReadOnlyDictionary<string, AppliedCalculation> first,
        IReadOnlyDictionary<string, AppliedCalculation> second)
    {
        var merged = new Dictionary<string, AppliedCalculation>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in first)
        {
            merged[pair.Key] = pair.Value;
        }

        foreach (var pair in second)
        {
            merged[pair.Key] = pair.Value;
        }

        return merged;
    }

    private static string MakeAttributeAppliedKey(string attributeKey, int requiredLevel, string target)
    {
        return attributeKey + "|" + requiredLevel.ToString() + "|" + target;
    }

    private static void AddProgressionCoverage(List<MappingCoverageItem> items, string path)
    {
        var root = ReadObjectOrSourceMissing(items, "Progression", path);

        if (root is null)
        {
            return;
        }

        var systems = root["Systems"]?.AsArray();

        if (systems is null)
        {
            items.Add(SourceMissing("Progression", path, "Systems array missing."));
            return;
        }

        foreach (var node in systems)
        {
            if (node is not JsonObject system)
            {
                continue;
            }

            var name = GetString(system, "SystemName");
            var needsEffectMapping = GetBool(system, "NeedsEffectMapping");

            items.Add(new MappingCoverageItem(
                SourceCategory: "Progression",
                SourceFile: path,
                SourceId: name,
                DisplayName: name,
                EffectKey: "ProgressionSystem",
                RawTarget: "",
                RecognizedTarget: "",
                FormulaStatus: needsEffectMapping ? "UnknownFormula" : "ContextMapped",
                Owned: false,
                OwnedLevel: 0,
                RawValue: "",
                ComputedContribution: "",
                Notes: GetString(system, "Notes")
            ));
        }
    }

    private static void AddAttributeCoverage(
        List<MappingCoverageItem> items,
        string path,
        IReadOnlyDictionary<string, AppliedCalculation> attributeAppliedIndex)
    {
        var root = ReadObjectOrSourceMissing(items, "Attribute", path);

        if (root is null)
        {
            return;
        }

        var attributes = root["Attributes"]?.AsArray();

        if (attributes is null)
        {
            items.Add(SourceMissing("Attribute", path, "Attributes array missing."));
            return;
        }

        foreach (var node in attributes)
        {
            if (node is not JsonObject attribute)
            {
                continue;
            }

            var saveKey = GetString(attribute, "SaveKey");
            var displayName = GetString(attribute, "DisplayName");
            var fileFound = GetBool(attribute, "FileFound");
            var saveValue = GetInt(attribute, "SaveValue");
            var activeRecords = attribute["ActiveRecords"]?.AsArray();

            if (saveKey.Equals("Ver", StringComparison.OrdinalIgnoreCase))
            {
                items.Add(new MappingCoverageItem(
                    SourceCategory: "Attribute",
                    SourceFile: path,
                    SourceId: saveKey,
                    DisplayName: displayName,
                    EffectKey: "Versatility",
                    RawTarget: "Base.AllBuildingsProfit",
                    RecognizedTarget: "Base.AllBuildingsProfit",
                    FormulaStatus: "SpecialCase",
                    Owned: saveValue > 0,
                    OwnedLevel: saveValue,
                    RawValue: "1.018",
                    ComputedContribution: "",
                    Notes: "Special-case base attribute. No normal data file is expected."
                ));

                continue;
            }

            if (!fileFound)
            {
                items.Add(new MappingCoverageItem(
                    SourceCategory: "Attribute",
                    SourceFile: path,
                    SourceId: saveKey,
                    DisplayName: displayName,
                    EffectKey: "AttributeDataFile",
                    RawTarget: "",
                    RecognizedTarget: "",
                    FormulaStatus: "SourceMissing",
                    Owned: saveValue > 0,
                    OwnedLevel: saveValue,
                    RawValue: "",
                    ComputedContribution: "",
                    Notes: GetString(attribute, "Warning")
                ));

                continue;
            }

            if (activeRecords is null || activeRecords.Count == 0)
            {
                items.Add(new MappingCoverageItem(
                    SourceCategory: "Attribute",
                    SourceFile: path,
                    SourceId: saveKey,
                    DisplayName: displayName,
                    EffectKey: "NoActiveRecords",
                    RawTarget: "",
                    RecognizedTarget: "",
                    FormulaStatus: "ContextMapped",
                    Owned: saveValue > 0,
                    OwnedLevel: saveValue,
                    RawValue: "",
                    ComputedContribution: "",
                    Notes: "Attribute data file exists, but no active records were unlocked by the save value."
                ));

                continue;
            }

            foreach (var activeNode in activeRecords)
            {
                if (activeNode is not JsonObject activeRecord)
                {
                    continue;
                }

                var target = GetString(activeRecord, "Target");
                var level = GetInt(activeRecord, "Level");
                var rawValue = FirstNonEmpty(
                    GetString(activeRecord, "Add"),
                    GetString(activeRecord, "Mult"),
                    GetString(activeRecord, "Effect")
                );

                if (string.IsNullOrWhiteSpace(target))
                {
                    items.Add(new MappingCoverageItem(
                        SourceCategory: "Attribute",
                        SourceFile: path,
                        SourceId: saveKey,
                        DisplayName: displayName,
                        EffectKey: $"Level {level}",
                        RawTarget: target,
                        RecognizedTarget: target,
                        FormulaStatus: "Unmapped",
                        Owned: saveValue >= level,
                        OwnedLevel: saveValue,
                        RawValue: rawValue,
                        ComputedContribution: "",
                        Notes: GetString(activeRecord, "Description")
                    ));

                    continue;
                }

                var appliedKey = MakeAttributeAppliedKey(saveKey, level, target);
                var isApplied = attributeAppliedIndex.TryGetValue(appliedKey, out var applied);

                items.Add(new MappingCoverageItem(
                    SourceCategory: "Attribute",
                    SourceFile: path,
                    SourceId: saveKey,
                    DisplayName: displayName,
                    EffectKey: $"Level {level}",
                    RawTarget: target,
                    RecognizedTarget: target,
                    FormulaStatus: isApplied ? "Applied" : "MappedNotApplied",
                    Owned: saveValue >= level,
                    OwnedLevel: saveValue,
                    RawValue: rawValue,
                    ComputedContribution: applied?.ComputedContribution ?? "",
                    Notes: isApplied
                        ? FirstNonEmpty(applied?.Notes ?? "", GetString(activeRecord, "Description"))
                        : GetString(activeRecord, "Description")
                ));
            }
        }
    }

    private static void AddMemoryCoverage(
        List<MappingCoverageItem> items,
        string path,
        IReadOnlyDictionary<string, AppliedCalculation> realmMemoryAppliedIndex)
    {
        var root = ReadObjectOrSourceMissing(items, "Memory", path);

        if (root is null)
        {
            return;
        }

        var upgradeMaps = root["UpgradeMaps"]?.AsArray();

        if (upgradeMaps is null)
        {
            items.Add(SourceMissing("Memory", path, "UpgradeMaps array missing."));
            return;
        }

        foreach (var node in upgradeMaps)
        {
            if (node is not JsonObject upgrade)
            {
                continue;
            }

            var upgradeId = GetString(upgrade, "UpgradeId");
            var level = GetInt(upgrade, "Level");
            var status = GetString(upgrade, "MappingStatus");
            var candidates = upgrade["Candidates"]?.AsArray();
            var candidateCount = candidates?.Count ?? 0;

            if (candidateCount == 0)
            {
                items.Add(new MappingCoverageItem(
                    SourceCategory: "Memory",
                    SourceFile: path,
                    SourceId: upgradeId,
                    DisplayName: $"Memory Upgrade {upgradeId}",
                    EffectKey: "UpgradeMap",
                    RawTarget: "",
                    RecognizedTarget: "",
                    FormulaStatus: "Unmapped",
                    Owned: level > 0,
                    OwnedLevel: level,
                    RawValue: "",
                    ComputedContribution: "",
                    Notes: status
                ));

                continue;
            }

            foreach (var candidateNode in candidates!)
            {
                if (candidateNode is not JsonObject candidate)
                {
                    continue;
                }

                var target = GetString(candidate, "Target");

                if (string.IsNullOrWhiteSpace(target))
                {
                    items.Add(new MappingCoverageItem(
                        SourceCategory: "Memory",
                        SourceFile: path,
                        SourceId: upgradeId,
                        DisplayName: FirstNonEmpty(GetString(candidate, "Name"), $"Memory Upgrade {upgradeId}"),
                        EffectKey: GetString(candidate, "DataFile"),
                        RawTarget: target,
                        RecognizedTarget: target,
                        FormulaStatus: "Unmapped",
                        Owned: level > 0,
                        OwnedLevel: level,
                        RawValue: FirstNonEmpty(GetString(candidate, "Add"), GetString(candidate, "Mult")),
                        ComputedContribution: "",
                        Notes: status
                    ));

                    continue;
                }

                var isApplied = realmMemoryAppliedIndex.TryGetValue(target, out var applied);

                items.Add(new MappingCoverageItem(
                    SourceCategory: "Memory",
                    SourceFile: path,
                    SourceId: upgradeId,
                    DisplayName: FirstNonEmpty(GetString(candidate, "Name"), $"Memory Upgrade {upgradeId}"),
                    EffectKey: GetString(candidate, "DataFile"),
                    RawTarget: target,
                    RecognizedTarget: target,
                    FormulaStatus: isApplied ? "Applied" : "MappedNotApplied",
                    Owned: level > 0,
                    OwnedLevel: level,
                    RawValue: FirstNonEmpty(GetString(candidate, "Add"), GetString(candidate, "Mult")),
                    ComputedContribution: applied?.ComputedContribution ?? "",
                    Notes: isApplied
                        ? FirstNonEmpty(applied?.Notes ?? "", status)
                        : status
                ));
            }
        }
    }

    private static void AddParamnesicCoverage(List<MappingCoverageItem> items, string path)
    {
        var root = ReadObjectOrSourceMissing(items, "Paramnesic", path);

        if (root is null)
        {
            return;
        }

        var catalog = root["Catalog"]?.AsArray();
        var owned = ReadOwnedIdLevels(root["OwnedParamnesics"]?.AsArray());

        if (catalog is null)
        {
            items.Add(SourceMissing("Paramnesic", path, "Catalog array missing."));
            return;
        }

        foreach (var node in catalog)
        {
            if (node is not JsonObject record)
            {
                continue;
            }

            var id = GetInt(record, "Id");
            var idText = id.ToString();
            var level = owned.TryGetValue(id, out var ownedLevel) ? ownedLevel : 0;
            var target = GetString(record, "Param");
            var add = GetString(record, "Add");
            var mult = GetString(record, "Mult");
            var power = GetString(record, "Power");
            var rawValue = FirstNonEmpty(add, mult, power);

            var formulaStatus = string.IsNullOrWhiteSpace(target)
                ? "Unmapped"
                : level > 0
                    ? "UnknownFormula"
                    : "MappedNotOwned";

            var computedContribution = "";
            var notes = "Paramnesic catalog record surfaced. Formula wiring is not confirmed.";

            // Source-confirmed PowIntW: Paramnesic.cs sets add=1, mult=P, parameter=Level
            // EffectFactory.PowerIntW applies: v.Change(0, a * (m + 1)^num)
            if (!string.IsNullOrWhiteSpace(target)
                && level > 0
                && !string.IsNullOrWhiteSpace(power)
                && double.TryParse(
                    power,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var powerValue))
            {
                var multiplier = Math.Pow(powerValue + 1.0, level);
                computedContribution = multiplier.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
                formulaStatus = "Applied";
                notes = "Source-confirmed Paramnesic PowIntW formula: add * (mult + 1)^level.";
            }

            items.Add(new MappingCoverageItem(
                SourceCategory: "Paramnesic",
                SourceFile: path,
                SourceId: idText,
                DisplayName: GetString(record, "Name"),
                EffectKey: "CatalogRecord",
                RawTarget: target,
                RecognizedTarget: target,
                FormulaStatus: formulaStatus,
                Owned: level > 0,
                OwnedLevel: level,
                RawValue: rawValue,
                ComputedContribution: computedContribution,
                Notes: notes
            ));
        }
    }

    private static void AddImprintHeritageCoverage(
        List<MappingCoverageItem> items,
        string path,
        IReadOnlyDictionary<string, AppliedCalculation> realmMemoryAppliedIndex)
    {
        var root = ReadObjectOrSourceMissing(items, "ImprintHeritage", path);

        if (root is null)
        {
            return;
        }

        AddDescriptorArray(items, path, "Imprint", root["ImprintEffectDescriptors"]?.AsArray(), realmMemoryAppliedIndex);
        AddDescriptorArray(items, path, "Heritage", root["HeritageEffectDescriptors"]?.AsArray(), realmMemoryAppliedIndex);
    }

    private static void AddDescriptorArray(
        List<MappingCoverageItem> items,
        string path,
        string category,
        JsonArray? descriptors,
        IReadOnlyDictionary<string, AppliedCalculation> realmMemoryAppliedIndex)
    {
        if (descriptors is null)
        {
            items.Add(SourceMissing(category, path, $"{category} descriptors array missing."));
            return;
        }

        foreach (var node in descriptors)
        {
            if (node is not JsonObject descriptor)
            {
                continue;
            }

            var target = GetString(descriptor, "Target");
            var formulaStatus = GetString(descriptor, "FormulaStatus");
            var owned = GetBool(descriptor, "IsOwned");
            var level = GetInt(descriptor, "Level");

            if (string.IsNullOrWhiteSpace(target))
            {
                items.Add(new MappingCoverageItem(
                    SourceCategory: category,
                    SourceFile: path,
                    SourceId: GetString(descriptor, "Id"),
                    DisplayName: GetString(descriptor, "Name"),
                    EffectKey: GetString(descriptor, "Operation"),
                    RawTarget: target,
                    RecognizedTarget: target,
                    FormulaStatus: "Unmapped",
                    Owned: owned,
                    OwnedLevel: level,
                    RawValue: GetString(descriptor, "RawValue"),
                    ComputedContribution: "",
                    Notes: formulaStatus
                ));

                continue;
            }

            var isApplied = realmMemoryAppliedIndex.TryGetValue(target, out var applied);
            var effectiveStatus = isApplied
                ? "Applied"
                : owned
                    ? NormalizeRawFormulaStatus(formulaStatus, target)
                    : "MappedNotOwned";

            items.Add(new MappingCoverageItem(
                SourceCategory: category,
                SourceFile: path,
                SourceId: GetString(descriptor, "Id"),
                DisplayName: GetString(descriptor, "Name"),
                EffectKey: GetString(descriptor, "Operation"),
                RawTarget: target,
                RecognizedTarget: target,
                FormulaStatus: effectiveStatus,
                Owned: owned,
                OwnedLevel: level,
                RawValue: GetString(descriptor, "RawValue"),
                ComputedContribution: applied?.ComputedContribution ?? "",
                Notes: isApplied
                    ? FirstNonEmpty(applied?.Notes ?? "", formulaStatus)
                    : formulaStatus
            ));
        }
    }

    private static void AddProgressionContextCoverage(List<MappingCoverageItem> items, string path)
    {
        var root = ReadObjectOrSourceMissing(items, "ProgressionContext", path);

        if (root is null)
        {
            return;
        }

        var coverage = root["MappingCoverage"]?.AsObject();

        if (coverage is null)
        {
            items.Add(SourceMissing("ProgressionContext", path, "MappingCoverage object missing."));
            return;
        }

        AddStringArrayCoverage(items, path, "ProgressionContextMapped", coverage["Mapped"]?.AsArray(), "ContextMapped");
        AddStringArrayCoverage(items, path, "ProgressionContextNeedsEffectMapping", coverage["NeedsEffectMapping"]?.AsArray(), "UnknownFormula");
        AddStringArrayCoverage(items, path, "ProgressionContextUnknownOrDeferred", coverage["UnknownOrDeferredSaveObjects"]?.AsArray(), "UnknownFormula");
    }

    private static void AddRealmMemoryAppliedCoverage(List<MappingCoverageItem> items, string path)
    {
        var root = ReadObjectOrSourceMissing(items, "RealmMemoryApplied", path);

        if (root is null)
        {
            return;
        }

        var summary = root["Summary"]?.AsObject();
        var aggregates = root["OwnedTargetAggregates"]?.AsArray();

        if (aggregates is null)
        {
            items.Add(SourceMissing("RealmMemoryApplied", path, "OwnedTargetAggregates array missing."));
            return;
        }

        var globalFormulaStatus = summary is null
            ? "Applied"
            : FirstNonEmpty(GetString(summary, "FormulaStatus"), "Applied");

        foreach (var node in aggregates)
        {
            if (node is not JsonObject aggregate)
            {
                continue;
            }

            var target = GetString(aggregate, "Target");

            items.Add(new MappingCoverageItem(
                SourceCategory: "RealmMemoryApplied",
                SourceFile: path,
                SourceId: target,
                DisplayName: target,
                EffectKey: "OwnedTargetAggregate",
                RawTarget: target,
                RecognizedTarget: target,
                FormulaStatus: "Applied",
                Owned: GetInt(aggregate, "OwnedEffectCount") > 0,
                OwnedLevel: GetInt(aggregate, "OwnedEffectCount"),
                RawValue: "",
                ComputedContribution: FirstNonEmpty(
                    GetString(aggregate, "OwnedMultiplierProduct"),
                    GetString(aggregate, "OwnedAdditiveSum"),
                    GetString(aggregate, "OwnedBonusPercent")
                ),
                Notes: globalFormulaStatus
            ));
        }
    }

    private static void AddStringArrayCoverage(
        List<MappingCoverageItem> items,
        string path,
        string category,
        JsonArray? values,
        string formulaStatus)
    {
        if (values is null)
        {
            return;
        }

        foreach (var value in values)
        {
            var text = value?.GetValue<string>() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            items.Add(new MappingCoverageItem(
                SourceCategory: category,
                SourceFile: path,
                SourceId: text,
                DisplayName: text,
                EffectKey: "ProgressionContext",
                RawTarget: "",
                RecognizedTarget: "",
                FormulaStatus: formulaStatus,
                Owned: false,
                OwnedLevel: 0,
                RawValue: "",
                ComputedContribution: "",
                Notes: "Progression context coverage entry."
            ));
        }
    }

    private static JsonObject? ReadObjectOrSourceMissing(
        List<MappingCoverageItem> items,
        string category,
        string path)
    {
        if (!File.Exists(path))
        {
            items.Add(SourceMissing(category, path, "Source file does not exist."));
            return null;
        }

        try
        {
            var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject();

            if (root is null)
            {
                items.Add(SourceMissing(category, path, "Source file root is not a JSON object."));
                return null;
            }

            return root;
        }
        catch (Exception ex)
        {
            items.Add(SourceMissing(category, path, "Failed to read JSON: " + ex.Message));
            return null;
        }
    }

    private static Dictionary<int, int> ReadOwnedIdLevels(JsonArray? array)
    {
        var result = new Dictionary<int, int>();

        if (array is null)
        {
            return result;
        }

        foreach (var node in array)
        {
            if (node is not JsonObject obj)
            {
                continue;
            }

            var id = GetInt(obj, "Id");
            var level = GetInt(obj, "Level");

            if (id > 0)
            {
                result[id] = level;
            }
        }

        return result;
    }

    private static MappingCoverageItem SourceMissing(string category, string path, string notes)
    {
        return new MappingCoverageItem(
            SourceCategory: category,
            SourceFile: path,
            SourceId: "",
            DisplayName: category,
            EffectKey: "",
            RawTarget: "",
            RecognizedTarget: "",
            FormulaStatus: "SourceMissing",
            Owned: false,
            OwnedLevel: 0,
            RawValue: "",
            ComputedContribution: "",
            Notes: notes
        );
    }

    private static string NormalizeRawFormulaStatus(string status, string target)
    {
        if (status.Equals("NoRawEffectField", StringComparison.OrdinalIgnoreCase))
        {
            return "Unmapped";
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            return "Unmapped";
        }

        if (status.Equals("RawMappedNotApplied", StringComparison.OrdinalIgnoreCase))
        {
            return "MappedNotApplied";
        }

        if (status.Equals("SourceConfirmedApplied", StringComparison.OrdinalIgnoreCase))
        {
            return "Applied";
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            return "UnknownFormula";
        }

        return status;
    }

    private static MappingCoverageSummary BuildSummary(IReadOnlyList<MappingCoverageItem> items)
    {
        return new MappingCoverageSummary(
            TotalEffects: items.Count,
            OwnedEffects: items.Count(x => x.Owned),
            MappedApplied: items.Count(x => IsStatus(x, "Applied") || IsStatus(x, "SourceConfirmedApplied")),
            MappedNotApplied: items.Count(x => IsStatus(x, "MappedNotApplied")),
            MappedNotOwned: items.Count(x => IsStatus(x, "MappedNotOwned")),
            Unmapped: items.Count(x => IsStatus(x, "Unmapped")),
            SpecialCase: items.Count(x => IsStatus(x, "SpecialCase")),
            UnknownFormula: items.Count(x => IsStatus(x, "UnknownFormula")),
            SourceMissing: items.Count(x => IsStatus(x, "SourceMissing"))
        );
    }

    private static bool IsStatus(MappingCoverageItem item, string status)
    {
        return item.FormulaStatus.Equals(status, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetString(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return string.Empty;
        }

        try
        {
            return node.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return node.ToJsonString().Trim('"');
        }
    }

    private static int GetInt(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return 0;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch
        {
            if (int.TryParse(GetString(obj, propertyName), out var parsed))
            {
                return parsed;
            }

            return 0;
        }
    }

    private static bool GetBool(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return false;
        }

        try
        {
            return node.GetValue<bool>();
        }
        catch
        {
            return bool.TryParse(GetString(obj, propertyName), out var parsed) && parsed;
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private sealed record AppliedCalculation(
        string FormulaStatus,
        string ComputedContribution,
        string Notes
    );

    private sealed record MappingCoverageReport(
        string GeneratedAtUtc,
        string BuildId,
        string BuildRoot,
        string OutputFile,
        MappingCoverageSourceFiles SourceFiles,
        MappingCoverageSummary Summary,
        IReadOnlyList<MappingCoverageItem> Items,
        IReadOnlyList<string> Notes
    );

    private sealed record MappingCoverageSourceFiles(
        string ProgressionEffects,
        string AttributeEffects,
        string MemoryEffects,
        string ParamnesicEffects,
        string ImprintHeritageSpend,
        string ProgressionContext,
        string RealmMemoryOwnedTargetSummary,
        string AttributeProfitEffects,
        string AttributeTargetEffects
    );

    private sealed record MappingCoverageSummary(
        int TotalEffects,
        int OwnedEffects,
        int MappedApplied,
        int MappedNotApplied,
        int MappedNotOwned,
        int Unmapped,
        int SpecialCase,
        int UnknownFormula,
        int SourceMissing
    );

    private sealed record MappingCoverageItem(
        string SourceCategory,
        string SourceFile,
        string SourceId,
        string DisplayName,
        string EffectKey,
        string RawTarget,
        string RecognizedTarget,
        string FormulaStatus,
        bool Owned,
        int OwnedLevel,
        string RawValue,
        string ComputedContribution,
        string Notes
    );
}

// EOF - MappingCoverageReportCommand.cs