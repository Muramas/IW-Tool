namespace IdleWizard.BuildTool.Core.Data;

public sealed record RawEffectDescriptor(
    string SourceFile,
    string SourceKind,
    string SourceId,
    string SourceName,
    string SourcePath,
    string Target,
    string Effect,
    string Addendum,
    string Multiplier,
    string Diminish,
    string Notes
);
