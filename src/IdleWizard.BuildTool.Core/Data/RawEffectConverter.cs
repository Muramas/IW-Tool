using System.Globalization;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Numbers;

namespace IdleWizard.BuildTool.Core.Data;

public sealed class RawEffectConverter
{
    public RawEffectConversionResult Convert(RawEffectDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Target))
        {
            return new RawEffectConversionResult(
                descriptor,
                RawEffectConversionStatus.MissingTarget,
                "Raw effect has no target resource.",
                descriptor.Target,
                descriptor.Addendum,
                descriptor.Multiplier
            );
        }

        if (descriptor.Effect != "0")
        {
            return new RawEffectConversionResult(
                descriptor,
                RawEffectConversionStatus.UnsupportedEffectType,
                $"Only effect type 0 is currently convertible. Found effect={descriptor.Effect}.",
                descriptor.Target,
                descriptor.Addendum,
                descriptor.Multiplier
            );
        }

        if (!CanParseNumber(descriptor.Addendum))
        {
            return new RawEffectConversionResult(
                descriptor,
                RawEffectConversionStatus.InvalidNumber,
                $"Addendum is not a supported numeric value: {descriptor.Addendum}",
                descriptor.Target,
                descriptor.Addendum,
                descriptor.Multiplier
            );
        }

        if (!CanParseNumber(descriptor.Multiplier))
        {
            return new RawEffectConversionResult(
                descriptor,
                RawEffectConversionStatus.InvalidNumber,
                $"Multiplier is not a supported numeric value: {descriptor.Multiplier}",
                descriptor.Target,
                descriptor.Addendum,
                descriptor.Multiplier
            );
        }

        return new RawEffectConversionResult(
            descriptor,
            RawEffectConversionStatus.Convertible,
            "Convertible to LinearEffect.",
            descriptor.Target,
            descriptor.Addendum,
            descriptor.Multiplier
        );
    }

    public ICalcEffect ToLinearEffect(
        RawEffectDescriptor descriptor,
        string? efficiencyValue = null,
        string? gildingValue = null)
    {
        var conversion = Convert(descriptor);

        if (conversion.Status != RawEffectConversionStatus.Convertible)
        {
            throw new InvalidOperationException(conversion.Message);
        }

        return new LinearEffect(
            $"{descriptor.SourceKind}:{descriptor.SourceId}:{descriptor.SourcePath}",
            descriptor.Target,
            CalcBigNumber.Parse(descriptor.Addendum),
            CalcBigNumber.Parse(descriptor.Multiplier),
            CalculatePowDiminishedEfficiency(
                descriptor.Diminish,
                efficiencyValue,
                gildingValue
            )
        );
    }


    private static CalcBigNumber? CalculatePowDiminishedEfficiency(
        string diminish,
        string? efficiencyValue,
        string? gildingValue)
    {
        if (string.IsNullOrWhiteSpace(efficiencyValue)
            && string.IsNullOrWhiteSpace(gildingValue))
        {
            return null;
        }

        var combined = CalcBigNumber.One;

        if (!string.IsNullOrWhiteSpace(gildingValue))
        {
            combined *= CalcBigNumber.Parse(gildingValue);
        }

        if (!string.IsNullOrWhiteSpace(efficiencyValue))
        {
            combined *= CalcBigNumber.Parse(efficiencyValue);
        }

        var pow = 1.0;

        if (!string.IsNullOrWhiteSpace(diminish))
        {
            double.TryParse(
                diminish,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out pow
            );
        }

        if (Math.Abs(pow - 1.0) > 0.0000001)
        {
            return combined.Pow(pow);
        }

        return combined;
    }
    private static bool CanParseNumber(string value)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out _
        );
    }
}

