using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class CompareItemsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            PrintUsage();
            return;
        }

        var workspacePath = args[1];

        var itemSpecs = new List<(string ItemId, string Tier)>();
        var userValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? efficiency = null;
        string? gilding = null;

        foreach (var rawArg in args.Skip(2))
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

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Items compared: {itemSpecs.Count}");
        Console.WriteLine($"User-provided stat values: {userValues.Count}");
        Console.WriteLine($"Efficiency: {efficiency ?? "not provided"}");
        Console.WriteLine($"Gilding: {gilding ?? "not provided"}");
        Console.WriteLine("");

        foreach (var spec in itemSpecs)
        {
            EvaluateOneItem(
                allItemEffects,
                converter,
                spec.ItemId,
                spec.Tier,
                userValues,
                efficiency,
                gilding
            );
        }

        Console.WriteLine("");
        Console.WriteLine("Note:");
        Console.WriteLine("  This compares changed resources side-by-side.");
        Console.WriteLine("  It does not yet rank items globally because no objective function has been selected.");
        Console.WriteLine("  Next useful objective examples: Hero.AbilityPower, Pet.AbilityPower, Base.AllBuildingsProfit, Spell.EvocationEfficiency, etc.");
    }

    private static void EvaluateOneItem(
        IReadOnlyList<RawEffectDescriptor> allItemEffects,
        RawEffectConverter converter,
        string itemId,
        string tier,
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

        Console.WriteLine("============================================================");
        Console.WriteLine($"Item {itemId}, Tier {tier}");

        if (itemEffects.Count == 0)
        {
            Console.WriteLine("No effects found for this item/tier.");
            Console.WriteLine("============================================================");
            Console.WriteLine("");
            return;
        }

        Console.WriteLine($"Name: {itemEffects[0].SourceName}");
        Console.WriteLine($"Raw effects: {itemEffects.Count}");
        Console.WriteLine("");

        var context = new CalculatorContext();
        var convertedEffects = new List<ICalcEffect>();
        var unresolved = new List<RawEffectConversionResult>();

        foreach (var rawEffect in itemEffects)
        {
            var conversion = converter.Convert(rawEffect);

            if (conversion.Status != RawEffectConversionStatus.Convertible)
            {
                unresolved.Add(conversion);
                continue;
            }

            if (!context.Resources.TryGet(rawEffect.Target, out _))
            {
                var initial = CalcBigNumber.One;

                if (userValues.TryGetValue(rawEffect.Target, out var provided))
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
                        rawEffect.Target,
                        initial
                    )
                );
            }

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

        Console.WriteLine("Resource changes:");

        foreach (var resource in context.Resources.All.OrderBy(x => x.Key))
        {
            var before = beforeValues[resource.Key];
            var after = resource.Value.Value;

            Console.WriteLine($"  {resource.Key}: {before} -> {after}");
        }

        Console.WriteLine("");

        Console.WriteLine("Applied effects:");

        foreach (var effect in report.Effects)
        {
            Console.WriteLine(
                $"  {effect.Status}: {effect.TargetKey} [{effect.Operation}] {effect.Value}"
            );
        }

        if (unresolved.Count > 0)
        {
            Console.WriteLine("");
            Console.WriteLine("Unresolved conversions:");

            foreach (var item in unresolved)
            {
                Console.WriteLine(
                    $"  {item.Status}: {item.Descriptor.SourcePath} {item.Message}"
                );
            }
        }

        Console.WriteLine("");
        Console.WriteLine($"Fully verified: {report.IsFullyVerified && unresolved.Count == 0}");
        Console.WriteLine("============================================================");
        Console.WriteLine("");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(@"  --compare-items .\iw_workspace_vNext 1:4 0:5 ""Base.SoulPower=10"" ""Char.Intelligence=150"" ""Experiment.Efficiency=20"" ""Hero.AbilityPower=100"" ""--efficiency=2"" ""--gilding=1.5""");
        Console.WriteLine("");
        Console.WriteLine("Item specs use:");
        Console.WriteLine("  itemId:tier");
        Console.WriteLine("");
        Console.WriteLine("Stat values use:");
        Console.WriteLine("  Resource.Key=value");
    }
}
