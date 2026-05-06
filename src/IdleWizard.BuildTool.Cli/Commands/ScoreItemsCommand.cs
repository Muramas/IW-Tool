using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ScoreItemsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 5)
        {
            PrintUsage();
            return;
        }

        var workspacePath = args[1];
        var objective = args[2];

        var itemSpecs = new List<(string ItemId, string Tier)>();
        var userValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? efficiency = null;
        string? gilding = null;

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

            if (rawArg.Contains("="))
            {
                var split = rawArg.Split("=", 2);

                if (split.Length == 2)
                {
                    userValues[split[0]] = split[1];
                    continue;
                }
            }

            if (rawArg.Contains(":"))
            {
                var split = rawArg.Split(":", 2);

                if (split.Length == 2)
                {
                    itemSpecs.Add((split[0], split[1]));
                    continue;
                }
            }

            Console.WriteLine($"Ignoring unrecognized argument: {rawArg}");
        }

        if (itemSpecs.Count == 0)
        {
            Console.WriteLine("No item specs provided.");
            PrintUsage();
            return;
        }

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();
        var allItemEffects = extractor.Extract(workspacePath, "Items");

        var results = new List<ScoredItemResult>();

        foreach (var spec in itemSpecs)
        {
            results.Add(
                EvaluateOneItem(
                    allItemEffects,
                    converter,
                    spec.ItemId,
                    spec.Tier,
                    objective,
                    userValues,
                    efficiency,
                    gilding
                )
            );
        }

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Objective: {objective}");
        Console.WriteLine($"Items scored: {results.Count}");
        Console.WriteLine($"User-provided stat values: {userValues.Count}");
        Console.WriteLine($"Efficiency: {efficiency ?? "not provided"}");
        Console.WriteLine($"Gilding: {gilding ?? "not provided"}");
        Console.WriteLine("");

        Console.WriteLine("Ranked results:");
        Console.WriteLine("");

        var rank = 1;

        foreach (var result in results.OrderByDescending(x => x.Log10Ratio))
        {
            Console.WriteLine($"#{rank}: Item {result.ItemId}, Tier {result.Tier} - {result.Name}");
            Console.WriteLine($"  Objective before: {result.ObjectiveBefore}");
            Console.WriteLine($"  Objective after:  {result.ObjectiveAfter}");
            Console.WriteLine($"  Ratio:            {result.Ratio}");
            Console.WriteLine($"  log10 ratio:      {result.Log10Ratio:0.############}");
            Console.WriteLine($"  Raw effects:      {result.RawEffectCount}");
            Console.WriteLine($"  Applied effects:  {result.AppliedEffectCount}");
            Console.WriteLine($"  Fully verified:   {result.FullyVerified}");

            if (!result.DirectlyTouchedObjective)
            {
                Console.WriteLine("  Note: item did not directly modify the objective resource.");
            }

            Console.WriteLine("");

            rank++;
        }

        Console.WriteLine("Changed resources by item:");
        Console.WriteLine("");

        foreach (var result in results.OrderByDescending(x => x.Log10Ratio))
        {
            Console.WriteLine($"Item {result.ItemId}, Tier {result.Tier} - {result.Name}");

            foreach (var change in result.Changes.OrderBy(x => x.Key))
            {
                Console.WriteLine($"  {change.Key}: {change.Value.Before} -> {change.Value.After}");
            }

            Console.WriteLine("");
        }

        Console.WriteLine("Note:");
        Console.WriteLine("  This is direct-resource scoring only.");
        Console.WriteLine("  Derived formulas are not included yet.");
        Console.WriteLine("  Example unresolved derived chain: Char.Intelligence may affect Hero.AbilityPower in-game, but this command only scores direct changes to the selected objective.");
    }

    private static ScoredItemResult EvaluateOneItem(
        IReadOnlyList<RawEffectDescriptor> allItemEffects,
        RawEffectConverter converter,
        string itemId,
        string tier,
        string objective,
        Dictionary<string, string> userValues,
        string? efficiency,
        string? gilding)
    {
        var itemEffects = allItemEffects
            .Where(
                effect =>
                    effect.SourceId.Equals(itemId, StringComparison.OrdinalIgnoreCase)
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

        var name = itemEffects.Count > 0
            ? itemEffects[0].SourceName
            : "(not found)";

        var directlyTouchedObjective = itemEffects.Any(
            effect => effect.Target.Equals(objective, StringComparison.OrdinalIgnoreCase)
        );

        return new ScoredItemResult(
            itemId,
            tier,
            name,
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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(@"  --score-items .\iw_workspace_vNext Hero.AbilityPower 1:4 0:5 ""Hero.AbilityPower=100"" ""Base.SoulPower=10"" ""Char.Intelligence=150"" ""Experiment.Efficiency=20"" ""--efficiency=2"" ""--gilding=1.5""");
        Console.WriteLine("");
        Console.WriteLine("Arguments:");
        Console.WriteLine("  workspace");
        Console.WriteLine("  objective resource key");
        Console.WriteLine("  one or more itemId:tier specs");
        Console.WriteLine("  optional Resource.Key=value current stat values");
        Console.WriteLine("  optional --efficiency=value");
        Console.WriteLine("  optional --gilding=value");
    }

    private sealed record ScoredItemResult(
        string ItemId,
        string Tier,
        string Name,
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
