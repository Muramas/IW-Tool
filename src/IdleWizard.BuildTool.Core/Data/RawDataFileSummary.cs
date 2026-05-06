namespace IdleWizard.BuildTool.Core.Data;

public sealed record RawDataFileSummary(
    string File,
    long Size,
    bool ParsedJson,
    string RootType,
    string RecordPath,
    int RecordCount,
    IReadOnlyList<string> Fields,
    string? Error
);
