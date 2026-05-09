using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class CalculateBuildCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --calculate-build active [H:\IdleWizard\Working\IW_Optimizer]");
            Console.WriteLine(@"  --calculate-build arcanist_risengiant_main [H:\IdleWizard\Working\IW_Optimizer]");
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
        var manifestPath = Path.Combine(buildRoot, "build_manifest.json");

        if (!File.Exists(manifestPath))
        {
            Console.WriteLine($"Build manifest not found: {manifestPath}");
            return;
        }

        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject();
        if (manifest is null)
        {
            Console.WriteLine($"Build manifest root is not a JSON object: {manifestPath}");
            return;
        }

        var activeRawExport = GetNodeString(manifest, "ActiveRawExport");
        if (string.IsNullOrWhiteSpace(activeRawExport))
        {
            Console.WriteLine($"Build manifest does not specify ActiveRawExport: {manifestPath}");
            return;
        }

        var rawSavePath = Path.Combine(buildRoot, "imports", "raw", activeRawExport);
        if (!File.Exists(rawSavePath))
        {
            Console.WriteLine($"Active raw save export not found: {rawSavePath}");
            return;
        }

        var sourceWorkspace = ResolveSourceWorkspace(root);
        if (string.IsNullOrWhiteSpace(sourceWorkspace))
        {
            Console.WriteLine("Could not resolve a source workspace containing RealmUpgrades.bytes.");
            Console.WriteLine("Checked:");
            Console.WriteLine($"  {Path.Combine(root, "exports", "IW_Current")}");
            Console.WriteLine($"  {Path.Combine(root, "iw_workspace_vNext")}");
            return;
        }

        var calculationsDir = Path.Combine(buildRoot, "calculations");
        Directory.CreateDirectory(calculationsDir);

        var realmMemoryDetailPath = Path.Combine(calculationsDir, "realm_memory_applied_effects.json");
        var realmMemorySummaryPath = Path.Combine(calculationsDir, "realm_memory_owned_target_summary.json");
        var saveContextPath = Path.Combine(calculationsDir, "save_calculation_context.json");

        Console.WriteLine("IW Optimizer build calculation");
        Console.WriteLine("------------------------------");
        Console.WriteLine($"Root:            {root}");
        Console.WriteLine($"BuildId:         {buildId}");
        Console.WriteLine($"BuildRoot:       {buildRoot}");
        Console.WriteLine($"RawSave:         {rawSavePath}");
        Console.WriteLine($"SourceWorkspace: {sourceWorkspace}");
        Console.WriteLine($"CalculationsDir: {calculationsDir}");
        Console.WriteLine("");

        Console.WriteLine("Generating realm memory applied effects...");
        SaveRealmMemoryAppliedEffectsCommand.Run(new[]
        {
            "--save-realm-memory-applied-effects",
            rawSavePath,
            sourceWorkspace,
            realmMemoryDetailPath,
            realmMemorySummaryPath
        });

        if (!File.Exists(realmMemorySummaryPath))
        {
            Console.WriteLine($"Expected realm memory summary was not created: {realmMemorySummaryPath}");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("Generating save calculation context...");
        SaveCalculationContextCommand.Run(new[]
        {
            "--save-calculation-context",
            rawSavePath,
            sourceWorkspace,
            saveContextPath
        });

        if (!File.Exists(saveContextPath))
        {
            Console.WriteLine($"Expected save calculation context was not created: {saveContextPath}");
            return;
        }

        Console.WriteLine("");
        Console.WriteLine("Patching generated context RealmMemoryAppliedEffects reference to build calculations folder...");
        PatchContextRealmMemoryReference(
            buildId,
            realmMemorySummaryPath,
            saveContextPath
        );

        UpdateManifestAfterCalculation(
            manifest,
            manifestPath,
            realmMemoryDetailPath,
            realmMemorySummaryPath,
            saveContextPath,
            buildId
        );

        var summaryRoot = JsonNode.Parse(File.ReadAllText(realmMemorySummaryPath))?.AsObject();
        var summary = summaryRoot?["Summary"]?.AsObject();

        Console.WriteLine("");
        Console.WriteLine("Build calculation complete");
        Console.WriteLine("--------------------------");
        Console.WriteLine($"BuildId: {buildId}");
        Console.WriteLine("");
        Console.WriteLine("Generated files:");
        Console.WriteLine($"  {realmMemoryDetailPath}");
        Console.WriteLine($"  {realmMemorySummaryPath}");
        Console.WriteLine($"  {saveContextPath}");
        Console.WriteLine("");

        if (summary is not null)
        {
            Console.WriteLine("Key values:");
            Console.WriteLine($"  OwnedTargetCount: {GetNodeInt(summary, "OwnedTargetCount")}");
            Console.WriteLine($"  BaseAllBuildingsProfitOwnedMultiplier: {GetNodeString(summary, "BaseAllBuildingsProfitOwnedMultiplier")}");
            Console.WriteLine($"  RealmIncomeOwnedMultiplier: {GetNodeString(summary, "RealmIncomeOwnedMultiplier")}");
            Console.WriteLine($"  FormulaStatus: {GetNodeString(summary, "FormulaStatus")}");
        }
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

        return GetNodeString(activeBuild, "ActiveBuildId");
    }

    private static string ResolveSourceWorkspace(string root)
    {
        var candidates = new[]
        {
            Path.Combine(root, "exports", "IW_Current"),
            Path.Combine(root, "iw_workspace_vNext")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && ContainsRealmUpgrades(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static bool ContainsRealmUpgrades(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "raw_files", "Assets", "Resources", "jsonfiles", "RealmUpgrades.bytes"),
            Path.Combine(workspacePath, "Assets", "Resources", "jsonfiles", "RealmUpgrades.bytes")
        };

        return candidates.Any(File.Exists);
    }

    private static void PatchContextRealmMemoryReference(
        string buildId,
        string realmMemorySummaryPath,
        string saveContextPath)
    {
        var summaryRoot = JsonNode.Parse(File.ReadAllText(realmMemorySummaryPath))?.AsObject();
        if (summaryRoot is null)
        {
            throw new InvalidOperationException("Realm memory summary root is not a JSON object.");
        }

        var summary = summaryRoot["Summary"]?.AsObject();
        if (summary is null)
        {
            throw new InvalidOperationException("Realm memory summary does not contain Summary object.");
        }

        var context = JsonNode.Parse(File.ReadAllText(saveContextPath))?.AsObject();
        if (context is null)
        {
            throw new InvalidOperationException("Save calculation context root is not a JSON object.");
        }

        var relativeSummaryPath = $"runtime/builds/{buildId}/calculations/realm_memory_owned_target_summary.json";

        context["RealmMemoryAppliedEffects"] = new JsonObject
        {
            ["File"] = relativeSummaryPath,
            ["OwnedTargetCount"] = GetNodeInt(summary, "OwnedTargetCount"),
            ["BaseAllBuildingsProfitOwnedMultiplier"] = GetNodeString(summary, "BaseAllBuildingsProfitOwnedMultiplier"),
            ["BaseAllBuildingsProfitOwnedBonusPercent"] = GetNodeString(summary, "BaseAllBuildingsProfitOwnedBonusPercent"),
            ["RealmIncomeOwnedMultiplier"] = GetNodeString(summary, "RealmIncomeOwnedMultiplier"),
            ["RealmIncomeOwnedBonusPercent"] = GetNodeString(summary, "RealmIncomeOwnedBonusPercent"),
            ["FormulaStatus"] = GetNodeString(summary, "FormulaStatus"),
            ["Notes"] = "Compact source-confirmed owned Realm memory target bonus summary. Detailed data is stored in this build profile's calculations folder."
        };

        File.WriteAllText(
            saveContextPath,
            context.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );
    }

    private static void UpdateManifestAfterCalculation(
        JsonObject manifest,
        string manifestPath,
        string realmMemoryDetailPath,
        string realmMemorySummaryPath,
        string saveContextPath,
        string buildId)
    {
        var now = DateTime.UtcNow.ToString("O");

        manifest["UpdatedAtUtc"] = now;
        manifest["LastCalculatedAtUtc"] = now;

        manifest["LastCalculation"] = new JsonObject
        {
            ["RealmMemoryAppliedEffects"] = $"runtime/builds/{buildId}/calculations/realm_memory_applied_effects.json",
            ["RealmMemoryOwnedTargetSummary"] = $"runtime/builds/{buildId}/calculations/realm_memory_owned_target_summary.json",
            ["SaveCalculationContext"] = $"runtime/builds/{buildId}/calculations/save_calculation_context.json"
        };

        File.WriteAllText(
            manifestPath,
            manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
        );
    }

    private static string GetNodeString(JsonObject obj, string name)
    {
        if (!obj.TryGetPropertyValue(name, out var node) || node is null)
        {
            return string.Empty;
        }

        try
        {
            return node.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            // Continue below.
        }

        try
        {
            return node.GetValue<decimal>().ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            // Continue below.
        }

        try
        {
            return node.GetValue<double>().ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            // Continue below.
        }

        try
        {
            return node.GetValue<int>().ToString(CultureInfo.InvariantCulture);
        }
        catch
        {
            // Continue below.
        }

        return node.ToJsonString().Trim('"');
    }

    private static int GetNodeInt(JsonObject obj, string name)
    {
        if (!obj.TryGetPropertyValue(name, out var node) || node is null)
        {
            return 0;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch
        {
            // Continue below.
        }

        if (int.TryParse(GetNodeString(obj, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0;
    }
}
// EOF - CalculateBuildCommand.cs
