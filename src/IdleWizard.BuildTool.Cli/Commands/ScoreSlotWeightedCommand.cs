using System.Text.Json;
using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ScoreSlotWeightedCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            PrintUsage();
            return;
        }

        var workspacePath = args[1];
        var slot = args[2];

        var objectives = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var userValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? efficiency = null;
        string? gilding = null;
        string tierMode = "highest";
        int? maxResults = 25;
        string? equippedSpec = null;
        string? jsonOut = null;

        foreach (var rawArg in args.Skip(3))
        {
            if (rawArg.StartsWith("--efficiency=", StringComparison.OrdinalIgnoreCase))
            {
                efficiency = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.StartsWith("--gilding=", StringComparison.OrdinalIgnoreCase))
            {
                gilding = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.StartsWith("--tier=", StringComparison.OrdinalIgnoreCase))
            {
                tierMode = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.StartsWith("--top=", StringComparison.OrdinalIgnoreCase))
            {
                var rawTop = rawArg.Split("=", 2)[1];

                if (int.TryParse(rawTop, out var parsedTop))
                {
                    maxResults = parsedTop;
                }

                continue;
            }

            if (rawArg.StartsWith("--equipped=", StringComparison.OrdinalIgnoreCase))
            {
                equippedSpec = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.StartsWith("--json-out=", StringComparison.OrdinalIgnoreCase))
            {
                jsonOut = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.Contains("@"))
            {
                var split = rawArg.Split("@", 2);

                if (split.Length == 2 && double.TryParse(split[1], out var weight))
                {
                    objectives[split[0]] = weight;
                    continue;
                }

                Console.WriteLine($"Ignoring invalid objective argument: {rawArg}");
                Console.WriteLine("Expected format: Resource.Key@weight");
                continue;
            }

            if (rawArg.Contains("="))
            {
                var split = rawArg.Split("=", 2);

                if (split.Length == 2)
                {
                    userValues[split[0]] = split[1];
                    continue;
                }
            }

            Console.WriteLine($"Ignoring unrecognized argument: {rawArg}");
        }

        if (objectives.Count == 0)
        {
            Console.WriteLine("No weighted objectives provided.");
            PrintUsage();
            return;
        }

        var items = LoadItems(workspacePath)
            .Where(item => item.Slot.Equals(slot, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (items.Count == 0)
        {
            Console.WriteLine($"No items found for slot: {slot}");
            return;
        }

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();
        var allItemEffects = extractor.Extract(workspacePath, "Items");

        var allResults = new List<WeightedSlotItemResult>();

        foreach (var item in items)
        {
            var selectedTier = SelectTier(item, tierMode);

            if (selectedTier is null)
            {
                continue;
            }

            allResults.Add(
                EvaluateOneItem(
                    allItemEffects,
                    converter,
                    item,
                    selectedTier,
                    objectives,
                    userValues,
                    efficiency,
                    gilding
                )
            );
        }

        var rankedAll = allResults
            .OrderByDescending(x => x.WeightedScore)
            .ThenBy(x => x.Name)
            .ToList();

        WeightedSlotItemResult? equippedResult = null;

        if (!string.IsNullOrWhiteSpace(equippedSpec) && equippedSpec.Contains(":"))
        {
            var split = equippedSpec.Split(":", 2);
            var equippedId = split[0];
            var equippedTier = split[1];

            equippedResult = allResults.FirstOrDefault(
                result =>
                    result.ItemId.Equals(equippedId, StringComparison.OrdinalIgnoreCase)
                    && result.Tier.Equals(equippedTier, StringComparison.OrdinalIgnoreCase)
            );
        }

        var displayResults = rankedAll;

        if (maxResults is not null)
        {
            displayResults = displayResults.Take(maxResults.Value).ToList();
        }

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Slot: {slot}");
        Console.WriteLine($"Tier mode: {tierMode}");
        Console.WriteLine($"Items found in slot: {items.Count}");
        Console.WriteLine($"Items scored: {allResults.Count}");
        Console.WriteLine($"User-provided stat values: {userValues.Count}");
        Console.WriteLine($"Efficiency: {efficiency ?? "not provided"}");
        Console.WriteLine($"Gilding: {gilding ?? "not provided"}");
        Console.WriteLine($"Equipped: {equippedSpec ?? "not provided"}");
        Console.WriteLine($"JSON out: {jsonOut ?? "not provided"}");
        Console.WriteLine("");

        Console.WriteLine("Weighted objectives:");

        foreach (var objective in objectives.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {objective.Key}: weight {objective.Value}");
        }

        Console.WriteLine("");

        if (!string.IsNullOrWhiteSpace(equippedSpec))
        {
            if (equippedResult is null)
            {
                Console.WriteLine($"Warning: equipped item not found in scored slot results: {equippedSpec}");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine($"Equipped baseline: Item {equippedResult.ItemId}, Tier {equippedResult.Tier} - {equippedResult.Name}");
                Console.WriteLine($"  Weighted score: {equippedResult.WeightedScore:0.############}");
                Console.WriteLine("");
            }
        }

        Console.WriteLine("Ranked weighted results:");
        Console.WriteLine("");

        var rank = 1;

        foreach (var result in displayResults)
        {
            Console.WriteLine($"#{rank}: Item {result.ItemId}, Tier {result.Tier} - {result.Name}");
            Console.WriteLine($"  Slot:             {result.Slot}");
            Console.WriteLine($"  Weighted score:   {result.WeightedScore:0.############}");

            if (equippedResult is not null)
            {
                var delta = result.WeightedScore - equippedResult.WeightedScore;
                Console.WriteLine($"  vs equipped score delta: {delta:0.############}");
            }

            Console.WriteLine($"  Raw effects:      {result.RawEffectCount}");
            Console.WriteLine($"  Applied effects:  {result.AppliedEffectCount}");
            Console.WriteLine($"  Fully verified:   {result.FullyVerified}");
            Console.WriteLine("");

            Console.WriteLine("  Objective contributions:");

            foreach (var objective in result.ObjectiveResults.OrderBy(x => x.Key))
            {
                Console.WriteLine($"    {objective.Key}");
                Console.WriteLine($"      before:       {objective.Value.Before}");
                Console.WriteLine($"      after:        {objective.Value.After}");
                Console.WriteLine($"      ratio:        {objective.Value.Ratio}");
                Console.WriteLine($"      log10 ratio:  {objective.Value.Log10Ratio:0.############}");
                Console.WriteLine($"      weight:       {objective.Value.Weight}");
                Console.WriteLine($"      contribution: {objective.Value.Contribution:0.############}");

                if (!objective.Value.DirectlyTouched)
                {
                    Console.WriteLine("      note: item did not directly modify this objective.");
                }
            }

            Console.WriteLine("");
            Console.WriteLine("  Changed resources:");

            foreach (var change in result.Changes.OrderBy(x => x.Key))
            {
                Console.WriteLine($"    {change.Key}: {change.Value.Before} -> {change.Value.After}");
            }

            Console.WriteLine("");

            rank++;
        }

        Console.WriteLine("Note:");
        Console.WriteLine("  Weighted scoring is direct-resource only.");
        Console.WriteLine("  Derived formulas are not included yet.");
        Console.WriteLine("  Score formula: sum(log10(after / before) * weight).");

        if (!string.IsNullOrWhiteSpace(jsonOut))
        {
            WriteJsonResult(
                jsonOut,
                workspacePath,
                slot,
                tierMode,
                objectives,
                userValues,
                efficiency,
                gilding,
                equippedSpec,
                equippedResult,
                rankedAll
            );

            Console.WriteLine("");
            Console.WriteLine($"Wrote ranked result JSON: {jsonOut}");
        }
    }

    private static void WriteJsonResult(
        string path,
        string workspacePath,
        string slot,
        string tierMode,
        Dictionary<string, double> objectives,
        Dictionary<string, string> userValues,
        string? efficiency,
        string? gilding,
        string? equippedSpec,
        WeightedSlotItemResult? equippedResult,
        IReadOnlyList<WeightedSlotItemResult> rankedResults)
    {
        var export = new
        {
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            workspace = workspacePath,
            slot,
            tierMode,
            efficiency,
            gilding,
            equipped = equippedSpec,
            objectives,
            currentValues = userValues,
            equippedBaseline = equippedResult is null
                ? null
                : ToJsonResult(equippedResult, null, null),
            results = rankedResults
                .Select(
                    (result, index) =>
                    {
                        double? vsEquippedScoreDelta = equippedResult is null
                            ? null
                            : result.WeightedScore - equippedResult.WeightedScore;

                        return ToJsonResult(result, index + 1, vsEquippedScoreDelta);
                    }
                )
                .ToList(),
            warnings = new[]
            {
                "Weighted scoring is direct-resource only.",
                "Derived formulas are not included yet.",
                "Score formula: sum(log10(after / before) * weight)."
            }
        };

        var json = JsonSerializer.Serialize(
            export,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(path, json);
    }

    private static object ToJsonResult(
        WeightedSlotItemResult result,
        int? rank,
        double? vsEquippedScoreDelta)
    {
        return new
        {
            rank,
            itemId = result.ItemId,
            tier = result.Tier,
            name = result.Name,
            slot = result.Slot,
            weightedScore = result.WeightedScore,
            vsEquippedScoreDelta,
            rawEffectCount = result.RawEffectCount,
            appliedEffectCount = result.AppliedEffectCount,
            fullyVerified = result.FullyVerified,
            objectiveContributions = result.ObjectiveResults.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    before = pair.Value.Before.ToString(),
                    after = pair.Value.After.ToString(),
                    ratio = pair.Value.Ratio.ToString(),
                    log10Ratio = pair.Value.Log10Ratio,
                    weight = pair.Value.Weight,
                    contribution = pair.Value.Contribution,
                    directlyTouched = pair.Value.DirectlyTouched
                },
                StringComparer.OrdinalIgnoreCase
            ),
            changedResources = result.Changes.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    before = pair.Value.Before.ToString(),
                    after = pair.Value.After.ToString()
                },
                StringComparer.OrdinalIgnoreCase
            )
        };
    }

    private static WeightedSlotItemResult EvaluateOneItem(
        IReadOnlyList<RawEffectDescriptor> allItemEffects,
        RawEffectConverter converter,
        ItemSummary item,
        string tier,
        Dictionary<string, double> objectives,
        Dictionary<string, string> userValues,
        string? efficiency,
        string? gilding)
    {
        var itemEffects = allItemEffects
            .Where(effect =>
                effect.SourceId.Equals(item.Id, StringComparison.OrdinalIgnoreCase)
                && effect.Notes.Equals($"Tier={tier}", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var context = new CalculatorContext();
        var convertedEffects = new List<ICalcEffect>();
        var unresolvedCount = 0;

        foreach (var objective in objectives.Keys)
        {
            RegisterResource(context, objective, userValues);
        }

        foreach (var rawEffect in itemEffects)
        {
            var conversion = converter.Convert(rawEffect);

            if (conversion.Status != RawEffectConversionStatus.Convertible)
            {
                unresolvedCount++;
                continue;
            }

            RegisterResource(context, rawEffect.Target, userValues);

            convertedEffects.Add(
                converter.ToLinearEffect(
                    rawEffect,
                    efficiency,
                    gilding
                )
            );
        }

        var beforeValues = context.Resources.All
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Value,
                StringComparer.OrdinalIgnoreCase
            );

        var report = new BuildEvaluator().Evaluate(context, convertedEffects);

        var afterValues = context.Resources.All
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Value,
                StringComparer.OrdinalIgnoreCase
            );

        var objectiveResults = new Dictionary<string, WeightedObjectiveResult>(
            StringComparer.OrdinalIgnoreCase
        );

        var weightedScore = 0.0;

        foreach (var objective in objectives)
        {
            var key = objective.Key;
            var weight = objective.Value;

            var before = beforeValues[key];
            var after = afterValues[key];
            var ratio = after / before;
            var log10Ratio = ratio.Log10();
            var contribution = log10Ratio * weight;

            weightedScore += contribution;

            var directlyTouched = itemEffects.Any(
                effect => effect.Target.Equals(key, StringComparison.OrdinalIgnoreCase)
            );

            objectiveResults[key] = new WeightedObjectiveResult(
                before,
                after,
                ratio,
                log10Ratio,
                weight,
                contribution,
                directlyTouched
            );
        }

        var changes = new Dictionary<string, ResourceChange>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in afterValues.Keys.OrderBy(x => x))
        {
            changes[key] = new ResourceChange(
                beforeValues[key],
                afterValues[key]
            );
        }

        return new WeightedSlotItemResult(
            item.Id,
            tier,
            item.Name,
            item.Slot,
            itemEffects.Count,
            report.Effects.Count,
            unresolvedCount == 0 && report.IsFullyVerified,
            weightedScore,
            objectiveResults,
            changes
        );
    }

    private static IReadOnlyList<ItemSummary> LoadItems(string workspacePath)
    {
        var rawRoot = Path.Combine(workspacePath, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            throw new DirectoryNotFoundException($"Missing raw_files directory: {rawRoot}");
        }

        var itemsFile = Directory
            .EnumerateFiles(rawRoot, "*", SearchOption.AllDirectories)
            .Where(path =>
                Path.GetFileName(path).Equals("Items.bytes", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).Equals("Items.bytes.txt", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).Equals("Items.json", StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(path => path)
            .FirstOrDefault();

        if (itemsFile is null)
        {
            return Array.Empty<ItemSummary>();
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(itemsFile));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ItemSummary>();
        }

        var items = new List<ItemSummary>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var id = Get(item, "ID");
            var name = Get(item, "Name");
            var slot = Get(item, "Slot");

            var tiers = new List<string>();

            if (item.TryGetProperty("Tiers", out var tiersElement)
                && tiersElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var tier in tiersElement.EnumerateArray())
                {
                    var tierValue = Get(tier, "Tier");

                    if (!string.IsNullOrWhiteSpace(tierValue))
                    {
                        tiers.Add(tierValue);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(id)
                && !string.IsNullOrWhiteSpace(slot))
            {
                items.Add(
                    new ItemSummary(
                        id,
                        name,
                        slot,
                        tiers
                    )
                );
            }
        }

        return items;
    }

    private static string? SelectTier(ItemSummary item, string tierMode)
    {
        if (!tierMode.Equals("highest", StringComparison.OrdinalIgnoreCase))
        {
            return tierMode;
        }

        var numericTiers = item.Tiers
            .Select(tier => int.TryParse(tier, out var parsed) ? parsed : (int?)null)
            .Where(tier => tier is not null)
            .Select(tier => tier!.Value)
            .ToList();

        if (numericTiers.Count == 0)
        {
            return item.Tiers.FirstOrDefault();
        }

        return numericTiers.Max().ToString();
    }

    private static void RegisterResource(
        CalculatorContext context,
        string key,
        Dictionary<string, string> userValues)
    {
        if (context.Resources.TryGet(key, out _))
        {
            return;
        }

        var initial = CalcBigNumber.One;

        if (userValues.TryGetValue(key, out var provided))
        {
            try
            {
                initial = CalcBigNumber.Parse(provided);
            }
            catch
            {
                initial = CalcBigNumber.One;
            }
        }

        context.Resources.Register(
            new CalcVariableComplex(
                key,
                initial
            )
        );
    }

    private static string Get(JsonElement element, string propertyName, string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => fallback,
            _ => value.GetRawText(),
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(@"  --score-slot-weighted .\iw_workspace_vNext Head ""Hero.AbilityPower@1.0"" ""Experiment.Efficiency@0.2"" ""Hero.ExpBoost@0.1"" ""Hero.AbilityPower=100"" ""Experiment.Efficiency=20"" ""Hero.ExpBoost=1"" ""--efficiency=2"" ""--gilding=1.5"" ""--equipped=0:5"" ""--json-out=.\results_head_hero.json""");
    }

    private sealed record ItemSummary(
        string Id,
        string Name,
        string Slot,
        IReadOnlyList<string> Tiers
    );

    private sealed record WeightedSlotItemResult(
        string ItemId,
        string Tier,
        string Name,
        string Slot,
        int RawEffectCount,
        int AppliedEffectCount,
        bool FullyVerified,
        double WeightedScore,
        IReadOnlyDictionary<string, WeightedObjectiveResult> ObjectiveResults,
        IReadOnlyDictionary<string, ResourceChange> Changes
    );

    private sealed record WeightedObjectiveResult(
        CalcBigNumber Before,
        CalcBigNumber After,
        CalcBigNumber Ratio,
        double Log10Ratio,
        double Weight,
        double Contribution,
        bool DirectlyTouched
    );

    private sealed record ResourceChange(
        CalcBigNumber Before,
        CalcBigNumber After
    );
}
