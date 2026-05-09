using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveAttributeProfitEffectsCommand
{
    private const string TargetName = "Base.AllBuildingsProfit";

    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --save-attribute-profit-effects active [H:\IdleWizard\Working\IW_Optimizer]");
            Console.WriteLine(@"  --save-attribute-profit-effects arcanist_risengiant_main [H:\IdleWizard\Working\IW_Optimizer]");
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

        var attributeMapPath = Path.Combine(mappingsDir, "attribute_effects.json");
        var progressionContextPath = Path.Combine(contextsDir, "progression_context.json");
        var saveCalculationContextPath = Path.Combine(calculationsDir, "save_calculation_context.json");
        var outputPath = Path.Combine(calculationsDir, "attribute_profit_effects.json");

        Directory.CreateDirectory(calculationsDir);

        if (!File.Exists(attributeMapPath))
        {
            Console.WriteLine($"Attribute map not found: {attributeMapPath}");
            return;
        }

        if (!File.Exists(progressionContextPath))
        {
            Console.WriteLine($"Progression context not found: {progressionContextPath}");
            return;
        }

        if (!File.Exists(saveCalculationContextPath))
        {
            Console.WriteLine($"Save calculation context not found: {saveCalculationContextPath}");
            return;
        }

        var attributeMap = JsonNode.Parse(File.ReadAllText(attributeMapPath))?.AsObject();
        var progressionContext = JsonNode.Parse(File.ReadAllText(progressionContextPath))?.AsObject();
        var saveCalculationContext = JsonNode.Parse(File.ReadAllText(saveCalculationContextPath))?.AsObject();

        if (attributeMap is null)
        {
            Console.WriteLine($"Attribute map root is not a JSON object: {attributeMapPath}");
            return;
        }

        if (progressionContext is null)
        {
            Console.WriteLine($"Progression context root is not a JSON object: {progressionContextPath}");
            return;
        }

        if (saveCalculationContext is null)
        {
            Console.WriteLine($"Save calculation context root is not a JSON object: {saveCalculationContextPath}");
            return;
        }

        var effects = BuildAttributeProfitEffects(attributeMap, progressionContext, saveCalculationContext, outputPath);

        var appliedEffects = effects
            .Where(x => x.FormulaStatus.StartsWith("SourceConfirmedPreview", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var multiplierProduct = 1.0;

        foreach (var effect in appliedEffects)
        {
            multiplierProduct *= effect.AppliedMultiplier;
        }

        var summary = new AttributeProfitEffectsSummary(
            Target: TargetName,
            TotalEffectCount: effects.Count,
            AppliedPreviewEffectCount: appliedEffects.Count,
            MissingWeightCount: effects.Count(x => x.FormulaStatus.Equals("MissingWeight", StringComparison.OrdinalIgnoreCase)),
            ProvisionalWeightCount: effects.Count(IsProvisionalOrPartialPreview),
            AppliedMultiplierProduct: multiplierProduct.ToString("G17", CultureInfo.InvariantCulture),
            AppliedBonusPercent: ((multiplierProduct - 1.0) * 100.0).ToString("G17", CultureInfo.InvariantCulture),
            FormulaStatus: effects.Any(x => x.FormulaStatus.Equals("MissingWeight", StringComparison.OrdinalIgnoreCase))
                ? "SourceConfirmedFormulaMissingWeights"
                : effects.Any(IsProvisionalOrPartialPreview)
                    ? "SourceConfirmedFormulaProvisionalWeights"
                    : "SourceConfirmedPreview"
        );

        var export = new AttributeProfitEffectsExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            BuildId: buildId,
            AttributeMapFile: attributeMapPath,
            ProgressionContextFile: progressionContextPath,
            SaveCalculationContextFile: saveCalculationContextPath,
            OutputFile: outputPath,
            SourceEvidence: new[]
            {
                "AttributeManager.Create maps non-zero attribute records to SimpleEffect perks using item.e, item.Target, item.a, item.m, and item.w.",
                "SimpleEffect.Apply delegates to effect.apply(target, applied_add, applied_mult, parameter, prev_e).",
                "EffectFactory.Linear with parameter applies AppliedAdd = a * w and AppliedMultiplier = 1 + m * w.",
                "EffectFactory.Log10 applies AppliedMultiplier = 1 + a * log10(w + 1)^m.",
                "Pet.Level uses Pet.RecalculateLevel(PetExp), which depends on runtime Pet.StartingLvl. PetStartingLevel is currently inferred from PetExp and PetMaxLevel until the runtime effect stack is mapped.",
                "This command is a source-confirmed formula preview. It does not yet feed final build profit."
            },
            Summary: summary,
            Effects: effects,
            Notes: new[]
            {
                "Hero.Level is resolved from save_hero_xp_map.PartialEstimate.EstimatedLevel when available; it remains partial until current-run hero level is source-complete.",
                "Pet.Level is derived from progression_context.Pets.PetExp and progression_context.Pets.PetStartingLevel using the Pet.RecalculateLevel XP curve, then validated against PetMaxLevel.",
                "If PetStartingLevel is unavailable or validation fails, Pet.Level falls back to progression_context.Pets.PetMaxLevel as a provisional runtime-starting-level fallback.",
                "Large BigNumber weights are evaluated through log10 approximation when the formula uses Log10.",
                "This output intentionally does not modify realm memory applied effects or final build totals."
            }
        );

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true })
        );

        Console.WriteLine("Attribute profit effects");
        Console.WriteLine("------------------------");
        Console.WriteLine($"BuildId: {buildId}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine("");
        Console.WriteLine("Summary:");
        Console.WriteLine($"  Target: {summary.Target}");
        Console.WriteLine($"  TotalEffectCount: {summary.TotalEffectCount}");
        Console.WriteLine($"  AppliedPreviewEffectCount: {summary.AppliedPreviewEffectCount}");
        Console.WriteLine($"  MissingWeightCount: {summary.MissingWeightCount}");
        Console.WriteLine($"  ProvisionalWeightCount: {summary.ProvisionalWeightCount}");
        Console.WriteLine($"  AppliedMultiplierProduct: {summary.AppliedMultiplierProduct}");
        Console.WriteLine($"  AppliedBonusPercent: {summary.AppliedBonusPercent}");
        Console.WriteLine($"  FormulaStatus: {summary.FormulaStatus}");
    }

    private static bool IsProvisionalOrPartialPreview(AttributeProfitEffect effect)
    {
        return effect.FormulaStatus.Equals("SourceConfirmedPreviewProvisionalWeight", StringComparison.OrdinalIgnoreCase)
            || effect.FormulaStatus.Equals("SourceConfirmedPreviewPartialHeroXpEstimate", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<AttributeProfitEffect> BuildAttributeProfitEffects(
        JsonObject attributeMap,
        JsonObject progressionContext,
        JsonObject saveCalculationContext,
        string outputPath)
    {
        var results = new List<AttributeProfitEffect>();
        var attributes = attributeMap["Attributes"]?.AsArray();

        if (attributes is null)
        {
            return results;
        }

        foreach (var attributeNode in attributes)
        {
            if (attributeNode is not JsonObject attribute)
            {
                continue;
            }

            var saveKey = GetString(attribute, "SaveKey");
            var displayName = GetString(attribute, "DisplayName");
            var saveValue = GetInt(attribute, "SaveValue");
            var activeRecords = attribute["ActiveRecords"]?.AsArray();

            if (activeRecords is null)
            {
                continue;
            }

            foreach (var recordNode in activeRecords)
            {
                if (recordNode is not JsonObject record)
                {
                    continue;
                }

                var target = GetString(record, "Target");

                if (!target.Equals(TargetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var level = GetInt(record, "Level");
                var addText = GetString(record, "Add");
                var multText = GetString(record, "Mult");
                var effectName = FirstNonEmpty(GetString(record, "Effect"), "Linear");
                var weightKey = GetString(record, "Weight");
                var description = GetString(record, "Description");

                var add = ParseDoubleOrDefault(addText, 0.0);
                var mult = ParseDoubleOrDefault(multText, effectName.Equals("Linear", StringComparison.OrdinalIgnoreCase) ? 0.0 : 1.0);

                var weight = ResolveWeight(
                    weightKey,
                    progressionContext,
                    saveCalculationContext,
                    outputPath);

                if (!weight.Found)
                {
                    results.Add(new AttributeProfitEffect(
                        AttributeKey: saveKey,
                        AttributeName: displayName,
                        AttributeSaveValue: saveValue,
                        RequiredLevel: level,
                        Target: target,
                        Add: addText,
                        Mult: multText,
                        EffectName: effectName,
                        WeightKey: weightKey,
                        WeightValue: "",
                        WeightSource: "",
                        AppliedAdd: "0",
                        AppliedMultiplier: 1.0,
                        AppliedMultiplierText: "1",
                        AppliedBonusPercent: "0",
                        Formula: BuildFormulaText(effectName, addText, multText, weightKey),
                        FormulaStatus: "MissingWeight",
                        Notes: "Could not resolve weight key: " + weightKey + ". " + description
                    ));

                    continue;
                }

                var appliedAdd = 0.0;
                var appliedMultiplier = 1.0;

                if (effectName.Equals("Log10", StringComparison.OrdinalIgnoreCase))
                {
                    appliedMultiplier = 1.0 + add * Math.Pow(weight.Log10ValuePlusOne, mult);
                }
                else if (effectName.Equals("Linear", StringComparison.OrdinalIgnoreCase))
                {
                    appliedAdd = add * weight.NumericValue;
                    appliedMultiplier = 1.0 + mult * weight.NumericValue;
                }
                else
                {
                    results.Add(new AttributeProfitEffect(
                        AttributeKey: saveKey,
                        AttributeName: displayName,
                        AttributeSaveValue: saveValue,
                        RequiredLevel: level,
                        Target: target,
                        Add: addText,
                        Mult: multText,
                        EffectName: effectName,
                        WeightKey: weightKey,
                        WeightValue: weight.DisplayValue,
                        WeightSource: weight.Source,
                        AppliedAdd: "0",
                        AppliedMultiplier: 1.0,
                        AppliedMultiplierText: "1",
                        AppliedBonusPercent: "0",
                        Formula: BuildFormulaText(effectName, addText, multText, weightKey),
                        FormulaStatus: "UnsupportedEffect",
                        Notes: "Unsupported effect for this preview command: " + effectName + ". " + description
                    ));

                    continue;
                }

                var status = weight.Source.Contains("PartialHeroXpEstimate", StringComparison.OrdinalIgnoreCase)
                    ? "SourceConfirmedPreviewPartialHeroXpEstimate"
                    : weight.Source.Contains("DerivedPetLevel", StringComparison.OrdinalIgnoreCase)
                        ? "SourceConfirmedPreviewDerivedPetLevel"
                        : weight.IsProvisional
                            ? "SourceConfirmedPreviewProvisionalWeight"
                            : "SourceConfirmedPreview";

                results.Add(new AttributeProfitEffect(
                    AttributeKey: saveKey,
                    AttributeName: displayName,
                    AttributeSaveValue: saveValue,
                    RequiredLevel: level,
                    Target: target,
                    Add: addText,
                    Mult: multText,
                    EffectName: effectName,
                    WeightKey: weightKey,
                    WeightValue: weight.DisplayValue,
                    WeightSource: weight.Source,
                    AppliedAdd: appliedAdd.ToString("G17", CultureInfo.InvariantCulture),
                    AppliedMultiplier: appliedMultiplier,
                    AppliedMultiplierText: appliedMultiplier.ToString("G17", CultureInfo.InvariantCulture),
                    AppliedBonusPercent: ((appliedMultiplier - 1.0) * 100.0).ToString("G17", CultureInfo.InvariantCulture),
                    Formula: BuildFormulaText(effectName, addText, multText, weightKey),
                    FormulaStatus: status,
                    Notes: description
                ));
            }
        }

        return results
            .OrderBy(x => x.AttributeKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.RequiredLevel)
            .ToList();
    }

    private static ResolvedWeight ResolveWeight(
        string weightKey,
        JsonObject progressionContext,
        JsonObject saveCalculationContext,
        string outputPath)
    {
        if (string.IsNullOrWhiteSpace(weightKey))
        {
            return ResolvedWeight.Missing(weightKey);
        }

        if (weightKey.Equals("Char.Total", StringComparison.OrdinalIgnoreCase))
        {
            var attributes = progressionContext["Attributes"]?.AsObject();
            var value = attributes is null ? 0.0 : GetDouble(attributes, "AttTotal");

            return ResolvedWeight.Exact(weightKey, value, value.ToString("G17", CultureInfo.InvariantCulture), "progression_context.Attributes.AttTotal");
        }

        if (weightKey.Equals("Achiev.Count", StringComparison.OrdinalIgnoreCase))
        {
            var trials = progressionContext["TrialsAndAchievements"]?.AsObject();
            var value = trials is null ? 0.0 : GetDouble(trials, "AchievementCount");

            return ResolvedWeight.Exact(weightKey, value, value.ToString("G17", CultureInfo.InvariantCulture), "progression_context.TrialsAndAchievements.AchievementCount");
        }

        if (weightKey.Equals("Base.TotalBuildings", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveResourceWeight(saveCalculationContext, "TotalBuildings", weightKey, "save_calculation_context.Resources.TotalBuildings");
        }

        if (weightKey.Equals("VoidMana.Collect", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveResourceWeight(saveCalculationContext, "ClickableCollect", weightKey, "save_calculation_context.Resources.ClickableCollect");
        }

        if (weightKey.Equals("Spell.SpellCast", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveResourceWeight(saveCalculationContext, "CastSpell", weightKey, "save_calculation_context.Resources.CastSpell");
        }

        if (weightKey.Equals("Hero.Level", StringComparison.OrdinalIgnoreCase))
        {
            if (TryReadHeroXpEstimatedLevel(outputPath, out var estimatedHeroLevel))
            {
                return ResolvedWeight.Provisional(
                    weightKey,
                    estimatedHeroLevel,
                    estimatedHeroLevel.ToString("G17", CultureInfo.InvariantCulture),
                    "save_hero_xp_map.PartialEstimate.EstimatedLevel.PartialHeroXpEstimate");
            }

            var character = saveCalculationContext["Character"]?.AsObject();
            var value = character is null ? 0.0 : GetDouble(character, "HeroMaxLevelAllTime");

            return ResolvedWeight.Provisional(weightKey, value, value.ToString("G17", CultureInfo.InvariantCulture), "save_calculation_context.Character.HeroMaxLevelAllTime");
        }

        if (weightKey.Equals("Pet.Level", StringComparison.OrdinalIgnoreCase))
        {
            var pets = progressionContext["Pets"]?.AsObject();

            if (pets is null)
            {
                return ResolvedWeight.Missing(weightKey);
            }

            var petExp = GetDouble(pets, "PetExp");
            var petMaxLevel = GetDouble(pets, "PetMaxLevel");
            var petStartingLevel = GetDouble(pets, "PetStartingLevel");
            var petStartingLevelSource = FirstNonEmpty(GetString(pets, "PetStartingLevelSource"), "unresolved");

            if (petExp > 0.0 && petMaxLevel > 0.0 && petStartingLevel >= 0.0)
            {
                var roundedStartingLevel = (int)Math.Round(petStartingLevel);
                var derivedPetLevel = DerivePetLevelFromExp(petExp, growBase: 1.2, startingLevel: roundedStartingLevel);

                if (derivedPetLevel > 0.0 && Math.Abs(derivedPetLevel - petMaxLevel) < 0.0000001)
                {
                    return ResolvedWeight.Exact(
                        weightKey,
                        derivedPetLevel,
                        derivedPetLevel.ToString("G17", CultureInfo.InvariantCulture),
                        "pet_level_estimate.PetExpFormula.GrowBase1.2.StartingLevel"
                        + roundedStartingLevel.ToString(CultureInfo.InvariantCulture)
                        + ".ValidatedAgainstPetMaxLevel."
                        + petStartingLevelSource
                        + ".DerivedPetLevel");
                }
            }

            var fallbackValue = petMaxLevel;

            return ResolvedWeight.Provisional(
                weightKey,
                fallbackValue,
                fallbackValue.ToString("G17", CultureInfo.InvariantCulture),
                "progression_context.Pets.PetMaxLevel.ProvisionalRuntimePetStartingLevelFallback");
        }

        return ResolvedWeight.Missing(weightKey);
    }

    private static bool TryReadHeroXpEstimatedLevel(string outputPath, out double estimatedHeroLevel)
    {
        estimatedHeroLevel = 0.0;

        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return false;
        }

        var heroXpMapPath = Path.Combine(outputDirectory, "save_hero_xp_map.json");

        if (!File.Exists(heroXpMapPath))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(heroXpMapPath));
            var root = document.RootElement;

            if (!root.TryGetProperty("PartialEstimate", out var partialEstimate)
                || !partialEstimate.TryGetProperty("EstimatedLevel", out var estimatedLevelNode))
            {
                return false;
            }

            if (estimatedLevelNode.ValueKind == JsonValueKind.Number
                && estimatedLevelNode.TryGetDouble(out var numericValue))
            {
                estimatedHeroLevel = numericValue;
                return estimatedHeroLevel > 0.0;
            }

            if (estimatedLevelNode.ValueKind == JsonValueKind.String
                && double.TryParse(estimatedLevelNode.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue))
            {
                estimatedHeroLevel = numericValue;
                return estimatedHeroLevel > 0.0;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static double DerivePetLevelFromExp(double petExp, double growBase, int startingLevel)
    {
        const double baseXp = 100.0;

        if (petExp < 0.0 || growBase <= 1.0)
        {
            return 0.0;
        }

        var startingExp = baseXp * (1.0 - Math.Pow(growBase, startingLevel)) / (1.0 - growBase);
        var argument = 1.0 - ((startingExp + petExp) * (1.0 - growBase) / baseXp);

        if (argument <= 0.0 || double.IsNaN(argument) || double.IsInfinity(argument))
        {
            return 0.0;
        }

        var levelBase = Math.Floor(Math.Log(argument) / Math.Log(growBase));

        if (levelBase < 0.0)
        {
            levelBase = 0.0;
        }

        return levelBase + 1.0;
    }

    private static ResolvedWeight ResolveResourceWeight(
        JsonObject saveCalculationContext,
        string resourceName,
        string weightKey,
        string source)
    {
        var resources = saveCalculationContext["Resources"]?.AsArray();

        if (resources is null)
        {
            return ResolvedWeight.Missing(weightKey);
        }

        foreach (var node in resources)
        {
            if (node is not JsonObject resource)
            {
                continue;
            }

            var name = GetString(resource, "Name");

            if (!name.Equals(resourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var scientific = GetString(resource, "Scientific");
            var numeric = ParseScientificToDouble(scientific);
            var log10PlusOne = EstimateLog10PlusOne(scientific);

            return new ResolvedWeight(
                Key: weightKey,
                Found: true,
                IsProvisional: false,
                NumericValue: numeric,
                Log10ValuePlusOne: log10PlusOne,
                DisplayValue: scientific,
                Source: source
            );
        }

        return ResolvedWeight.Missing(weightKey);
    }

    private static string BuildFormulaText(string effectName, string add, string mult, string weightKey)
    {
        if (effectName.Equals("Log10", StringComparison.OrdinalIgnoreCase))
        {
            return $"AppliedMultiplier = 1 + {add} * log10({weightKey} + 1) ^ {mult}";
        }

        if (effectName.Equals("Linear", StringComparison.OrdinalIgnoreCase))
        {
            return $"AppliedAdd = {FirstNonEmpty(add, "0")} * {weightKey}; AppliedMultiplier = 1 + {FirstNonEmpty(mult, "0")} * {weightKey}";
        }

        return $"{effectName}: a={add}; m={mult}; w={weightKey}";
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
            if (int.TryParse(GetString(obj, propertyName), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0;
        }
    }

    private static double GetDouble(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return 0.0;
        }

        try
        {
            return node.GetValue<double>();
        }
        catch
        {
            return ParseDoubleOrDefault(GetString(obj, propertyName), 0.0);
        }
    }

    private static double ParseDoubleOrDefault(string text, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultValue;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static double ParseScientificToDouble(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0.0;
        }

        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            if (double.IsInfinity(parsed))
            {
                return double.MaxValue;
            }

            return parsed;
        }

        return 0.0;
    }

    private static double EstimateLog10PlusOne(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0.0;
        }

        var normalized = text.Trim();
        var eIndex = normalized.IndexOf('e');

        if (eIndex < 0)
        {
            eIndex = normalized.IndexOf('E');
        }

        if (eIndex > 0)
        {
            var mantissaText = normalized[..eIndex];
            var exponentText = normalized[(eIndex + 1)..];

            if (double.TryParse(mantissaText, NumberStyles.Float, CultureInfo.InvariantCulture, out var mantissa)
                && int.TryParse(exponentText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var exponent))
            {
                if (exponent > 6)
                {
                    return exponent + Math.Log10(Math.Abs(mantissa));
                }
            }
        }

        var value = ParseScientificToDouble(normalized);

        if (value <= 0.0)
        {
            return 0.0;
        }

        return Math.Log10(value + 1.0);
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

    private sealed record ResolvedWeight(
        string Key,
        bool Found,
        bool IsProvisional,
        double NumericValue,
        double Log10ValuePlusOne,
        string DisplayValue,
        string Source)
    {
        public static ResolvedWeight Missing(string key)
        {
            return new ResolvedWeight(key, false, false, 0.0, 0.0, "", "");
        }

        public static ResolvedWeight Exact(string key, double value, string displayValue, string source)
        {
            var log = value <= 0.0 ? 0.0 : Math.Log10(value + 1.0);
            return new ResolvedWeight(key, true, false, value, log, displayValue, source);
        }

        public static ResolvedWeight Provisional(string key, double value, string displayValue, string source)
        {
            var log = value <= 0.0 ? 0.0 : Math.Log10(value + 1.0);
            return new ResolvedWeight(key, true, true, value, log, displayValue, source);
        }
    }

    private sealed record AttributeProfitEffectsExport(
        string GeneratedAtUtc,
        string BuildId,
        string AttributeMapFile,
        string ProgressionContextFile,
        string SaveCalculationContextFile,
        string OutputFile,
        IReadOnlyList<string> SourceEvidence,
        AttributeProfitEffectsSummary Summary,
        IReadOnlyList<AttributeProfitEffect> Effects,
        IReadOnlyList<string> Notes
    );

    private sealed record AttributeProfitEffectsSummary(
        string Target,
        int TotalEffectCount,
        int AppliedPreviewEffectCount,
        int MissingWeightCount,
        int ProvisionalWeightCount,
        string AppliedMultiplierProduct,
        string AppliedBonusPercent,
        string FormulaStatus
    );

    private sealed record AttributeProfitEffect(
        string AttributeKey,
        string AttributeName,
        int AttributeSaveValue,
        int RequiredLevel,
        string Target,
        string Add,
        string Mult,
        string EffectName,
        string WeightKey,
        string WeightValue,
        string WeightSource,
        string AppliedAdd,
        double AppliedMultiplier,
        string AppliedMultiplierText,
        string AppliedBonusPercent,
        string Formula,
        string FormulaStatus,
        string Notes
    );
}

// EOF - SaveAttributeProfitEffectsCommand.cs