using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveHeroXpMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-hero-xp-map .\save_export.txt .\iw_workspace_vNext [zz_save_hero_xp_map.json] [observedCurrentLevel]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\zz_save_hero_xp_map.json";

        var observedLevel = args.Length >= 5 && int.TryParse(args[4], out var parsedObserved)
            ? parsedObserved
            : 0;

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var heroEnum = LoadEnumMap(workspacePath, "HeroesNames.cs", "HeroesNames");
        var heroId = GetInt(root, "Hero");
        var className = heroEnum.TryGetValue(heroId, out var mappedHero)
            ? mappedHero
            : heroId.ToString();

        var saveFields = BuildRelevantSaveFields(root);
        var upgradeXpTargets = LoadUpgradeXpTargets();
        var candidateInputs = BuildCandidateInputs(root, upgradeXpTargets);
        var estimate = BuildEstimate(root, upgradeXpTargets, observedLevel);

        var map = new SaveHeroXpMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            Character: new HeroXpCharacterContext(
                HeroId: heroId,
                ClassName: className,
                HeroMaxLevelAllTime: GetInt(root, "HeroMaxLevelAllTime"),
                CharExpFlat: ReadBigNumberScientific(root, "CharExp"),
                CharExpStack: ReadBigNumberScientific(root, "CharExpMult"),
                HeroPlayedTime: GetInt(root, "HeroPlayedTime"),
                HeroSkipedPlayedTime: ReadBigNumberScientific(root, "HeroSkipedPlayedTime"),
                CurrentLevelMapped: estimate.EstimateIsSourceComplete,
                Notes: "HeroMaxLevelAllTime is all-time max. CharExp/CharExpMult are ExpFlat/ExpStack. Current level requires dynamic XP reconstruction."
            ),
            SourceConfirmedFormula: new HeroXpFormulaModel(
                FormulaName: "BaseHero.UpdateExp / GetExpActive / GetExpBuildings candidate",
                GetExpActive: "(log10((Clicks + AutoClicks) * ExpBoost + 1) + 1) * (ln(CastSpell * ExpBoost + 1) / 4 + 1) * (log10(ManaSession + 1) + 1) * (ln(ClickableCollect * ExpBoost + 1) / 2 + 1)",
                GetExpBuildings: "pow(TotalBuildings * ExpManaSources + 1, 0.855)",
                GetExpMultPart: "ExpMult * ((GetExpBuildings + 100) * GetExpActive - 100)",
                RecalculateExp: "Experience = StartingLevelXp + ExpStack * GetExpMultPart",
                LevelCurve: "Level = floor(log_base_1.09(1 - Experience * (1 - 1.09) / 1500)) + AddLevel + 1",
                Notes: new[]
                {
                    "Archon appears to use base Hero.GetExpActive in the current source pass.",
                    "This estimate uses save counters and purchased-upgrade target aggregates only.",
                    "This estimate does not include full runtime effect stack from challenges, attributes, items, pantheon/gods, realm systems, or other non-upgrade effects unless those effects are already represented in upgrade target aggregates.",
                    "If the estimate differs from the observed level, missing runtime StartingLevel/AddLevel/ExpBoost/ExpManaSources/ExpMult effects are the likely reason."
                }
            ),
            CandidateInputs: candidateInputs,
            PartialEstimate: estimate,
            RelevantSaveFields: saveFields,
            UpgradeXpTargets: upgradeXpTargets,
            MissingOrUnconfirmed: new[]
            {
                "Exact current Hero.Level save field does not exist; level is runtime-derived.",
                "StartingLevel current runtime value is not directly serialized.",
                "AddLevel current runtime value is not directly serialized.",
                "Full ExpBoost after all runtime effects may exceed purchased-upgrade aggregate estimate.",
                "Full ExpManaSources after all runtime effects may exceed purchased-upgrade aggregate estimate.",
                "Full ExpMult after all runtime effects is not fully mapped.",
                "Items/equipment effects are not included yet.",
                "Challenge, attribute, pantheon/god, realm, and special source-hook effects are not included yet."
            }
        );

        var json = JsonSerializer.Serialize(
            map,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save hero XP map");
        Console.WriteLine("----------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Output:    {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Class:     {className}");
        Console.WriteLine($"Fields:    {saveFields.Count}");
        Console.WriteLine($"Inputs:    {candidateInputs.Count}");
        Console.WriteLine($"XP upgrade targets: {upgradeXpTargets.Count}");
        Console.WriteLine("");
        Console.WriteLine("Partial level estimate:");
        Console.WriteLine($"  Estimated level: {estimate.EstimatedLevel}");
        Console.WriteLine($"  Observed level:  {(estimate.ObservedLevel > 0 ? estimate.ObservedLevel.ToString() : "not provided")}");
        Console.WriteLine($"  Difference:      {(estimate.ObservedLevel > 0 ? estimate.DifferenceFromObserved.ToString() : "n/a")}");
        Console.WriteLine($"  Source complete: {estimate.EstimateIsSourceComplete}");
        Console.WriteLine("");
        Console.WriteLine("Core assumptions:");
        Console.WriteLine($"  StartingLevel={estimate.Assumptions.StartingLevel}");
        Console.WriteLine($"  AddLevel={estimate.Assumptions.AddLevel}");
        Console.WriteLine($"  ExpBoost={estimate.Assumptions.ExpBoost}");
        Console.WriteLine($"  ExpManaSources={estimate.Assumptions.ExpManaSources}");
        Console.WriteLine($"  ExpMult={estimate.Assumptions.ExpMult}");
        Console.WriteLine($"  ExpStack={estimate.Assumptions.ExpStack}");
    }

    private static HeroXpPartialEstimate BuildEstimate(
        JsonElement root,
        IReadOnlyList<HeroXpUpgradeTarget> upgradeTargets,
        int observedLevel)
    {
        var clicks = ReadBigNumberDouble(root, "Clicks");
        var autoClicks = ReadBigNumberDouble(root, "AutoClicks");
        var castSpell = ReadBigNumberDouble(root, "CastSpell");
        var manaSessionLog10 = ReadBigNumberLog10(root, "ManaSession");
        var clickableCollect = ReadBigNumberDouble(root, "ClickableCollect");
        var totalBuildings = ReadBigNumberDouble(root, "TotalBuildings");
        var expStack = Math.Max(1.0, ReadBigNumberDouble(root, "CharExpMult"));

        var expBoost = GetTargetMultiplier(upgradeTargets, "Hero.ExpBoost", defaultValue: 1.0);
        var expManaSources = GetTargetMultiplier(upgradeTargets, "Hero.ExpMS", defaultValue: 1.0);
        var expMult = GetTargetMultiplier(upgradeTargets, "Hero.ExpMult", defaultValue: 1.0);

        var startingLevel = 1;
        var addLevel = 0;
        var growBaseLevel = 1.09;
        var baseXp = 1500.0;

        var activeClickTerm = Math.Log10((clicks + autoClicks) * expBoost + 1.0) + 1.0;
        var activeSpellTerm = Math.Log(castSpell * expBoost + 1.0) / 4.0 + 1.0;
        var manaTerm = manaSessionLog10 + 1.0;
        var clickableTerm = Math.Log(clickableCollect * expBoost + 1.0) / 2.0 + 1.0;
        var getExpActive = activeClickTerm * activeSpellTerm * manaTerm * clickableTerm;

        var getExpBuildings = Math.Pow(totalBuildings * expManaSources + 1.0, 0.855);
        var getExpMultPart = expMult * ((getExpBuildings + 100.0) * getExpActive - 100.0);
        var startingExp = baseXp * (1.0 - Math.Pow(growBaseLevel, startingLevel - 1.0)) / (1.0 - growBaseLevel);
        var totalExperience = startingExp + expStack * getExpMultPart;

        var levelArgument = 1.0 - totalExperience * (1.0 - growBaseLevel) / baseXp;
        var estimatedLevel = 0;

        if (levelArgument > 0.0 && !double.IsNaN(levelArgument) && !double.IsInfinity(levelArgument))
        {
            estimatedLevel = (int)Math.Floor(Math.Log(levelArgument, growBaseLevel)) + addLevel + 1;
        }

        return new HeroXpPartialEstimate(
            EstimatedLevel: estimatedLevel,
            ObservedLevel: observedLevel,
            DifferenceFromObserved: observedLevel > 0 ? estimatedLevel - observedLevel : 0,
            EstimateIsSourceComplete: false,
            Status: "PartialEstimateOnly",
            Assumptions: new HeroXpEstimateAssumptions(
                StartingLevel: startingLevel,
                AddLevel: addLevel,
                ExpBoost: expBoost,
                ExpManaSources: expManaSources,
                ExpMult: expMult,
                ExpStack: expStack,
                GrowBaseLevel: growBaseLevel,
                BaseXp: baseXp,
                Notes: "StartingLevel/AddLevel are assumed because runtime values are not directly serialized. ExpBoost/ExpManaSources/ExpMult use purchased-upgrade aggregate multipliers only."
            ),
            Inputs: new HeroXpEstimateInputs(
                Clicks: clicks,
                AutoClicks: autoClicks,
                CastSpell: castSpell,
                ManaSessionLog10: manaSessionLog10,
                ClickableCollect: clickableCollect,
                TotalBuildings: totalBuildings
            ),
            Components: new HeroXpEstimateComponents(
                ActiveClickTerm: activeClickTerm,
                ActiveSpellTerm: activeSpellTerm,
                ManaTerm: manaTerm,
                ClickableTerm: clickableTerm,
                GetExpActive: getExpActive,
                GetExpBuildings: getExpBuildings,
                GetExpMultPart: getExpMultPart,
                StartingExp: startingExp,
                TotalExperience: totalExperience,
                LevelCurveArgument: levelArgument
            ),
            Notes: new[]
            {
                "This is a lower/partial estimate using currently mapped save counters and upgrade target aggregates.",
                "If this estimate is below the observed level, missing StartingLevel/AddLevel/ExpBoost/ExpManaSources/ExpMult effects are likely responsible.",
                "If this estimate is above the observed level, one or more candidate save counters may have wrong scope for current Hero.GetExpActive."
            }
        );
    }

    private static List<HeroXpCandidateInput> BuildCandidateInputs(JsonElement root, IReadOnlyList<HeroXpUpgradeTarget> upgradeTargets)
    {
        var result = new List<HeroXpCandidateInput>();

        AddCandidate(result, root, "C", "Clicks/autoclicks pool", new[] { "Clicks", "AutoClicks" }, "Source-confirmed GetExpActive uses Statistic.Clicks + Statistic.AutoClicks multiplied by ExpBoost.");
        AddCandidate(result, root, "S", "Spell cast pool", new[] { "CastSpell" }, "Source-confirmed GetExpActive uses Statistic.CastSpell multiplied by ExpBoost.");
        AddCandidate(result, root, "M", "Mana pool", new[] { "ManaSession" }, "Source-confirmed GetExpActive uses Statistic.ManaSession.");
        AddCandidate(result, root, "V", "Clickable pool", new[] { "ClickableCollect" }, "Source-confirmed base GetExpActive uses Statistic.ClickableCollect multiplied by ExpBoost.");
        AddCandidate(result, root, "B", "Mana source / building source pool", new[] { "TotalBuildings" }, "Source-confirmed GetExpBuildings uses Statistic.TotalBuildings multiplied by ExpManaSources.");
        AddSyntheticCandidate(result, "Y", "ExpBoost", GetTargetMultiplier(upgradeTargets, "Hero.ExpBoost", 1.0).ToString("G17"), "PartialFromUpgradeTargets", "Candidate ExpBoost from purchased upgrade target aggregate Hero.ExpBoost only.");
        AddSyntheticCandidate(result, "Z", "ExpManaSources", GetTargetMultiplier(upgradeTargets, "Hero.ExpMS", 1.0).ToString("G17"), "PartialFromUpgradeTargets", "Candidate ExpManaSources from purchased upgrade target aggregate Hero.ExpMS only.");
        AddSyntheticCandidate(result, "H", "ExpMult", GetTargetMultiplier(upgradeTargets, "Hero.ExpMult", 1.0).ToString("G17"), "PartialFromUpgradeTargets", "Candidate ExpMult from purchased upgrade target aggregate Hero.ExpMult only. Defaults to 1 if absent.");
        AddCandidate(result, root, "ExpStack", "ExpStack multiplier", new[] { "CharExpMult" }, "Source-confirmed SaveData CharExpMult = CurrentHero.ExpStack.Value.");
        AddCandidate(result, root, "X", "ExpFlat / StartingLevel flat XP candidate", new[] { "CharExp" }, "Source-confirmed SaveData CharExp = CurrentHero.ExpFlat.Value. StartingLevel runtime value is not directly serialized.");

        return result;
    }

    private static void AddCandidate(List<HeroXpCandidateInput> result, JsonElement root, string symbol, string poolName, IReadOnlyList<string> candidateFields, string notes)
    {
        var values = new List<string>();
        var fieldEntries = new List<HeroXpFieldValue>();

        foreach (var field in candidateFields)
        {
            if (!root.TryGetProperty(field, out var value))
            {
                continue;
            }

            var rendered = RenderValue(value);
            values.Add(field + "=" + rendered);
            fieldEntries.Add(new HeroXpFieldValue(field, rendered, value.ValueKind.ToString()));
        }

        result.Add(new HeroXpCandidateInput(symbol, poolName, fieldEntries, values.Count == 0 ? "" : string.Join("; ", values), values.Count == 0 ? "MissingOrRuntimeDerived" : "SourceConfirmedSaveField", notes));
    }

    private static void AddSyntheticCandidate(List<HeroXpCandidateInput> result, string symbol, string poolName, string value, string status, string notes)
    {
        result.Add(new HeroXpCandidateInput(symbol, poolName, Array.Empty<HeroXpFieldValue>(), value, status, notes));
    }

    private static double GetTargetMultiplier(IReadOnlyList<HeroXpUpgradeTarget> targets, string targetName, double defaultValue)
    {
        var target = targets.FirstOrDefault(x => string.Equals(x.Target, targetName, StringComparison.OrdinalIgnoreCase));
        return target is null || target.MultiplierProduct <= 0.0 ? defaultValue : target.MultiplierProduct;
    }

    private static List<HeroXpSaveField> BuildRelevantSaveFields(JsonElement root)
    {
        var result = new List<HeroXpSaveField>();
        var regex = new Regex("Hero|CharExp|Exp|Click|Cast|Spell|Mana|Void|Shard|Building|Time|Skip|Collect|Soul|Trial|Achiev|BoughtUpgrades|TotalBuildings", RegexOptions.IgnoreCase);

        foreach (var property in root.EnumerateObject().OrderBy(x => x.Name))
        {
            if (!regex.IsMatch(property.Name))
            {
                continue;
            }

            result.Add(new HeroXpSaveField(property.Name, property.Value.ValueKind.ToString(), RenderValue(property.Value), GuessFieldNotes(property.Name)));
        }

        return result;
    }

    private static List<HeroXpUpgradeTarget> LoadUpgradeXpTargets()
    {
        var aggregatePath = Path.Combine(Directory.GetCurrentDirectory(), "zz_save_upgrade_target_aggregate.json");
        var result = new List<HeroXpUpgradeTarget>();

        if (!File.Exists(aggregatePath))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(aggregatePath));
            var root = doc.RootElement;

            if (!root.TryGetProperty("Targets", out var targets) || targets.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var target in targets.EnumerateArray())
            {
                var name = GetString(target, "Target");

                if (!name.Contains("Exp", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("Hero.", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("Pet.BonusExp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(
                    new HeroXpUpgradeTarget(
                        Target: name,
                        PurchasedCount: GetInt(target, "PurchasedCount"),
                        AdditiveSum: GetDouble(target, "AdditiveSum"),
                        MultiplierProduct: GetDouble(target, "MultiplierProduct"),
                        FormulaCount: GetInt(target, "FormulaCount"),
                        Notes: "Upgrade target aggregate that may affect hero/pet XP or hero-level-adjacent calculations. Needs full runtime effect stack for exact level."
                    )
                );
            }
        }
        catch
        {
            return result;
        }

        return result.OrderByDescending(x => x.PurchasedCount).ThenBy(x => x.Target).ToList();
    }

    private static string GuessFieldNotes(string name)
    {
        if (name.Equals("HeroMaxLevelAllTime", StringComparison.OrdinalIgnoreCase))
        {
            return "All-time max hero level, not current run level.";
        }

        if (name.Equals("CharExp", StringComparison.OrdinalIgnoreCase))
        {
            return "Source-confirmed SaveData CharExp = CurrentHero.ExpFlat.Value.";
        }

        if (name.Equals("CharExpMult", StringComparison.OrdinalIgnoreCase))
        {
            return "Source-confirmed SaveData CharExpMult = CurrentHero.ExpStack.Value.";
        }

        if (name.Contains("Realm", StringComparison.OrdinalIgnoreCase))
        {
            return "Realm-scoped counter. Hero.GetExpActive uses non-realm/session counters where source-confirmed.";
        }

        return "Candidate hero-XP-related save field.";
    }

    private static double ReadBigNumberDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        if (LooksLikeBigNumber(value))
        {
            var mantissa = GetDouble(value, "Mantissa");
            var exponent = GetInt(value, "Exponent");

            if (exponent > 308)
            {
                return double.PositiveInfinity;
            }

            return mantissa * Math.Pow(10.0, exponent);
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

    private static double ReadBigNumberLog10(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        if (LooksLikeBigNumber(value))
        {
            var mantissa = GetDouble(value, "Mantissa");
            var exponent = GetInt(value, "Exponent");
            return exponent + Math.Log10(Math.Max(mantissa, double.Epsilon));
        }

        var number = ReadBigNumberDouble(root, propertyName);
        return number > 0.0 && !double.IsInfinity(number) ? Math.Log10(number) : 0.0;
    }

    private static string RenderValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Array => "Array length " + value.GetArrayLength(),
            JsonValueKind.Object => LooksLikeBigNumber(value) ? ReadBigNumberScientific(value) : "Object properties: " + string.Join(", ", value.EnumerateObject().Select(x => x.Name).Take(12)),
            _ => value.GetRawText()
        };
    }

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object && value.TryGetProperty("Mantissa", out _) && value.TryGetProperty("Exponent", out _);
    }

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return LooksLikeBigNumber(value) ? ReadBigNumberScientific(value) : RenderValue(value);
    }

    private static string ReadBigNumberScientific(JsonElement value)
    {
        return GetDouble(value, "Mantissa") + "e" + GetInt(value, "Exponent");
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

    private static Dictionary<int, string> LoadEnumMap(string workspacePath, string fileName, string enumName)
    {
        var result = new Dictionary<int, string>();
        var file = WorkspacePaths.FindSourceFile(workspacePath, fileName);

        if (file is null)
        {
            return result;
        }

        var text = File.ReadAllText(file);
        var match = Regex.Match(text, @"enum\s+" + Regex.Escape(enumName) + @"\s*\{(?<body>[\s\S]*?)\}", RegexOptions.IgnoreCase);

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

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
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
        var text = GetString(element, propertyName);
        return int.TryParse(text, out var value) ? value : 0;
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        var text = GetString(element, propertyName);
        return double.TryParse(text, out var value) ? value : 0.0;
    }

    private sealed record SaveHeroXpMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        HeroXpCharacterContext Character,
        HeroXpFormulaModel SourceConfirmedFormula,
        IReadOnlyList<HeroXpCandidateInput> CandidateInputs,
        HeroXpPartialEstimate PartialEstimate,
        IReadOnlyList<HeroXpSaveField> RelevantSaveFields,
        IReadOnlyList<HeroXpUpgradeTarget> UpgradeXpTargets,
        IReadOnlyList<string> MissingOrUnconfirmed
    );

    private sealed record HeroXpCharacterContext(int HeroId, string ClassName, int HeroMaxLevelAllTime, string CharExpFlat, string CharExpStack, int HeroPlayedTime, string HeroSkipedPlayedTime, bool CurrentLevelMapped, string Notes);

    private sealed record HeroXpFormulaModel(string FormulaName, string GetExpActive, string GetExpBuildings, string GetExpMultPart, string RecalculateExp, string LevelCurve, IReadOnlyList<string> Notes);

    private sealed record HeroXpCandidateInput(string FormulaSymbolOrPool, string PoolName, IReadOnlyList<HeroXpFieldValue> CandidateSaveFields, string CandidateValue, string MappingStatus, string Notes);

    private sealed record HeroXpFieldValue(string Name, string Value, string JsonKind);

    private sealed record HeroXpSaveField(string Name, string JsonKind, string Value, string Notes);

    private sealed record HeroXpUpgradeTarget(string Target, int PurchasedCount, double AdditiveSum, double MultiplierProduct, int FormulaCount, string Notes);

    private sealed record HeroXpPartialEstimate(int EstimatedLevel, int ObservedLevel, int DifferenceFromObserved, bool EstimateIsSourceComplete, string Status, HeroXpEstimateAssumptions Assumptions, HeroXpEstimateInputs Inputs, HeroXpEstimateComponents Components, IReadOnlyList<string> Notes);

    private sealed record HeroXpEstimateAssumptions(int StartingLevel, int AddLevel, double ExpBoost, double ExpManaSources, double ExpMult, double ExpStack, double GrowBaseLevel, double BaseXp, string Notes);

    private sealed record HeroXpEstimateInputs(double Clicks, double AutoClicks, double CastSpell, double ManaSessionLog10, double ClickableCollect, double TotalBuildings);

    private sealed record HeroXpEstimateComponents(double ActiveClickTerm, double ActiveSpellTerm, double ManaTerm, double ClickableTerm, double GetExpActive, double GetExpBuildings, double GetExpMultPart, double StartingExp, double TotalExperience, double LevelCurveArgument);
}
