using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;

namespace IdleWizard.BuildTool.Core.Effects;

public sealed class LinearEffect : ICalcEffect
{
    public string SourceId { get; }
    public string TargetKey { get; }
    public EffectOperation Operation => EffectOperation.Linear;

    public CalcBigNumber Addendum { get; }
    public CalcBigNumber Multiplier { get; }

    public CalcBigNumber? Efficiency { get; }

    public LinearEffect(
        string sourceId,
        string targetKey,
        CalcBigNumber addendum,
        CalcBigNumber multiplier,
        CalcBigNumber? efficiency = null)
    {
        SourceId = sourceId;
        TargetKey = targetKey;
        Addendum = addendum;
        Multiplier = multiplier;
        Efficiency = efficiency;
    }

    public EffectApplyResult Apply(CalculatorContext context)
    {
        if (!context.Resources.TryGet(TargetKey, out var variable) || variable is null)
        {
            return new(
                SourceId,
                TargetKey,
                Operation,
                EffectStatus.UnresolvedTarget,
                "Target resource key is not registered."
            );
        }

        var adjustedAddendum = Addendum;
        var adjustedMultiplier = Multiplier;

        if (Efficiency is not null && Efficiency.Value.Mantissa != 0)
        {
            var e = Efficiency.Value;

            if (!(e.Mantissa == 1 && e.Exponent == 0))
            {
                adjustedAddendum *= e;

                if (!(adjustedMultiplier.Mantissa == 1 && adjustedMultiplier.Exponent == 0)
                    && adjustedMultiplier.Mantissa != 0)
                {
                    if (adjustedMultiplier.Log10() >= 0)
                    {
                        adjustedMultiplier =
                            CalcBigNumber.One
                            + (adjustedMultiplier - CalcBigNumber.One) * e;
                    }
                    else
                    {
                        adjustedMultiplier /= e;
                    }
                }
            }
        }

        variable.Change(adjustedAddendum, adjustedMultiplier);

        return new(
            SourceId,
            TargetKey,
            Operation,
            EffectStatus.Applied,
            "Applied linear effect.",
            $"{adjustedAddendum}@{adjustedMultiplier}"
        );
    }
}
