using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveUpgradeFormulaDependenciesCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-upgrade-formula-dependencies .\zz_save_upgrade_target_aggregate.json .\save_calculation_context.json [zz_save_upgrade_formula_dependencies.json]");
            return;
        }

        var aggregatePath = Path.GetFullPath(args[1]);
        var contextPath = Path.GetFullPath(args[2]);
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\zz_save_upgrade_formula_dependencies.json";

        if (!File.Exists(aggregatePath))
        {
            Console.WriteLine($"Upgrade target aggregate not found: {aggregatePath}");
            return;
        }

        if (!File.Exists(contextPath))
        {
            Console.WriteLine($"Save calculation context not found: {contextPath}");
            return;
        }

        using var aggregateDoc = JsonDocument.Parse(File.ReadAllText(aggregatePath));
        using var contextDoc = JsonDocument.Parse(File.ReadAllText(contextPath));

        var aggregateRoot = aggregateDoc.RootElement;
        var contextRoot = contextDoc.RootElement;
        var resolver = new SaveContextValueResolver(contextRoot);

        var formulaDependencies = new List<FormulaDependencyEntry>();
        var conditionDependencies = new List<ConditionDependencyEntry>();

        if (aggregateRoot.TryGetProperty("Targets", out var targets)
            && targets.ValueKind == JsonValueKind.Array)
        {
            foreach (var target in targets.EnumerateArray())
            {
                var targetName = GetString(target, "Target");

                if (target.TryGetProperty("FormulaUpgrades", out var formulas)
                    && formulas.ValueKind == JsonValueKind.Array)
                {
                    foreach (var formula in formulas.EnumerateArray())
                    {
                        var secondary = GetString(formula, "Secondary");
                        var resolved = resolver.Resolve(secondary);

                        formulaDependencies.Add(
                            new FormulaDependencyEntry(
                                UpgradeId: GetInt(formula, "Id"),
                                UpgradeName: GetString(formula, "Name"),
                                Target: targetName,
                                Effect: GetString(formula, "Effect"),
                                Addendum: GetString(formula, "Addendum"),
                                Multiplier: GetString(formula, "Multiplier"),
                                Secondary: secondary,
                                Resolved: resolved.Resolved,
                                ResolvedValue: resolved.Value,
                                ValueKind: resolved.ValueKind,
                                ResolutionSource: resolved.Source,
                                Notes: resolved.Notes,
                                FormulaSummary: GetString(formula, "FormulaSummary")
                            )
                        );
                    }
                }

                if (target.TryGetProperty("ConditionedUpgrades", out var conditions)
                    && conditions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var condition in conditions.EnumerateArray())
                    {
                        var parameter = GetString(condition, "ConditionParameter");
                        var resolved = resolver.Resolve(parameter);

                        conditionDependencies.Add(
                            new ConditionDependencyEntry(
                                UpgradeId: GetInt(condition, "Id"),
                                UpgradeName: GetString(condition, "Name"),
                                Target: targetName,
                                ConditionParameter: parameter,
                                ConditionArgument: GetString(condition, "ConditionArgument"),
                                Resolved: resolved.Resolved,
                                ResolvedValue: resolved.Value,
                                ValueKind: resolved.ValueKind,
                                ResolutionSource: resolved.Source,
                                Notes: resolved.Notes
                            )
                        );
                    }
                }
            }
        }

        var formulaGroups = formulaDependencies
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Secondary) ? "(blank)" : x.Secondary)
            .Select(g => new DependencySummaryEntry(
                Name: g.Key,
                UsageCount: g.Count(),
                ResolvedCount: g.Count(x => x.Resolved),
                UnresolvedCount: g.Count(x => !x.Resolved),
                ExampleUpgradeIds: g.Select(x => x.UpgradeId).Take(12).ToList(),
                ExampleUpgradeNames: g.Select(x => x.UpgradeName).Where(x => !string.IsNullOrWhiteSpace(x)).Take(8).ToList(),
                ResolutionSource: g.Select(x => x.ResolutionSource).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "",
                Notes: g.Select(x => x.Notes).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? ""
            ))
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.Name)
            .ToList();

        var conditionGroups = conditionDependencies
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ConditionParameter) ? "(blank)" : x.ConditionParameter)
            .Select(g => new DependencySummaryEntry(
                Name: g.Key,
                UsageCount: g.Count(),
                ResolvedCount: g.Count(x => x.Resolved),
                UnresolvedCount: g.Count(x => !x.Resolved),
                ExampleUpgradeIds: g.Select(x => x.UpgradeId).Take(12).ToList(),
                ExampleUpgradeNames: g.Select(x => x.UpgradeName).Where(x => !string.IsNullOrWhiteSpace(x)).Take(8).ToList(),
                ResolutionSource: g.Select(x => x.ResolutionSource).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "",
                Notes: g.Select(x => x.Notes).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? ""
            ))
            .OrderByDescending(x => x.UsageCount)
            .ThenBy(x => x.Name)
            .ToList();

        var export = new UpgradeFormulaDependencyExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            AggregateFile: aggregatePath,
            CalculationContextFile: contextPath,
            Summary: new DependencyMapSummary(
                FormulaDependencyCount: formulaDependencies.Count,
                FormulaResolvedCount: formulaDependencies.Count(x => x.Resolved),
                FormulaUnresolvedCount: formulaDependencies.Count(x => !x.Resolved),
                UniqueFormulaDependencyCount: formulaGroups.Count,
                ConditionDependencyCount: conditionDependencies.Count,
                ConditionResolvedCount: conditionDependencies.Count(x => x.Resolved),
                ConditionUnresolvedCount: conditionDependencies.Count(x => !x.Resolved),
                UniqueConditionDependencyCount: conditionGroups.Count
            ),
            FormulaDependencySummary: formulaGroups,
            ConditionDependencySummary: conditionGroups,
            FormulaDependencies: formulaDependencies.OrderBy(x => x.Secondary).ThenBy(x => x.UpgradeId).ToList(),
            ConditionDependencies: conditionDependencies.OrderBy(x => x.ConditionParameter).ThenBy(x => x.UpgradeId).ToList(),
            Notes: new[]
            {
                "Formula dependencies come from FormulaUpgrades[].Secondary in zz_save_upgrade_target_aggregate.json.",
                "Condition dependencies come from ConditionedUpgrades[].ConditionParameter in zz_save_upgrade_target_aggregate.json.",
                "Resolved values are pulled from save_calculation_context.json and zz_save_achievement_map.json where mappings are known.",
                "Trial.Completed is source-confirmed as Trial.completed. Trial.TotalCompleted is source-confirmed as Trial.totalCompleted.",
                "Hero.Level intentionally remains unresolved because current run character level is not directly mapped yet."
            }
        );

        var json = JsonSerializer.Serialize(
            export,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save upgrade formula dependencies");
        Console.WriteLine("---------------------------------");
        Console.WriteLine($"Aggregate: {aggregatePath}");
        Console.WriteLine($"Context:   {contextPath}");
        Console.WriteLine($"Output:    {Path.GetFullPath(outputPath)}");
        Console.WriteLine("");
        Console.WriteLine($"Formula dependencies: {export.Summary.FormulaDependencyCount}");
        Console.WriteLine($"  Resolved:           {export.Summary.FormulaResolvedCount}");
        Console.WriteLine($"  Unresolved:         {export.Summary.FormulaUnresolvedCount}");
        Console.WriteLine($"  Unique:             {export.Summary.UniqueFormulaDependencyCount}");
        Console.WriteLine("");
        Console.WriteLine($"Condition dependencies: {export.Summary.ConditionDependencyCount}");
        Console.WriteLine($"  Resolved:             {export.Summary.ConditionResolvedCount}");
        Console.WriteLine($"  Unresolved:           {export.Summary.ConditionUnresolvedCount}");
        Console.WriteLine($"  Unique:               {export.Summary.UniqueConditionDependencyCount}");
        Console.WriteLine("");
        Console.WriteLine("Formula dependency summary:");

        foreach (var dependency in export.FormulaDependencySummary)
        {
            Console.WriteLine($"  {dependency.Name}: uses={dependency.UsageCount}, resolved={dependency.ResolvedCount}, unresolved={dependency.UnresolvedCount}");
        }

        Console.WriteLine("");
        Console.WriteLine("Top unresolved condition dependencies:");

        foreach (var dependency in export.ConditionDependencySummary.Where(x => x.UnresolvedCount > 0).Take(30))
        {
            Console.WriteLine($"  {dependency.Name}: uses={dependency.UsageCount}, unresolved={dependency.UnresolvedCount}");
        }
    }

    private sealed class SaveContextValueResolver
    {
        private readonly JsonElement contextRoot;

        public SaveContextValueResolver(JsonElement contextRoot)
        {
            this.contextRoot = contextRoot;
        }

        public ResolvedDependencyValue Resolve(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Missing(name, "Blank dependency.");
            }

            if (TryResolveBuildingLevel(name, out var buildingResult))
            {
                return buildingResult;
            }

            if (TryResolveAchievementMap(name, out var achievementResult))
            {
                return achievementResult;
            }

            if (TryResolveResource(name, out var resourceResult))
            {
                return resourceResult;
            }

            if (TryResolveCharacter(name, out var characterResult))
            {
                return characterResult;
            }

            if (TryResolveProgress(name, out var progressResult))
            {
                return progressResult;
            }

            if (TryResolveTrial(name, out var trialResult))
            {
                return trialResult;
            }

            if (TryResolveCatalyst(name, out var catalystResult))
            {
                return catalystResult;
            }

            if (TryResolveSpellAggregate(name, out var spellResult))
            {
                return spellResult;
            }

            return Missing(name, "No mapping in save_calculation_context.json or zz_save_achievement_map.json yet.");
        }

        private bool TryResolveBuildingLevel(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            var match = Regex.Match(name, @"^Building\.(?<tier>\d+)\.Level$", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Groups["tier"].Value, out var tier))
            {
                return false;
            }

            if (!contextRoot.TryGetProperty("Buildings", out var buildings)
                || buildings.ValueKind != JsonValueKind.Array)
            {
                result = Missing(name, "Buildings are not present in calculation context.");
                return true;
            }

            foreach (var building in buildings.EnumerateArray())
            {
                if (GetInt(building, "Tier") == tier)
                {
                    result = new ResolvedDependencyValue(
                        Resolved: true,
                        Value: GetString(building, "Level"),
                        ValueKind: "Integer",
                        Source: "Buildings[].Level by tier",
                        Notes: "Resolved from save BuildingLevels mapped by building tier."
                    );
                    return true;
                }
            }

            result = Missing(name, "Building tier was not found in context Buildings array.");
            return true;
        }

        private bool TryResolveAchievementMap(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            var achievementMapPath = Path.Combine(Directory.GetCurrentDirectory(), "zz_save_achievement_map.json");

            if (!File.Exists(achievementMapPath))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(achievementMapPath));
                var root = doc.RootElement;

                if (name.Equals("Achiev.Count", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("Achieves", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("AchievUnlocked", StringComparison.OrdinalIgnoreCase))
                {
                    if (root.TryGetProperty("DerivedResources", out var derived)
                        && derived.ValueKind == JsonValueKind.Object)
                    {
                        result = new ResolvedDependencyValue(
                            Resolved: true,
                            Value: GetString(derived, "AchievCount"),
                            ValueKind: "Integer",
                            Source: "zz_save_achievement_map.DerivedResources.AchievCount",
                            Notes: "Derived from sum of AchievementsSave unlocked level counts."
                        );
                        return true;
                    }
                }

                if (name.Equals("Achiev.Points", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("AchievPoints", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("AchievsPoints", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryGetAchievementPointLowerBound(root, out var lowerBound))
                    {
                        result = new ResolvedDependencyValue(
                            Resolved: true,
                            Value: lowerBound,
                            ValueKind: "LowerBound",
                            Source: "zz_save_achievement_map.Categories[AchievPoints].UnlockedRows[].Argument",
                            Notes: "Achiev.Points is not directly serialized. This lower bound is inferred from the highest unlocked AchievPoints threshold."
                        );
                        return true;
                    }

                    if (root.TryGetProperty("DerivedResources", out var derived)
                        && derived.ValueKind == JsonValueKind.Object)
                    {
                        result = new ResolvedDependencyValue(
                            Resolved: true,
                            Value: GetString(derived, "AchievPoints"),
                            ValueKind: "Estimate",
                            Source: "zz_save_achievement_map.DerivedResources.AchievPoints",
                            Notes: "Estimated by summing matched achievement catalog points. May undercount secret/triumph points."
                        );
                        return true;
                    }
                }

                if (TryGetAchievementFlag(root, name, out var flagSource))
                {
                    result = new ResolvedDependencyValue(
                        Resolved: true,
                        Value: "true",
                        ValueKind: "Boolean",
                        Source: flagSource,
                        Notes: "Resolved from achievement category/secret/class/triumph flags."
                    );
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                result = Missing(name, "Failed to read zz_save_achievement_map.json: " + ex.Message);
                return true;
            }
        }

        private static bool TryGetAchievementPointLowerBound(JsonElement root, out string lowerBound)
        {
            lowerBound = "";

            if (!root.TryGetProperty("Categories", out var categories)
                || categories.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var category in categories.EnumerateArray())
            {
                if (!string.Equals(GetString(category, "Key"), "AchievPoints", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!category.TryGetProperty("UnlockedRows", out var rows)
                    || rows.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var lastArgument = "";

                foreach (var row in rows.EnumerateArray())
                {
                    var argument = GetString(row, "Argument");

                    if (!string.IsNullOrWhiteSpace(argument))
                    {
                        lastArgument = argument;
                    }
                }

                if (!string.IsNullOrWhiteSpace(lastArgument))
                {
                    lowerBound = lastArgument;
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetAchievementFlag(JsonElement root, string name, out string source)
        {
            source = "";

            if (TryGetBooleanDictionaryFlag(root, "ClassFlags", name, out source))
            {
                return true;
            }

            if (TryGetBooleanDictionaryFlag(root, "SecretFlags", name, out source))
            {
                return true;
            }

            if (TryGetBooleanDictionaryFlag(root, "TriumphFlags", name, out source))
            {
                return true;
            }

            if (root.TryGetProperty("Categories", out var categories)
                && categories.ValueKind == JsonValueKind.Array)
            {
                foreach (var category in categories.EnumerateArray())
                {
                    if (!string.Equals(GetString(category, "Key"), name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (GetInt(category, "UnlockedLevelCount") > 0)
                    {
                        source = "zz_save_achievement_map.Categories[" + name + "]";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryGetBooleanDictionaryFlag(
            JsonElement root,
            string propertyName,
            string flagName,
            out string source)
        {
            source = "";

            if (!root.TryGetProperty(propertyName, out var flags)
                || flags.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (var property in flags.EnumerateObject())
            {
                if (!string.Equals(property.Name, flagName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.True)
                {
                    source = "zz_save_achievement_map." + propertyName + "." + property.Name;
                    return true;
                }

                return false;
            }

            return false;
        }

        private bool TryResolveResource(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            var resourceName = name switch
            {
                "Base.PlayedTimeRealm" => "TimeRealm",
                "Base.PlayedTimeTotal" => "TimeTotal",
                "Base.PlayedTimeSession" => "TimeSession",
                "Base.Souls" => "Souls",

                "VoidMana.Collect" => "ClickableCollect",
                "VoidMana.Realm" => "VoidManaRealm",
                "VoidMana.Session" => "VoidManaSession",
                "VoidMana" => "VoidManaAllTime",

                "Items.EDustRealm" => "EDE",
                "Items.EDustExile" => "EDE",

                "Spell.SpellCastRealm" => "CastSpellRealm",
                "Spell.SpellCastTotal" => "CastSpellTotal",
                "Spell.SpellCast" => "CastSpell",
                "SpellCasts" => "CastSpell",

                "Spell.ShardsTotal" => "ShardsTotal",
                "Spell.ShardsRealm" => "ShardsRealm",
                "Shards" => "ShardsPool",

                "Click.AutoTotalAllTime" => "AutoClicksTotal",
                "Click.AutoTotalRealm" => "AutoClicksRealm",
                "Click.AutoTotalSession" => "AutoClicks",
                "Click.AutoTotal" => "AutoClicksTotal",

                "TotalBuildings" => "TotalBuildings",
                "Base.TotalBuildings" => "TotalBuildings",
                "Base.BoughtUpgrades" => "BoughtUpgrades",

                _ => ""
            };

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return false;
            }

            if (TryFindResource(resourceName, out result))
            {
                return true;
            }

            result = Missing(name, "Mapped to save/resource name '" + resourceName + "', but that value is not present in calculation context yet.");
            return true;
        }

        private bool TryResolveCharacter(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            if (!contextRoot.TryGetProperty("Character", out var character)
                || character.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            switch (name)
            {
                case "Hero.Level":
                case "HeroLevel":
                    result = Missing(name, "Current run character level is intentionally not mapped yet.");
                    return true;

                case "Hero.MaxLevelAllTime":
                case "HeroMaxLevelAllTime":
                    result = new ResolvedDependencyValue(true, GetString(character, "HeroMaxLevelAllTime"), "Integer", "Character.HeroMaxLevelAllTime", "All-time max hero level, not current run level.");
                    return true;

                case "Pet.MaxLevelAllTime":
                case "PetMaxLevelAllTime":
                    result = new ResolvedDependencyValue(true, GetString(character, "PetMaxLevelAllTime"), "Integer", "Character.PetMaxLevelAllTime", "All-time max pet level.");
                    return true;

                case "Pet.MaxLevel":
                case "PetMaxLevel":
                    result = new ResolvedDependencyValue(true, GetString(character, "MaxPetLevel"), "Integer", "Character.MaxPetLevel", "Pet max level statistic from save.");
                    return true;
            }

            return false;
        }

        private bool TryResolveProgress(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            if (!contextRoot.TryGetProperty("Progress", out var progress)
                || progress.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            switch (name)
            {
                case "Ascends":
                    result = new ResolvedDependencyValue(true, GetString(progress, "Ascends"), "Integer", "Progress.Ascends", "Resolved from save.");
                    return true;

                case "AscendsRealm":
                case "Realm.Times":
                    result = new ResolvedDependencyValue(true, GetString(progress, "AscendsRealm"), "Integer", "Progress.AscendsRealm", "Realm.Times is tentatively mapped to AscendsRealm until source-confirmed.");
                    return true;

                case "Base.ManaAllTimeLog":
                case "TotalManaLog":
                    result = new ResolvedDependencyValue(true, GetString(progress, "TotalManaLog"), "Integer", "Progress.TotalManaLog", "Log10 exponent of ManaAllTime.");
                    return true;
            }

            return false;
        }

        private bool TryResolveTrial(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            if (!contextRoot.TryGetProperty("Trial", out var trial)
                || trial.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            switch (name)
            {
                case "Trial.Completed":
                    result = new ResolvedDependencyValue(
                        Resolved: true,
                        Value: GetString(trial, "Completed"),
                        ValueKind: "Integer",
                        Source: "Trial.Completed",
                        Notes: "Source-confirmed: Trial.Completed maps to TrialManager.Completed and save Trial.completed."
                    );
                    return true;

                case "Trial.TotalCompleted":
                    result = new ResolvedDependencyValue(
                        Resolved: true,
                        Value: GetString(trial, "TotalCompleted"),
                        ValueKind: "Integer",
                        Source: "Trial.TotalCompleted",
                        Notes: "Source-confirmed: Trial.TotalCompleted maps to TrialManager.TotalCompleted and save Trial.totalCompleted."
                    );
                    return true;

                case "Trial.Tries":
                    result = new ResolvedDependencyValue(true, GetString(trial, "Tries"), "Integer", "Trial.Tries", "Resolved from save Trial.tries.");
                    return true;

                case "Trial.Keys":
                    result = new ResolvedDependencyValue(true, GetString(trial, "Keys"), "Integer", "Trial.Keys", "Resolved from save Trial.keys.");
                    return true;
            }

            return false;
        }

        private bool TryResolveCatalyst(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            if (!contextRoot.TryGetProperty("Catalysts", out var catalysts)
                || catalysts.ValueKind != JsonValueKind.Object
                || !catalysts.TryGetProperty("Totals", out var totals)
                || totals.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            switch (name)
            {
                case "Base.CatalystsTotal":
                    result = new ResolvedDependencyValue(true, GetString(totals, "TotalCatalysts"), "BigNumber", "Catalysts.Totals.TotalCatalysts", "Resolved from catalyst save totals.");
                    return true;

                case "Catalysts.GreenTotal":
                    result = new ResolvedDependencyValue(true, GetString(totals, "TotalGreenCatalysts"), "BigNumber", "Catalysts.Totals.TotalGreenCatalysts", "Resolved from catalyst save totals.");
                    return true;

                case "Catalysts.BlueTotal":
                    result = new ResolvedDependencyValue(true, GetString(totals, "TotalBlueCatalysts"), "BigNumber", "Catalysts.Totals.TotalBlueCatalysts", "Resolved from catalyst save totals.");
                    return true;

                case "Catalysts.RedTotal":
                    result = new ResolvedDependencyValue(true, GetString(totals, "TotalRedCatalysts"), "BigNumber", "Catalysts.Totals.TotalRedCatalysts", "Resolved from catalyst save totals.");
                    return true;
            }

            return false;
        }

        private bool TryResolveSpellAggregate(string name, out ResolvedDependencyValue result)
        {
            result = Missing(name, "");

            if (!contextRoot.TryGetProperty("Spells", out var spells)
                || spells.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            switch (name)
            {
                case "Spell.AccumutaledCasts":
                case "Spell.AccumulatedCasts":
                    result = new ResolvedDependencyValue(true, GetString(spells, "AccumCasts"), "BigNumber", "Spells.AccumCasts", "Resolved from SaveData.AccumCasts.");
                    return true;
            }

            return false;
        }

        private bool TryFindResource(string resourceName, out ResolvedDependencyValue result)
        {
            result = Missing(resourceName, "");

            if (!contextRoot.TryGetProperty("Resources", out var resources)
                || resources.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var resource in resources.EnumerateArray())
            {
                if (!string.Equals(GetString(resource, "Name"), resourceName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result = new ResolvedDependencyValue(
                    Resolved: true,
                    Value: GetString(resource, "Scientific"),
                    ValueKind: GetString(resource, "ValueKind"),
                    Source: "Resources." + resourceName,
                    Notes: "Resolved from save calculation context resources."
                );
                return true;
            }

            return false;
        }

        private static ResolvedDependencyValue Missing(string name, string notes)
        {
            return new ResolvedDependencyValue(false, "", "", "", notes);
        }
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

    private sealed record UpgradeFormulaDependencyExport(
        string GeneratedAtUtc,
        string AggregateFile,
        string CalculationContextFile,
        DependencyMapSummary Summary,
        IReadOnlyList<DependencySummaryEntry> FormulaDependencySummary,
        IReadOnlyList<DependencySummaryEntry> ConditionDependencySummary,
        IReadOnlyList<FormulaDependencyEntry> FormulaDependencies,
        IReadOnlyList<ConditionDependencyEntry> ConditionDependencies,
        IReadOnlyList<string> Notes
    );

    private sealed record DependencyMapSummary(
        int FormulaDependencyCount,
        int FormulaResolvedCount,
        int FormulaUnresolvedCount,
        int UniqueFormulaDependencyCount,
        int ConditionDependencyCount,
        int ConditionResolvedCount,
        int ConditionUnresolvedCount,
        int UniqueConditionDependencyCount
    );

    private sealed record DependencySummaryEntry(
        string Name,
        int UsageCount,
        int ResolvedCount,
        int UnresolvedCount,
        IReadOnlyList<int> ExampleUpgradeIds,
        IReadOnlyList<string> ExampleUpgradeNames,
        string ResolutionSource,
        string Notes
    );

    private sealed record FormulaDependencyEntry(
        int UpgradeId,
        string UpgradeName,
        string Target,
        string Effect,
        string Addendum,
        string Multiplier,
        string Secondary,
        bool Resolved,
        string ResolvedValue,
        string ValueKind,
        string ResolutionSource,
        string Notes,
        string FormulaSummary
    );

    private sealed record ConditionDependencyEntry(
        int UpgradeId,
        string UpgradeName,
        string Target,
        string ConditionParameter,
        string ConditionArgument,
        bool Resolved,
        string ResolvedValue,
        string ValueKind,
        string ResolutionSource,
        string Notes
    );

    private sealed record ResolvedDependencyValue(
        bool Resolved,
        string Value,
        string ValueKind,
        string Source,
        string Notes
    );
}

