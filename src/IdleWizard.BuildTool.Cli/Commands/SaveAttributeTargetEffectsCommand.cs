using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveAttributeTargetEffectsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@" --save-attribute-target-effects active [H:\IdleWizard\Working\IW_Optimizer]");
            Console.WriteLine(@" --save-attribute-target-effects level_validation_export_20260508 [H:\IdleWizard\Working\IW_Optimizer]");
            return;
        }

        var requestedBuildId = args[1];
        var root = args.Length >= 3
            ? Path.GetFullPath(args[2])
            : Directory.GetCurrentDirectory();

        var buildId = requestedBuildId.Equals("active", StringComparison.OrdinalIgnoreCase)
            ? ReadActiveBuildId(root)
            : requestedBuildId;

        if (string.IsNullOrWhiteSpace(buildId))
        {
            Console.WriteLine("No build ID was provided and no active build could be resolved.");
            return;
        }

        var buildRoot = Path.Combine(root, "runtime", "builds", buildId);
        var mappingsDir = Path.Combine(buildRoot, "mappings");
        var calculationsDir = Path.Combine(buildRoot, "calculations");

        var attributeMapPath = Path.Combine(mappingsDir, "attribute_effects.json");
        var outputPath = Path.Combine(calculationsDir, "attribute_target_effects.json");

        Directory.CreateDirectory(calculationsDir);

        if (!File.Exists(attributeMapPath))
        {
            Console.WriteLine($"Attribute map not found: {attributeMapPath}");
            return;
        }

        var attributeMap = JsonNode.Parse(File.ReadAllText(attributeMapPath))?.AsObject();

        if (attributeMap is null)
        {
            Console.WriteLine($"Attribute map root is not a JSON object: {attributeMapPath}");
            return;
        }

        var effects = BuildEffects(attributeMap);

        var targetSummaries = effects
            .Where(x => !string.IsNullOrWhiteSpace(x.Target))
            .GroupBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .Select(group => new AttributeTargetSummary(
                Target: group.Key,
                EffectCount: group.Count(),
                OwnedEffectCount: group.Count(x => x.Owned),
                SourceConfirmedPreviewCount: group.Count(x => x.FormulaStatus.StartsWith("SourceConfirmedPreview", StringComparison.OrdinalIgnoreCase)),
                UnsupportedOrMissingFormulaCount: group.Count(x =>
                    x.FormulaStatus.Equals("UnsupportedEffect", StringComparison.OrdinalIgnoreCase)
                    || x.FormulaStatus.Equals("MissingTarget", StringComparison.OrdinalIgnoreCase))
            ))
            .OrderByDescending(x => x.OwnedEffectCount)
            .ThenBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var summary = new AttributeTargetEffectsSummary(
            TotalEffectCount: effects.Count,
            OwnedEffectCount: effects.Count(x => x.Owned),
            TargetCount: targetSummaries.Count,
            SourceConfirmedPreviewCount: effects.Count(x => x.FormulaStatus.StartsWith("SourceConfirmedPreview", StringComparison.OrdinalIgnoreCase)),
            MissingTargetCount: effects.Count(x => x.FormulaStatus.Equals("MissingTarget", StringComparison.OrdinalIgnoreCase)),
            UnsupportedEffectCount: effects.Count(x => x.FormulaStatus.Equals("UnsupportedEffect", StringComparison.OrdinalIgnoreCase))
        );

        var export = new AttributeTargetEffectsExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            BuildId: buildId,
            AttributeMapFile: attributeMapPath,
            OutputFile: outputPath,
            Summary: summary,
            TargetSummaries: targetSummaries,
            Effects: effects,
            Notes: new[]
            {
                "Phase 2C attribute target aggregation output.",
                "This file represents mapped attribute effects by target so coverage can distinguish mapped-and-represented rows from mapped-but-not-represented rows.",
                "This is not the final Phase 3 formula chain.",
                "Contribution fields preserve source raw values for targets whose final calculation formula is not yet wired."
            }
        );

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true })
        );

        Console.WriteLine("Attribute target effects");
        Console.WriteLine("------------------------");
        Console.WriteLine($"BuildId: {buildId}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine("");
        Console.WriteLine("Summary:");
        Console.WriteLine($" TotalEffectCount: {summary.TotalEffectCount}");
        Console.WriteLine($" OwnedEffectCount: {summary.OwnedEffectCount}");
        Console.WriteLine($" TargetCount: {summary.TargetCount}");
        Console.WriteLine($" SourceConfirmedPreviewCount: {summary.SourceConfirmedPreviewCount}");
        Console.WriteLine($" MissingTargetCount: {summary.MissingTargetCount}");
        Console.WriteLine($" UnsupportedEffectCount: {summary.UnsupportedEffectCount}");
    }

    private static IReadOnlyList<AttributeTargetEffect> BuildEffects(JsonObject attributeMap)
    {
        var result = new List<AttributeTargetEffect>();
        var attributes = attributeMap["Attributes"]?.AsArray();

        if (attributes is null)
        {
            return result;
        }

        foreach (var attributeNode in attributes)
        {
            if (attributeNode is not JsonObject attribute)
            {
                continue;
            }

            var saveKey = GetString(attribute, "SaveKey");
            var displayName = GetString(attribute, "DisplayName");
            var saveValue = GetInt(attribute, "SaveValue");
            var activeRecords = attribute["ActiveRecords"]?.AsArray();

            if (activeRecords is null)
            {
                continue;
            }

            foreach (var recordNode in activeRecords)
            {
                if (recordNode is not JsonObject record)
                {
                    continue;
                }

                var target = GetString(record, "Target");
                var requiredLevel = GetInt(record, "Level");
                var add = GetString(record, "Add");
                var mult = GetString(record, "Mult");
                var effectName = FirstNonEmpty(GetString(record, "Effect"), "Linear");
                var weightKey = GetString(record, "Weight");
                var description = GetString(record, "Description");
                var owned = saveValue >= requiredLevel;

                var rawContribution = FirstNonEmpty(add, mult, effectName);
                var formulaStatus = string.IsNullOrWhiteSpace(target)
                    ? "MissingTarget"
                    : IsSupportedPreviewEffect(effectName)
                        ? "SourceConfirmedPreviewMappedAttributeTarget"
                        : "UnsupportedEffect";

                result.Add(new AttributeTargetEffect(
                    AttributeKey: saveKey,
                    AttributeName: displayName,
                    AttributeSaveValue: saveValue,
                    RequiredLevel: requiredLevel,
                    Owned: owned,
                    Target: target,
                    Add: add,
                    Mult: mult,
                    EffectName: effectName,
                    WeightKey: weightKey,
                    RawContribution: rawContribution,
                    AppliedAdd: "",
                    AppliedMultiplierText: rawContribution,
                    AppliedBonusPercent: "",
                    FormulaStatus: formulaStatus,
                    Notes: description
                ));
            }
        }

        return result
            .OrderBy(x => x.AttributeKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.RequiredLevel)
            .ThenBy(x => x.Target, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsSupportedPreviewEffect(string effectName)
    {
        return effectName.Equals("Linear", StringComparison.OrdinalIgnoreCase)
            || effectName.Equals("Log10", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(effectName);
    }

    private static string ReadActiveBuildId(string root)
    {
        var activeBuildPath = Path.Combine(root, "runtime", "active_build.json");

        if (!File.Exists(activeBuildPath))
        {
            return string.Empty;
        }

        var activeBuild = JsonNode.Parse(File.ReadAllText(activeBuildPath))?.AsObject();

        if (activeBuild is null)
        {
            return string.Empty;
        }

        return GetString(activeBuild, "ActiveBuildId");
    }

    private static string GetString(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return string.Empty;
        }

        try
        {
            return node.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            return node.ToJsonString().Trim('"');
        }
    }

    private static int GetInt(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return 0;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch
        {
            if (int.TryParse(GetString(obj, propertyName), out var parsed))
            {
                return parsed;
            }

            return 0;
        }
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private sealed record AttributeTargetEffectsExport(
        string GeneratedAtUtc,
        string BuildId,
        string AttributeMapFile,
        string OutputFile,
        AttributeTargetEffectsSummary Summary,
        IReadOnlyList<AttributeTargetSummary> TargetSummaries,
        IReadOnlyList<AttributeTargetEffect> Effects,
        IReadOnlyList<string> Notes
    );

    private sealed record AttributeTargetEffectsSummary(
        int TotalEffectCount,
        int OwnedEffectCount,
        int TargetCount,
        int SourceConfirmedPreviewCount,
        int MissingTargetCount,
        int UnsupportedEffectCount
    );

    private sealed record AttributeTargetSummary(
        string Target,
        int EffectCount,
        int OwnedEffectCount,
        int SourceConfirmedPreviewCount,
        int UnsupportedOrMissingFormulaCount
    );

    private sealed record AttributeTargetEffect(
        string AttributeKey,
        string AttributeName,
        int AttributeSaveValue,
        int RequiredLevel,
        bool Owned,
        string Target,
        string Add,
        string Mult,
        string EffectName,
        string WeightKey,
        string RawContribution,
        string AppliedAdd,
        string AppliedMultiplierText,
        string AppliedBonusPercent,
        string FormulaStatus,
        string Notes
    );
}

// EOF - SaveAttributeTargetEffectsCommand.cs
