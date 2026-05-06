namespace IdleWizard.BuildTool.Core.Data;

public sealed record GameDataRecord(
    string SourceFile,
    int Index,
    IReadOnlyDictionary<string, string> Fields
);
