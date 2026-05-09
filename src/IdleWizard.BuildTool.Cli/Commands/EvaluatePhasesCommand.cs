using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class EvaluatePhasesCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine(@"Usage: --evaluate-phases .\save_export.txt .\iw_workspace_vNext .\phase_config.json .\phase_eval.json");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var phaseConfigPath = Path.GetFullPath(args[3]);
        var outputPath = Path.GetFullPath(args[4]);

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        if (!File.Exists(phaseConfigPath))
        {
            Console.WriteLine($"Phase config not found: {phaseConfigPath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var saveDoc = JsonDocument.Parse(saveJson);
        var saveRoot = saveDoc.RootElement;

        using var configDoc = JsonDocument.Parse(File.ReadAllText(phaseConfigPath));
        var configRoot = configDoc.RootElement;

        var referencePhaseId = GetString(configRoot, "referencePhaseId", "burst");
        var baseline = LoadBaselineAssumptions(savePath);
        var heroMap = LoadEnumMap(workspacePath, "HeroesNames.cs", "HeroesNames");
        var heroId = GetInt(saveRoot, "Hero");
        var className = heroMap.TryGetValue(heroId, out var mappedHero) ? mappedHero : heroId.ToString();

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();
        var allItemEffects = extractor.Extract(workspacePath, "Items");
        var craftItems = ReadCraftItems(saveRoot);

        var phases = new List<PhaseEvaluation>();

        if (configRoot.TryGetProperty("phases", out var phasesElement) && phasesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var phaseElement in phasesElement.EnumerateArray())
            {
                phases.Add(EvaluatePhase(saveRoot, phaseElement, craftItems, allItemEffects, converter, baseline));
            }
        }

        var referencePhase = phases.FirstOrDefault(x => x.PhaseId.Equals(referencePhaseId, StringComparison.OrdinalIgnoreCase))
            ?? phases.FirstOrDefault();

        var summary = new CharacterSummary(
            HeroId: heroId,
            ClassName: className,
            RealmOrLegacy: GetNestedString(saveRoot, new[] { "Realm", "Active" }, ""),
            Ascensions: GetInt(saveRoot, "Ascends"),
            Paragon: GetInt(saveRoot, "Paragon"),
            EnchantingDust: ReadNestedBigNumberScientific(saveRoot, new[] { "Gilding", "resource" }),
            HeroMaxLevelAllTime: GetInt(saveRoot, "HeroMaxLevelAllTime"),
            ReferencePhaseId: referencePhase?.PhaseId ?? "",
            ReferenceLevel: referencePhase?.CalculatedLevel ?? 0,
            ReferenceLevelSource: referencePhase?.DisplayName ?? ""
        );

        var output = new PhaseEvaluationExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            PhaseConfig: phaseConfigPath,
            CharacterSummary: summary,
            BaselineAssumptions: baseline,
            Phases: phases,
            Notes: new[]
            {
                "Level is evaluated per phase. The sidebar/reference level should use CharacterSummary.ReferenceLevel.",
                "Phase equipment can come from explicit equipmentItemIds or an ItemPresets path such as ItemPresets.quasi.arcan.3.",
                "BaselineAssumptions are read from an existing zz save hero XP map if present; otherwise conservative defaults are used."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Evaluate phases");
        Console.WriteLine("---------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Config:    {phaseConfigPath}");
        Console.WriteLine($"Output:    {outputPath}");
        Console.WriteLine($"Class:     {className}");
        Console.WriteLine($"Reference: {summary.ReferencePhaseId} level {summary.ReferenceLevel}");
        Console.WriteLine("");

        foreach (var phase in phases)
        {
            Console.WriteLine($"{phase.DisplayName} [{phase.PhaseId}]");
            Console.WriteLine($"  Objective:       {phase.Objective}");
            Console.WriteLine($"  Items:           {phase.ItemCount}");
            Console.WriteLine($"  XP item effects: {phase.HeroXpRelevantItemEffects.Count}");
            Console.WriteLine($"  ExpBoost:        {phase.DerivedStats.ExpBoost}");
            Console.WriteLine($"  ExpMS:           {phase.DerivedStats.ExpManaSources}");
            Console.WriteLine($"  ExpMult:         {phase.DerivedStats.ExpMult}");
            Console.WriteLine($"  Level:           {phase.CalculatedLevel}");
            Console.WriteLine($"  Diff observed:   {phase.DifferenceFromObserved}");
            Console.WriteLine("");
        }
    }

    private static PhaseEvaluation EvaluatePhase(
        JsonElement saveRoot,
        JsonElement phaseElement,
        IReadOnlyDictionary<int, CraftItem> craftItems,
        IReadOnlyList<RawEffectDescriptor> allItemEffects,
        RawEffectConverter converter,
        BaselineAssumptions baseline)
    {
        var phaseId = GetString(phaseElement, "phaseId", "phase");
        var displayName = GetString(phaseElement, "displayName", phaseId);
        var objective = GetString(phaseElement, "objective", "Unknown");
        var usesVoidManaFromPhaseId = GetString(phaseElement, "usesVoidManaFromPhaseId", "");

        var itemIds = ResolvePhaseItemIds(saveRoot, phaseElement);
        var itemSpecs = new List<ItemSpec>();

        foreach (var itemId in itemIds)
        {
            if (craftItems.TryGetValue(itemId, out var craftItem))
            {
                itemSpecs.Add(new ItemSpec(itemId, craftItem.Tier, craftItem.Enchant, craftItem.Favorite, "Mapped"));
            }
            else
            {
                itemSpecs.Add(new ItemSpec(itemId, 0, 0, false, "MissingCraftItem"));
            }
        }

        var context = new CalculatorContext();
        Register(context, "Hero.ExpBoost", CalcBigNumber.One);
        Register(context, "Hero.ExpMS", CalcBigNumber.One);
        Register(context, "Hero.ExpMult", CalcBigNumber.One);

        var effectsToApply = new List<ICalcEffect>();
        var xpEffectRows = new List<PhaseItemEffect>();
        var unresolved = new List<PhaseUnresolvedEffect>();

        foreach (var spec in itemSpecs.Where(x => x.Status == "Mapped"))
        {
            var itemEffects = allItemEffects
                .Where(effect => effect.SourceId.Equals(spec.ItemId.ToString(), StringComparison.OrdinalIgnoreCase)
                                 && effect.Notes.Equals($"Tier={spec.Tier}", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var rawEffect in itemEffects)
            {
                Register(context, rawEffect.Target, CalcBigNumber.One);
                var conversion = converter.Convert(rawEffect);

                if (conversion.Status != RawEffectConversionStatus.Convertible)
                {
                    unresolved.Add(new PhaseUnresolvedEffect(spec.ItemId, spec.Tier, rawEffect.Target, conversion.Status.ToString(), conversion.Message, rawEffect.SourcePath));
                    continue;
                }

                effectsToApply.Add(converter.ToLinearEffect(rawEffect, null, null));

                if (IsHeroXpTarget(rawEffect.Target))
                {
                    xpEffectRows.Add(new PhaseItemEffect(spec.ItemId, spec.Tier, spec.Enchant, rawEffect.SourceName, rawEffect.Target, rawEffect.Notes, rawEffect.SourcePath));
                }
            }
        }

        var report = new BuildEvaluator().Evaluate(context, effectsToApply);
        var factorExpBoost = ToDouble(context.Resources.All["Hero.ExpBoost"].Value);
        var factorExpMS = ToDouble(context.Resources.All["Hero.ExpMS"].Value);
        var factorExpMult = ToDouble(context.Resources.All["Hero.ExpMult"].Value);

        var expBoost = baseline.ExpBoost * factorExpBoost;
        var expMS = baseline.ExpManaSources * factorExpMS;
        var expMult = baseline.ExpMult * factorExpMult;

        var estimate = EstimateHeroLevel(saveRoot, baseline, expBoost, expMS, expMult);

        return new PhaseEvaluation(
            PhaseId: phaseId,
            DisplayName: displayName,
            Objective: objective,
            EquipmentPresetPath: GetString(phaseElement, "equipmentPresetPath", ""),
            UsesVoidManaFromPhaseId: usesVoidManaFromPhaseId,
            ItemCount: itemSpecs.Count,
            Items: itemSpecs,
            AppliedEffectCount: report.Effects.Count,
            UnresolvedEffectCount: unresolved.Count,
            IsFullyVerified: report.IsFullyVerified && unresolved.Count == 0,
            ItemEffectFactors: new HeroXpEffectFactors(factorExpBoost, factorExpMS, factorExpMult),
            DerivedStats: new HeroXpDerivedStats(expBoost, expMS, expMult, baseline.ExpStack),
            CalculatedLevel: estimate.EstimatedLevel,
            ObservedLevel: baseline.ObservedLevel,
            DifferenceFromObserved: baseline.ObservedLevel > 0 ? estimate.EstimatedLevel - baseline.ObservedLevel : 0,
            Estimate: estimate,
            HeroXpRelevantItemEffects: xpEffectRows,
            Unresolved: unresolved,
            Notes: new[]
            {
                "This phase level uses baseline XP assumptions multiplied by this phase equipment item effects.",
                "Spell, pet, VM, and burst objective math are intentionally placeholders in V1; this command establishes phase-local equipment-derived level."
            }
        );
    }

    private static List<int> ResolvePhaseItemIds(JsonElement saveRoot, JsonElement phaseElement)
    {
        if (phaseElement.TryGetProperty("equipmentItemIds", out var explicitItems) && explicitItems.ValueKind == JsonValueKind.Array)
        {
            return explicitItems.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number)
                .Select(x => x.GetInt32())
                .Where(x => x >= 0)
                .Distinct()
                .ToList();
        }

        var presetPath = GetString(phaseElement, "equipmentPresetPath", "");
        if (!string.IsNullOrWhiteSpace(presetPath))
        {
            return ResolveItemPresetPath(saveRoot, presetPath);
        }

        return ReadCurrentlyEquippedItems(saveRoot);
    }

    private static List<int> ResolveItemPresetPath(JsonElement saveRoot, string path)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 4) return new List<int>();
        if (!parts[0].Equals("ItemPresets", StringComparison.OrdinalIgnoreCase)) return new List<int>();

        var presetIndex = parts[^1];
        var element = saveRoot;

        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!element.TryGetProperty(parts[i], out element))
            {
                return new List<int>();
            }
        }

        if (element.ValueKind != JsonValueKind.String) return new List<int>();
        var presetString = element.GetString() ?? string.Empty;
        return ParsePresetItemIds(presetString, presetIndex);
    }

    private static List<int> ParsePresetItemIds(string presetString, string presetIndex)
    {
        var pattern = "#" + Regex.Escape(presetIndex) + "#.*?@(?<items>[^#]+)";
        var match = Regex.Match(presetString, pattern, RegexOptions.Singleline);
        if (!match.Success) return new List<int>();

        return match.Groups["items"].Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var parsed) ? parsed : -1)
            .Where(x => x >= 0)
            .Distinct()
            .ToList();
    }

    private static List<int> ReadCurrentlyEquippedItems(JsonElement saveRoot)
    {
        var result = new List<int>();
        var seen = new HashSet<int>();

        if (!saveRoot.TryGetProperty("Craft", out var craft)) return result;
        if (!craft.TryGetProperty("Equiped", out var equipped) || equipped.ValueKind != JsonValueKind.Array) return result;

        foreach (var entry in equipped.EnumerateArray())
        {
            var itemId = GetInt(entry, "Value");
            if (itemId >= 0 && seen.Add(itemId)) result.Add(itemId);
        }

        return result;
    }

    private static Dictionary<int, CraftItem> ReadCraftItems(JsonElement root)
    {
        var result = new Dictionary<int, CraftItem>();
        if (!root.TryGetProperty("Craft", out var craft)) return result;
        if (!craft.TryGetProperty("Items", out var items) || items.ValueKind != JsonValueKind.Array) return result;

        foreach (var item in items.EnumerateArray())
        {
            var id = GetInt(item, "id");
            result[id] = new CraftItem(id, GetInt(item, "tier"), GetInt(item, "enchant"), GetBool(item, "fav"));
        }

        return result;
    }

    private static BaselineAssumptions LoadBaselineAssumptions(string savePath)
    {
        var saveDir = Path.GetDirectoryName(Path.GetFullPath(savePath)) ?? Environment.CurrentDirectory;
        var candidates = new[]
        {
            Path.Combine(saveDir, "zz", "save_hero_xp_map_fixed_audit.json"),
            Path.Combine(saveDir, "zz", "zz_save_hero_xp_map.json"),
            Path.Combine(saveDir, "zz", "save_hero_xp_map.json")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                var estimate = root.GetProperty("PartialEstimate");
                var assumptions = estimate.GetProperty("Assumptions");

                return new BaselineAssumptions(
                    Source: path,
                    ObservedLevel: GetInt(estimate, "ObservedLevel"),
                    StartingLevel: GetInt(assumptions, "StartingLevel", 1),
                    AddLevel: GetInt(assumptions, "AddLevel", 0),
                    ExpBoost: GetDouble(assumptions, "ExpBoost", 1.0),
                    ExpManaSources: GetDouble(assumptions, "ExpManaSources", 1.0),
                    ExpMult: GetDouble(assumptions, "ExpMult", 1.0),
                    ExpStack: GetDouble(assumptions, "ExpStack", 1.0),
                    GrowBaseLevel: GetDouble(assumptions, "GrowBaseLevel", 1.09),
                    BaseXp: GetDouble(assumptions, "BaseXp", 1500.0));
            }
            catch
            {
                // Ignore malformed baseline candidate and fall through to defaults.
            }
        }

        return new BaselineAssumptions(
            Source: "DefaultsNoExistingHeroXpMapFound",
            ObservedLevel: 0,
            StartingLevel: 1,
            AddLevel: 0,
            ExpBoost: 1.0,
            ExpManaSources: 1.0,
            ExpMult: 1.0,
            ExpStack: 1.0,
            GrowBaseLevel: 1.09,
            BaseXp: 1500.0);
    }

    private static HeroXpEstimate EstimateHeroLevel(JsonElement root, BaselineAssumptions baseline, double expBoost, double expMS, double expMult)
    {
        var clicks = ReadBigNumberDouble(root, "Clicks");
        var autoClicks = ReadBigNumberDouble(root, "AutoClicks");
        var castSpell = ReadBigNumberDouble(root, "CastSpell");
        var manaSessionLog10 = ReadBigNumberLog10(root, "ManaSession");
        var clickableCollect = ReadBigNumberDouble(root, "ClickableCollect");
        var totalBuildings = ReadBigNumberDouble(root, "TotalBuildings");
        var expStack = Math.Max(1.0, baseline.ExpStack);

        var activeClickTerm = Math.Log10((clicks + autoClicks) * expBoost + 1.0) + 1.0;
        var activeSpellTerm = Math.Log(castSpell * expBoost + 1.0) / 4.0 + 1.0;
        var manaTerm = manaSessionLog10 + 1.0;
        var clickableTerm = Math.Log(clickableCollect * expBoost + 1.0) / 2.0 + 1.0;
        var getExpActive = activeClickTerm * activeSpellTerm * manaTerm * clickableTerm;
        var getExpBuildings = Math.Pow(totalBuildings * expMS + 1.0, 0.855);
        var getExpMultPart = expMult * ((getExpBuildings + 100.0) * getExpActive - 100.0);
        var startingExp = baseline.BaseXp * (1.0 - Math.Pow(baseline.GrowBaseLevel, baseline.StartingLevel - 1.0)) / (1.0 - baseline.GrowBaseLevel);
        var totalExperience = startingExp + expStack * getExpMultPart;
        var levelArgument = 1.0 - totalExperience * (1.0 - baseline.GrowBaseLevel) / baseline.BaseXp;
        var estimatedLevel = 0;

        if (levelArgument > 0.0 && !double.IsNaN(levelArgument) && !double.IsInfinity(levelArgument))
        {
            estimatedLevel = (int)Math.Floor(Math.Log(levelArgument, baseline.GrowBaseLevel)) + baseline.AddLevel + 1;
        }

        return new HeroXpEstimate(
            EstimatedLevel: estimatedLevel,
            ActiveClickTerm: activeClickTerm,
            ActiveSpellTerm: activeSpellTerm,
            ManaTerm: manaTerm,
            ClickableTerm: clickableTerm,
            GetExpActive: getExpActive,
            GetExpBuildings: getExpBuildings,
            GetExpMultPart: getExpMultPart,
            StartingExp: startingExp,
            TotalExperience: totalExperience,
            LevelCurveArgument: levelArgument);
    }

    private static bool IsHeroXpTarget(string target)
    {
        return target.Equals("Hero.ExpBoost", StringComparison.OrdinalIgnoreCase)
            || target.Equals("Hero.ExpMS", StringComparison.OrdinalIgnoreCase)
            || target.Equals("Hero.ExpManaSources", StringComparison.OrdinalIgnoreCase)
            || target.Equals("Hero.ExpMult", StringComparison.OrdinalIgnoreCase);
    }

    private static void Register(CalculatorContext context, string key, CalcBigNumber initial)
    {
        if (!context.Resources.TryGet(key, out _))
        {
            context.Resources.Register(new CalcVariableComplex(key, initial));
        }
    }

    private static double ToDouble(CalcBigNumber value)
    {
        var s = value.ToString();
        if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var direct)) return direct;
        return Math.Pow(10.0, value.Log10());
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

    private static Dictionary<int, string> LoadEnumMap(string workspacePath, string fileName, string enumName)
    {
        var rawFiles = Path.Combine(workspacePath, "raw_files");
        var file = Directory.Exists(rawFiles)
            ? Directory.GetFiles(rawFiles, fileName, SearchOption.AllDirectories).FirstOrDefault()
            : null;

        if (file is null) return new Dictionary<int, string>();

        var text = File.ReadAllText(file);
        var match = Regex.Match(text, $@"enum\s+{Regex.Escape(enumName)}\s*\{{(?<body>[\s\S]*?)\}}", RegexOptions.IgnoreCase);
        if (!match.Success) return new Dictionary<int, string>();

        var result = new Dictionary<int, string>();
        var current = 0;

        foreach (var rawPart in match.Groups["body"].Value.Split(','))
        {
            var part = Regex.Replace(rawPart, "//.*", "").Trim();
            if (string.IsNullOrWhiteSpace(part)) continue;

            var pieces = part.Split('=', 2);
            var name = pieces[0].Trim();

            if (pieces.Length == 2 && int.TryParse(pieces[1].Trim(), out var explicitValue)) current = explicitValue;

            result[current] = name;
            current++;
        }

        return result;
    }

    private static double ReadBigNumberDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return 0.0;
        if (value.ValueKind == JsonValueKind.Number) return value.GetDouble();
        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out var mantissa)
            && value.TryGetProperty("Exponent", out var exponent))
        {
            return mantissa.GetDouble() * Math.Pow(10.0, exponent.GetDouble());
        }
        return 0.0;
    }

    private static double ReadBigNumberLog10(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)) return 0.0;
        if (value.ValueKind == JsonValueKind.Number)
        {
            var d = value.GetDouble();
            return d > 0.0 ? Math.Log10(d) : 0.0;
        }
        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out var mantissa)
            && value.TryGetProperty("Exponent", out var exponent))
        {
            var m = mantissa.GetDouble();
            var e = exponent.GetDouble();
            return m > 0.0 ? Math.Log10(m) + e : 0.0;
        }
        return 0.0;
    }

    private static string ReadNestedBigNumberScientific(JsonElement root, IReadOnlyList<string> path)
    {
        var element = root;

        foreach (var part in path)
        {
            if (!element.TryGetProperty(part, out element)) return "";
        }

        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("Mantissa", out var mantissa)
            && element.TryGetProperty("Exponent", out var exponent))
        {
            return $"{mantissa.GetDouble():G17}e{exponent.GetInt32()}";
        }

        return element.ToString();
    }

    private static string GetNestedString(JsonElement root, IReadOnlyList<string> path, string defaultValue)
    {
        var element = root;

        foreach (var part in path)
        {
            if (!element.TryGetProperty(part, out element)) return defaultValue;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? defaultValue : element.ToString();
    }

    private static string GetString(JsonElement element, string propertyName, string defaultValue)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? defaultValue : defaultValue;
    }

    private static int GetInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : defaultValue;
    }

    private static double GetDouble(JsonElement element, string propertyName, double defaultValue)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : defaultValue;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean();
    }

    private sealed record PhaseEvaluationExport(string GeneratedAtUtc, string SaveFile, string Workspace, string PhaseConfig, CharacterSummary CharacterSummary, BaselineAssumptions BaselineAssumptions, IReadOnlyList<PhaseEvaluation> Phases, IReadOnlyList<string> Notes);
    private sealed record CharacterSummary(int HeroId, string ClassName, string RealmOrLegacy, int Ascensions, int Paragon, string EnchantingDust, int HeroMaxLevelAllTime, string ReferencePhaseId, int ReferenceLevel, string ReferenceLevelSource);
    private sealed record BaselineAssumptions(string Source, int ObservedLevel, int StartingLevel, int AddLevel, double ExpBoost, double ExpManaSources, double ExpMult, double ExpStack, double GrowBaseLevel, double BaseXp);
    private sealed record PhaseEvaluation(string PhaseId, string DisplayName, string Objective, string EquipmentPresetPath, string UsesVoidManaFromPhaseId, int ItemCount, IReadOnlyList<ItemSpec> Items, int AppliedEffectCount, int UnresolvedEffectCount, bool IsFullyVerified, HeroXpEffectFactors ItemEffectFactors, HeroXpDerivedStats DerivedStats, int CalculatedLevel, int ObservedLevel, int DifferenceFromObserved, HeroXpEstimate Estimate, IReadOnlyList<PhaseItemEffect> HeroXpRelevantItemEffects, IReadOnlyList<PhaseUnresolvedEffect> Unresolved, IReadOnlyList<string> Notes);
    private sealed record CraftItem(int ItemId, int Tier, int Enchant, bool Favorite);
    private sealed record ItemSpec(int ItemId, int Tier, int Enchant, bool Favorite, string Status);
    private sealed record HeroXpEffectFactors(double ExpBoostFactor, double ExpManaSourcesFactor, double ExpMultFactor);
    private sealed record HeroXpDerivedStats(double ExpBoost, double ExpManaSources, double ExpMult, double ExpStack);
    private sealed record PhaseItemEffect(int ItemId, int Tier, int Enchant, string SourceName, string Target, string Notes, string SourcePath);
    private sealed record PhaseUnresolvedEffect(int ItemId, int Tier, string Target, string Status, string Message, string SourcePath);
    private sealed record HeroXpEstimate(int EstimatedLevel, double ActiveClickTerm, double ActiveSpellTerm, double ManaTerm, double ClickableTerm, double GetExpActive, double GetExpBuildings, double GetExpMultPart, double StartingExp, double TotalExperience, double LevelCurveArgument);
}

// EOF - EvaluatePhasesCommand.cs
