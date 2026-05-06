namespace IdleWizard.BuildTool.Core.Enchanting;

public static class EnchantCalculator
{
    public static EnchantCalculation Calculate(
        double rate,
        int baseLevel,
        int bonusLevel)
    {
        var effectiveLevel = baseLevel + bonusLevel;
        var multiplier = Math.Pow(1.0 + rate, effectiveLevel);
        var bonus = multiplier - 1.0;

        return new EnchantCalculation(
            rate,
            baseLevel,
            bonusLevel,
            effectiveLevel,
            multiplier,
            bonus,
            bonus * 100.0
        );
    }
}

public sealed record EnchantCalculation(
    double Rate,
    int BaseLevel,
    int BonusLevel,
    int EffectiveLevel,
    double Multiplier,
    double Bonus,
    double BonusPercent
);
