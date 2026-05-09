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
        var upgradeXpTargets = LoadUpgradeXpTargets(workspacePath);
        var challengeXpRewards = LoadChallengeXpRewards(root, workspacePath);
        var candidateInputs = BuildCandidateInputs(root, upgradeXpTargets, challengeXpRewards);
        var estimate = BuildEstimate(root, upgradeXpTargets, challengeXpRewards, observedLevel);

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
                    "This estimate uses save counters, purchased-upgrade target aggregates, and completed challenge XP reward mapping when zz_challenge_xp_reward_map.json exists.",
                    "This estimate still does not include full runtime effect stack from attributes, items, pantheon/gods, realm systems, or special source-hook effects unless those effects are already represented in mapped upgrade/challenge sources.",
                    "If the estimate differs from the observed level, missing runtime StartingLevel/AddLevel/ExpBoost/ExpManaSources/ExpMult effects are still likely."
                }
            ),
            CandidateInputs: candidateInputs,
            PartialEstimate: estimate,
            RelevantSaveFields: saveFields,
            UpgradeXpTargets: upgradeXpTargets,
            ChallengeXpRewards: challengeXpRewards,
            MissingOrUnconfirmed: new[]
            {
                "Exact current Hero.Level save field does not exist; level is runtime-derived.",
                "StartingLevel current runtime value is not directly serialized; this command estimates it from base 1 plus completed challenge StartingLevel rewards.",
                "AddLevel current runtime value is not directly serialized and remains 0 until source-confirmed.",
                "Full ExpBoost after all runtime effects may exceed purchased-upgrade and challenge reward estimates.",
                "Full ExpManaSources after all runtime effects may exceed purchased-upgrade and challenge reward estimates.",
                "Full ExpMult after all runtime effects is not fully mapped.",
                "Items/equipment effects are not included yet.",
                "Attribute, pantheon/god, realm, and special source-hook effects are not included yet.",
                "LevelReduction is tracked from challenges but is not folded into AddLevel until source-confirmed."
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
        Console.WriteLine("");

        Console.WriteLine("Challenge XP rewards:");
        Console.WriteLine($"  Loaded={challengeXpRewards.Loaded}");
        Console.WriteLine($"  Source={challengeXpRewards.SourceFile}");
        Console.WriteLine($"  Completed IDs={challengeXpRewards.CompletedChallengeIds.Count}");
        Console.WriteLine($"  Applied rewards={challengeXpRewards.AppliedRewards.Count}");
        Console.WriteLine($"  Challenge ExpBoost multiplier={challengeXpRewards.Totals.ExpBoostMultiplier}");
        Console.WriteLine($"  Challenge ExpManaSources multiplier={challengeXpRewards.Totals.ExpManaSourcesMultiplier}");
        Console.WriteLine($"  Challenge StartingLevel add={challengeXpRewards.Totals.StartingLevelAdd}");
        Console.WriteLine($"  Challenge LevelReduction add={challengeXpRewards.Totals.LevelReductionAdd}");
        Console.WriteLine($"  Notes={challengeXpRewards.Notes}");
    }

    private static HeroXpPartialEstimate BuildEstimate(
        JsonElement root,
        IReadOnlyList<HeroXpUpgradeTarget> upgradeTargets,
        ChallengeXpRewardApplication challengeXpRewards,
        int observedLevel)
    {
        var clicks = ReadBigNumberDouble(root, "Clicks");
        var autoClicks = ReadBigNumberDouble(root, "AutoClicks");
        var castSpell = ReadBigNumberDouble(root, "CastSpell");
        var manaSessionLog10 = ReadBigNumberLog10(root, "ManaSession");
        var clickableCollect = ReadBigNumberDouble(root, "ClickableCollect");
        var totalBuildings = ReadBigNumberDouble(root, "TotalBuildings");

        var expStack = Math.Max(1.0, ReadBigNumberDouble(root, "CharExpMult"));
        var challengeTotals = challengeXpRewards.Totals;

        var expBoost =
            GetTargetMultiplier(upgradeTargets, "Hero.ExpBoost", defaultValue: 1.0)
            * challengeTotals.ExpBoostMultiplier;

        var expManaSources =
            GetTargetMultiplier(upgradeTargets, "Hero.ExpMS", defaultValue: 1.0)
            * challengeTotals.ExpManaSourcesMultiplier;

        var expMult = GetTargetMultiplier(upgradeTargets, "Hero.ExpMult", defaultValue: 1.0);

        var startingLevel = 1 + challengeTotals.StartingLevelAdd;
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
                Notes: "StartingLevel uses base 1 plus completed challenge StartingLevel rewards. ExpBoost/ExpManaSources use purchased-upgrade aggregate multipliers multiplied by completed challenge reward multipliers. AddLevel remains 0 until source-confirmed. LevelReduction is tracked but not folded into AddLevel."
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
                "This is a partial estimate using currently mapped save counters, upgrade target aggregates, and completed challenge XP rewards.",
                "If this estimate is below the observed level, missing StartingLevel/AddLevel/ExpBoost/ExpManaSources/ExpMult effects are likely responsible.",
                "If this estimate is above the observed level, one or more mapped challenge reward rows may be duplicated or one or more candidate save counters may have wrong scope for current Hero.GetExpActive."
            }
        );
    }

    private static List<HeroXpCandidateInput> BuildCandidateInputs(
        JsonElement root,
        IReadOnlyList<HeroXpUpgradeTarget> upgradeTargets,
        ChallengeXpRewardApplication challengeXpRewards)
    {
        var result = new List<HeroXpCandidateInput>();

        AddCandidate(result, root, "C", "Clicks/autoclicks pool", new[] { "Clicks", "AutoClicks" }, "Source-confirmed GetExpActive uses Statistic.Clicks + Statistic.AutoClicks multiplied by ExpBoost.");
        AddCandidate(result, root, "S", "Spell cast pool", new[] { "CastSpell" }, "Source-confirmed GetExpActive uses Statistic.CastSpell multiplied by ExpBoost.");
        AddCandidate(result, root, "M", "Mana pool", new[] { "ManaSession" }, "Source-confirmed GetExpActive uses Statistic.ManaSession.");
        AddCandidate(result, root, "V", "Clickable pool", new[] { "ClickableCollect" }, "Source-confirmed base GetExpActive uses Statistic.ClickableCollect multiplied by ExpBoost.");
        AddCandidate(result, root, "B", "Mana source / building source pool", new[] { "TotalBuildings" }, "Source-confirmed GetExpBuildings uses Statistic.TotalBuildings multiplied by ExpManaSources.");

        var challengeTotals = challengeXpRewards.Totals;

        AddSyntheticCandidate(
            result,
            "Y",
            "ExpBoost",
            (GetTargetMultiplier(upgradeTargets, "Hero.ExpBoost", 1.0) * challengeTotals.ExpBoostMultiplier).ToString("G17"),
            "PartialFromUpgradeTargetsAndCompletedChallenges",
            "Candidate ExpBoost from purchased upgrade target aggregate Hero.ExpBoost multiplied by completed challenge ExpBoost rewards."
        );

        AddSyntheticCandidate(
            result,
            "Z",
            "ExpManaSources",
            (GetTargetMultiplier(upgradeTargets, "Hero.ExpMS", 1.0) * challengeTotals.ExpManaSourcesMultiplier).ToString("G17"),
            "PartialFromUpgradeTargetsAndCompletedChallenges",
            "Candidate ExpManaSources from purchased upgrade target aggregate Hero.ExpMS multiplied by completed challenge ExpManaSources rewards."
        );

        AddSyntheticCandidate(
            result,
            "StartingLevel",
            "StartingLevel",
            (1 + challengeTotals.StartingLevelAdd).ToString("G17"),
            "PartialFromCompletedChallenges",
            "Candidate StartingLevel from base 1 plus completed challenge StartingLevel additive rewards."
        );

        AddSyntheticCandidate(
            result,
            "LevelReduction",
            "LevelReduction",
            challengeTotals.LevelReductionAdd.ToString("G17"),
            "TrackedButNotApplied",
            "Completed challenge LevelReduction additive rewards are tracked but not folded into AddLevel until source-confirmed."
        );

        AddSyntheticCandidate(
            result,
            "H",
            "ExpMult",
            GetTargetMultiplier(upgradeTargets, "Hero.ExpMult", 1.0).ToString("G17"),
            "PartialFromUpgradeTargets",
            "Candidate ExpMult from purchased upgrade target aggregate Hero.ExpMult only. Defaults to 1 if absent."
        );

        AddCandidate(result, root, "ExpStack", "ExpStack multiplier", new[] { "CharExpMult" }, "Source-confirmed SaveData CharExpMult = CurrentHero.ExpStack.Value.");
        AddCandidate(result, root, "X", "ExpFlat / StartingLevel flat XP candidate", new[] { "CharExp" }, "Source-confirmed SaveData CharExp = CurrentHero.ExpFlat.Value. StartingLevel runtime value is not directly serialized.");

        return result;
    }

    private static void AddCandidate(
        List<HeroXpCandidateInput> result,
        JsonElement root,
        string symbol,
        string poolName,
        IReadOnlyList<string> candidateFields,
        string notes)
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

        result.Add(
            new HeroXpCandidateInput(
                symbol,
                poolName,
                fieldEntries,
                values.Count == 0 ? "" : string.Join("; ", values),
                values.Count == 0 ? "MissingOrRuntimeDerived" : "SourceConfirmedSaveField",
                notes
            )
        );
    }

    private static void AddSyntheticCandidate(
        List<HeroXpCandidateInput> result,
        string symbol,
        string poolName,
        string value,
        string status,
        string notes)
    {
        result.Add(
            new HeroXpCandidateInput(
                symbol,
                poolName,
                Array.Empty<HeroXpFieldValue>(),
                value,
                status,
                notes
            )
        );
    }

    private static double GetTargetMultiplier(
        IReadOnlyList<HeroXpUpgradeTarget> targets,
        string targetName,
        double defaultValue)
    {
        var target = targets.FirstOrDefault(x => string.Equals(x.Target, targetName, StringComparison.OrdinalIgnoreCase));
        return target is null || target.MultiplierProduct <= 0.0
            ? defaultValue
            : target.MultiplierProduct;
    }

    private static ChallengeXpRewardApplication LoadChallengeXpRewards(JsonElement saveRoot, string workspacePath)
    {
        var completedIds = ReadCompletedChallengeIds(saveRoot);

        var candidateMapPaths = new[]
        {
            Path.Combine(workspacePath, "zz", "zz_challenge_xp_reward_map.json"),
            Path.Combine(workspacePath, "zz_challenge_xp_reward_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "zz", "zz_challenge_xp_reward_map.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "zz_challenge_xp_reward_map.json")
        };

        var mapPath = candidateMapPaths.FirstOrDefault(File.Exists);

        if (string.IsNullOrWhiteSpace(mapPath))
        {
            return ChallengeXpRewardApplication.Empty(
                completedIds,
                "Challenge XP reward map was not found. Checked: " + string.Join("; ", candidateMapPaths)
            );
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(mapPath));
            var mapRoot = doc.RootElement;

            if (!mapRoot.TryGetProperty("Rewards", out var rewards)
                || rewards.ValueKind != JsonValueKind.Array)
            {
                return ChallengeXpRewardApplication.Empty(
                    completedIds,
                    "Challenge XP reward map loaded but did not contain Rewards[]. Source=" + mapPath
                );
            }

            var applied = new List<ChallengeXpAppliedReward>();
            var totals = new ChallengeXpRewardTotals(1.0, 1.0, 0, 0);
            var rewardRows = 0;
            var matchedRows = 0;

            var appliedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var reward in rewards.EnumerateArray())
            {
                rewardRows++;

                var challengeId = GetInt(reward, "ChallengeId");
                if (challengeId <= 0 || !completedIds.Contains(challengeId))
                {
                    continue;
                }

                matchedRows++;

                var target = GetString(reward, "Target");
                var add = GetDouble(reward, "Add");
                var mult = GetDouble(reward, "Mult");
                var description = GetString(reward, "RewardDescription");
                var sourceLine = GetInt(reward, "SourceLine");

                var key = string.Join(
                    "|",
                    challengeId,
                    target,
                    Math.Round(add, 8).ToString("G17"),
                    Math.Round(mult, 8).ToString("G17"),
                    description,
                    sourceLine
                );

                if (!appliedKeys.Add(key))
                {
                    continue;
                }

                if (target.Equals("Hero.ExpBoost", StringComparison.OrdinalIgnoreCase))
                {
                    totals = totals with
                    {
                        ExpBoostMultiplier = totals.ExpBoostMultiplier * SafeMultiplier(mult)
                    };
                }
                else if (target.Equals("Hero.ExpManaSources", StringComparison.OrdinalIgnoreCase))
                {
                    totals = totals with
                    {
                        ExpManaSourcesMultiplier = totals.ExpManaSourcesMultiplier * SafeMultiplier(mult)
                    };
                }
                else if (target.Equals("Hero.StartingLevel", StringComparison.OrdinalIgnoreCase))
                {
                    totals = totals with
                    {
                        StartingLevelAdd = totals.StartingLevelAdd + (int)Math.Round(add)
                    };
                }
                else if (target.Equals("LevelReduction", StringComparison.OrdinalIgnoreCase))
                {
                    totals = totals with
                    {
                        LevelReductionAdd = totals.LevelReductionAdd + (int)Math.Round(add)
                    };
                }
                else
                {
                    continue;
                }

                applied.Add(
                    new ChallengeXpAppliedReward(
                        ChallengeId: challengeId,
                        Target: target,
                        Add: add,
                        Mult: mult,
                        RewardDescription: description,
                        SourceLine: sourceLine
                    )
                );
            }

            return new ChallengeXpRewardApplication(
                Loaded: true,
                SourceFile: mapPath,
                CompletedChallengeIds: completedIds,
                AppliedRewards: applied,
                Totals: totals,
                Notes: "Rows=" + rewardRows +
                       "; MatchedCompletedChallengeRows=" + matchedRows +
                       "; AppliedXpRelevantRows=" + applied.Count
            );
        }
        catch (Exception ex)
        {
            return ChallengeXpRewardApplication.Empty(
                completedIds,
                "Failed to read challenge XP reward map: " + ex.Message
            );
        }
    }

    private static HashSet<int> ReadCompletedChallengeIds(JsonElement root)
    {
        var result = new HashSet<int>();

        if (!root.TryGetProperty("CompletedChIDs", out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            var id = 0;

            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number))
            {
                id = number;
            }
            else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out number))
            {
                id = number;
            }

            if (id > 0)
            {
                result.Add(id);
            }
        }

        return result;
    }

    private static double SafeMultiplier(double value)
    {
        return value <= 0.0 ? 1.0 : value;
    }

    private static List<HeroXpSaveField> BuildRelevantSaveFields(JsonElement root)
    {
        var result = new List<HeroXpSaveField>();

        var regex = new Regex(
            "Hero|CharExp|Exp|Click|Cast|Spell|Mana|Void|Shard|Building|Time|Skip|Collect|Soul|Trial|Achiev|BoughtUpgrades|TotalBuildings",
            RegexOptions.IgnoreCase
        );

        foreach (var property in root.EnumerateObject().OrderBy(x => x.Name))
        {
            if (!regex.IsMatch(property.Name))
            {
                continue;
            }

            result.Add(
                new HeroXpSaveField(
                    property.Name,
                    property.Value.ValueKind.ToString(),
                    RenderValue(property.Value),
                    GuessFieldNotes(property.Name)
                )
            );
        }

        return result;
    }

    private static List<HeroXpUpgradeTarget> LoadUpgradeXpTargets(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "zz", "zz_save_upgrade_target_aggregate.json"),
            Path.Combine(workspacePath, "zz_save_upgrade_target_aggregate.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "zz", "zz_save_upgrade_target_aggregate.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "zz_save_upgrade_target_aggregate.json")
        };

        var aggregatePath = candidates.FirstOrDefault(File.Exists);
        var result = new List<HeroXpUpgradeTarget>();

        if (string.IsNullOrWhiteSpace(aggregatePath))
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(aggregatePath));
            var root = doc.RootElement;

            if (!root.TryGetProperty("Targets", out var targets)
                || targets.ValueKind != JsonValueKind.Array)
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

        return result
            .OrderByDescending(x => x.PurchasedCount)
            .ThenBy(x => x.Target)
            .ToList();
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
        return number > 0.0 && !double.IsInfinity(number)
            ? Math.Log10(number)
            : 0.0;
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
            JsonValueKind.Object => LooksLikeBigNumber(value)
                ? ReadBigNumberScientific(value)
                : "Object properties: " + string.Join(", ", value.EnumerateObject().Select(x => x.Name).Take(12)),
            _ => value.GetRawText()
        };
    }

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out _)
            && value.TryGetProperty("Exponent", out _);
    }

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return LooksLikeBigNumber(value)
            ? ReadBigNumberScientific(value)
            : RenderValue(value);
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
        ChallengeXpRewardApplication ChallengeXpRewards,
        IReadOnlyList<string> MissingOrUnconfirmed
    );

    private sealed record HeroXpCharacterContext(
        int HeroId,
        string ClassName,
        int HeroMaxLevelAllTime,
        string CharExpFlat,
        string CharExpStack,
        int HeroPlayedTime,
        string HeroSkipedPlayedTime,
        bool CurrentLevelMapped,
        string Notes
    );

    private sealed record HeroXpFormulaModel(
        string FormulaName,
        string GetExpActive,
        string GetExpBuildings,
        string GetExpMultPart,
        string RecalculateExp,
        string LevelCurve,
        IReadOnlyList<string> Notes
    );

    private sealed record HeroXpCandidateInput(
        string FormulaSymbolOrPool,
        string PoolName,
        IReadOnlyList<HeroXpFieldValue> CandidateSaveFields,
        string CandidateValue,
        string MappingStatus,
        string Notes
    );

    private sealed record HeroXpFieldValue(
        string Name,
        string Value,
        string JsonKind
    );

    private sealed record HeroXpSaveField(
        string Name,
        string JsonKind,
        string Value,
        string Notes
    );

    private sealed record HeroXpUpgradeTarget(
        string Target,
        int PurchasedCount,
        double AdditiveSum,
        double MultiplierProduct,
        int FormulaCount,
        string Notes
    );

    private sealed record ChallengeXpRewardApplication(
        bool Loaded,
        string SourceFile,
        IReadOnlySet<int> CompletedChallengeIds,
        IReadOnlyList<ChallengeXpAppliedReward> AppliedRewards,
        ChallengeXpRewardTotals Totals,
        string Notes
    )
    {
        public static ChallengeXpRewardApplication Empty(
            IReadOnlySet<int> completedChallengeIds,
            string notes)
        {
            return new ChallengeXpRewardApplication(
                Loaded: false,
                SourceFile: "",
                CompletedChallengeIds: completedChallengeIds,
                AppliedRewards: Array.Empty<ChallengeXpAppliedReward>(),
                Totals: new ChallengeXpRewardTotals(1.0, 1.0, 0, 0),
                Notes: notes
            );
        }
    }

    private sealed record ChallengeXpRewardTotals(
        double ExpBoostMultiplier,
        double ExpManaSourcesMultiplier,
        int StartingLevelAdd,
        int LevelReductionAdd
    );

    private sealed record ChallengeXpAppliedReward(
        int ChallengeId,
        string Target,
        double Add,
        double Mult,
        string RewardDescription,
        int SourceLine
    );

    private sealed record HeroXpPartialEstimate(
        int EstimatedLevel,
        int ObservedLevel,
        int DifferenceFromObserved,
        bool EstimateIsSourceComplete,
        string Status,
        HeroXpEstimateAssumptions Assumptions,
        HeroXpEstimateInputs Inputs,
        HeroXpEstimateComponents Components,
        IReadOnlyList<string> Notes
    );

    private sealed record HeroXpEstimateAssumptions(
        int StartingLevel,
        int AddLevel,
        double ExpBoost,
        double ExpManaSources,
        double ExpMult,
        double ExpStack,
        double GrowBaseLevel,
        double BaseXp,
        string Notes
    );

    private sealed record HeroXpEstimateInputs(
        double Clicks,
        double AutoClicks,
        double CastSpell,
        double ManaSessionLog10,
        double ClickableCollect,
        double TotalBuildings
    );

    private sealed record HeroXpEstimateComponents(
        double ActiveClickTerm,
        double ActiveSpellTerm,
        double ManaTerm,
        double ClickableTerm,
        double GetExpActive,
        double GetExpBuildings,
        double GetExpMultPart,
        double StartingExp,
        double TotalExperience,
        double LevelCurveArgument
    );
}