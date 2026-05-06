namespace IdleWizard.BuildTool.Core.Data;

public enum RawEffectConversionStatus
{
    Convertible,
    UnsupportedEffectType,
    MissingTarget,
    InvalidNumber
}

public sealed record RawEffectConversionResult(
    RawEffectDescriptor Descriptor,
    RawEffectConversionStatus Status,
    string Message,
    string Target,
    string Addendum,
    string Multiplier
);
