using System.Globalization;
using IdleWizard.BuildTool.Core.Enchanting;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class EnchantValueCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            PrintUsage();
            return;
        }

        if (!double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
        {
            Console.WriteLine($"Could not parse rate: {args[1]}");
            return;
        }

        if (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseLevel))
        {
            Console.WriteLine($"Could not parse base level: {args[2]}");
            return;
        }

        if (!int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bonusLevel))
        {
            Console.WriteLine($"Could not parse bonus level: {args[3]}");
            return;
        }

        var result = EnchantCalculator.Calculate(rate, baseLevel, bonusLevel);

        Console.WriteLine("Enchant value");
        Console.WriteLine("-------------");
        Console.WriteLine($"Rate:            {result.Rate:P2}");
        Console.WriteLine($"Base level:      {result.BaseLevel}");
        Console.WriteLine($"Bonus level:     {result.BonusLevel}");
        Console.WriteLine($"Effective level: {result.EffectiveLevel}");
        Console.WriteLine($"Multiplier:      {result.Multiplier:0.############}");
        Console.WriteLine($"Current bonus:   {result.BonusPercent:0.00}%");
        Console.WriteLine("");
        Console.WriteLine("Formula:");
        Console.WriteLine("  (1 + rate) ^ (baseLevel + bonusLevel) - 1");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  --enchant-value RATE BASE_LEVEL BONUS_LEVEL");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine("  --enchant-value 0.05 1 1");
        Console.WriteLine("  --enchant-value 0.15 5 5");
        Console.WriteLine("  --enchant-value 0.15 7 1");
    }
}
