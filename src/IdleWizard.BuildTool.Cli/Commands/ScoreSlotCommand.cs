using System.Text.Json;
using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ScoreSlotCommand
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
        var objective = args[3];

        var userValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? efficiency = null;
        string? gilding = null;
        string tierMode = "highest";
        int? maxResults = 25;
        string? equippedSpec = null;

        foreach (var rawArg in args.Skip(4))
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

        var items = LoadItems(workspacePath)
            .Where(item => item.Slot.Equals(slot, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (items.Count == 0)
        {
            Console.WriteLine($"No items found for slot: {slot}");
            Console.WriteLine("");
            Console.WriteLine("Try inspecting slots with:");
            Console.WriteLine(@"  --records .\iw_workspace_vNext Items 20");
            return;
        }

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();
        var allItemEffects = extractor.Extract(workspacePath, "Items");

        var results = new List<ScoredSlotItemResult>();

        foreach (var item in items)
        {
            var selectedTier = SelectTier(item, tierMode);

            if (selectedTier is null)
            {
                continue;
            }

            results.Add(
                EvaluateOneItem(
                    allItemEffects,
                    converter,
                    item,
                    selectedTier,
                    objective,
                    userValues,
                    efficiency,
                    gilding
                )
            );
        }

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Slot: {slot}");
        Console.WriteLine($"Objective: {objective}");
        Console.WriteLine($"Tier mode: {tierMode}");
        Console.WriteLine($"Items found in slot: {items.Count}");
        Console.WriteLine($"Items scored: {results.Count}");
        Console.WriteLine($"User-provided stat values: {userValues.Count}");
        Console.WriteLine($"Efficiency: {efficiency ?? "not provided"}");
        Console.WriteLine($"Gilding: {gilding ?? "not provided"}");
        Console.WriteLine($"Equipped: {equippedSpec ?? "not provided"}");
        Console.WriteLine("");

        Console.WriteLine("Ranked results:");
        Console.WriteLine("");

        var ranked = results
            .OrderByDescending(x => x.Log10Ratio)
            .ThenBy(x => x.Name)
            .ToList();

        ScoredSlotItemResult? equippedResult = null;

        if (!string.IsNullOrWhiteSpace(equippedSpec) && equippedSpec.Contains(":"))
        {
            var split = equippedSpec.Split(":", 2);
            var equippedId = split[0];
            var equippedTier = split[1];

            equippedResult = results.FirstOrDefault(
                result =>
                    result.ItemId.Equals(equippedId, StringComparison.OrdinalIgnoreCase)
                    && result.Tier.Equals(equippedTier, StringComparison.OrdinalIgnoreCase)
            );

            if (equippedResult is null)
            {
                Console.WriteLine($"Warning: equipped item not found in scored slot results: {equippedSpec}");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine($"Equipped baseline: Item {equippedResult.ItemId}, Tier {equippedResult.Tier} - {equippedResult.Name}");
                Console.WriteLine($"  Objective after equipped: {equippedResult.ObjectiveAfter}");
                Console.WriteLine($"  Equipped ratio:           {equippedResult.Ratio}");
                Console.WriteLine("");
            }
        }

        if (maxResults is not null)
        {
            ranked = ranked.Take(maxResults.Value).ToList();
        }

        var rank = 1;

        foreach (var result in ranked)
        {
            Console.WriteLine($"#{rank}: Item {result.ItemId}, Tier {result.Tier} - {result.Name}");
            Console.WriteLine($"  Slot:             {result.Slot}");
            Console.WriteLine($"  Objective before: {result.ObjectiveBefore}");
            Console.WriteLine($"  Objective after:  {result.ObjectiveAfter}");
            Console.WriteLine($"  Ratio:            {result.Ratio}");
            Console.WriteLine($"  log10 ratio:      {result.Log10Ratio:0.############}");

            if (equippedResult is not null)
            {
                var vsEquippedRatio = result.Ratio / equippedResult.Ratio;
                var vsEquippedLog10 = result.Log10Ratio - equippedResult.Log10Ratio;

                Console.WriteLine($"  vs equipped ratio: {vsEquippedRatio}");
                Console.WriteLine($"  vs equipped log10: {vsEquippedLog10:0.############}");
            }
            Console.WriteLine($"  Raw effects:      {result.RawEffectCount}");
            Console.WriteLine($"  Applied effects:  {result.AppliedEffectCount}");
            Console.WriteLine($"  Fully verified:   {result.FullyVerified}");

            if (!result.DirectlyTouchedObjective)
            {
                Console.WriteLine("  Note: item did not directly modify the objective resource.");
            }

            Console.WriteLine("  Changed resources:");

            foreach (var change in result.Changes.OrderBy(x => x.Key))
            {
                Console.WriteLine($"    {change.Key}: {change.Value.Before} -> {change.Value.After}");
            }

            Console.WriteLine("");

            rank++;
        }

        Console.WriteLine("Note:");
        Console.WriteLine("  This is direct-resource scoring only.");
        Console.WriteLine("  Derived formulas are not included yet.");
        Console.WriteLine("  Slot scoring currently chooses the highest item tier by default.");
        Console.WriteLine("  Use --tier=NUMBER to force a specific tier.");
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

    private static ScoredSlotItemResult EvaluateOneItem(
        IReadOnlyList<RawEffectDescriptor> allItemEffects,
        RawEffectConverter converter,
        ItemSummary item,
        string tier,
        string objective,
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

        RegisterResource(context, objective, userValues);

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

        var objectiveBefore = beforeValues[objective];
        var objectiveAfter = afterValues[objective];

        var ratio = objectiveAfter / objectiveBefore;
        var log10Ratio = ratio.Log10();

        var changes = new Dictionary<string, ResourceChange>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in afterValues.Keys.OrderBy(x => x))
        {
            changes[key] = new ResourceChange(
                beforeValues[key],
                afterValues[key]
            );
        }

        var directlyTouchedObjective = itemEffects.Any(
            effect => effect.Target.Equals(objective, StringComparison.OrdinalIgnoreCase)
        );

        return new ScoredSlotItemResult(
            item.Id,
            tier,
            item.Name,
            item.Slot,
            itemEffects.Count,
            report.Effects.Count,
            unresolvedCount == 0 && report.IsFullyVerified,
            objectiveBefore,
            objectiveAfter,
            ratio,
            log10Ratio,
            directlyTouchedObjective,
            changes
        );
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
        Console.WriteLine(@"  --score-slot .\iw_workspace_vNext Head Hero.AbilityPower ""Hero.AbilityPower=100"" ""--efficiency=2"" ""--gilding=1.5""");
        Console.WriteLine("");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  workspace");
        Console.WriteLine("  slot name, e.g. Head, Ring, Body, Weapon");
        Console.WriteLine("  objective resource key");
        Console.WriteLine("  optional Resource.Key=value current stat values");
        Console.WriteLine("  optional --efficiency=value");
        Console.WriteLine("  optional --gilding=value");
        Console.WriteLine("  optional --tier=highest or --tier=NUMBER");
        Console.WriteLine("  optional --top=NUMBER");
        Console.WriteLine("  optional --equipped=ITEMID:TIER");
    }

    private sealed record ItemSummary(
        string Id,
        string Name,
        string Slot,
        IReadOnlyList<string> Tiers
    );

    private sealed record ScoredSlotItemResult(
        string ItemId,
        string Tier,
        string Name,
        string Slot,
        int RawEffectCount,
        int AppliedEffectCount,
        bool FullyVerified,
        CalcBigNumber ObjectiveBefore,
        CalcBigNumber ObjectiveAfter,
        CalcBigNumber Ratio,
        double Log10Ratio,
        bool DirectlyTouchedObjective,
        IReadOnlyDictionary<string, ResourceChange> Changes
    );

    private sealed record ResourceChange(
        CalcBigNumber Before,
        CalcBigNumber After
    );
}

