namespace IdleWizard.BuildTool.Core.Effects;

public enum EffectStatus
{
    Applied,
    SkippedConditionFalse,
    UnresolvedCondition,
    UnresolvedTarget,
    UnsupportedOperation,
    Error
}

public sealed record EffectApplyResult(
    string SourceId,
    string TargetKey,
    EffectOperation Operation,
    EffectStatus Status,
    string Message,
    string? Value = null
);
