using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveAchievementMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-achievement-map .\save_export.txt .\iw_workspace_vNext [zz_save_achievement_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\zz_save_achievement_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var saveDoc = JsonDocument.Parse(saveJson);
        var root = saveDoc.RootElement;

        var achievementKeyMap = LoadEnumMap(workspacePath, "AchievementKey.cs", "AchievementKey");
        var achievementCatalog = LoadAchievementCatalog(workspacePath);

        var savedCategories = ReadAchievementSaveArray(root, "AchievementsSave", achievementKeyMap);
        var recent = ReadAchievementSaveArray(root, "RAchieves", achievementKeyMap);
        var failedTriumphs = ReadTriumphFail(root, achievementKeyMap);

        var categoryEntries = new List<AchievementCategorySaveEntry>();
        var unlockedFlags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var totalUnlockedEstimate = 0;
        var totalPointsEstimate = 0;
        var catalogMatchedCategories = 0;
        var catalogUnmatchedCategories = 0;

        foreach (var saved in savedCategories.OrderBy(x => x.KeyId))
        {
            var catalogRows = achievementCatalog
                .Where(x => string.Equals(x.Key, saved.Key, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Level)
                .ThenBy(x => x.Name)
                .ToList();

            var unlockedRows = catalogRows
                .Take(Math.Max(0, saved.UnlockedLevelCount))
                .ToList();

            var points = unlockedRows.Sum(x => x.Points);
            var matched = catalogRows.Count > 0;

            if (matched)
            {
                catalogMatchedCategories++;
            }
            else
            {
                catalogUnmatchedCategories++;
            }

            totalUnlockedEstimate += saved.UnlockedLevelCount;
            totalPointsEstimate += points;

            if (!string.IsNullOrWhiteSpace(saved.Key))
            {
                unlockedFlags[saved.Key] = saved.UnlockedLevelCount > 0;
            }

            categoryEntries.Add(
                new AchievementCategorySaveEntry(
                    KeyId: saved.KeyId,
                    Key: saved.Key,
                    UnlockedLevelCount: saved.UnlockedLevelCount,
                    CatalogRowCount: catalogRows.Count,
                    CatalogMatched: matched,
                    EstimatedPoints: points,
                    UnlockedRows: unlockedRows.Select(x => new AchievementUnlockedRow(
                        Key: x.Key,
                        Level: x.Level,
                        Name: x.Name,
                        Points: x.Points,
                        Condition: x.Condition,
                        Parameter: x.Parameter,
                        Argument: x.Argument
                    )).ToList()
                )
            );
        }

        var classFlags = BuildFlagSubset(unlockedFlags, IsLikelyClassFlag);
        var secretFlags = BuildFlagSubset(unlockedFlags, x => x.StartsWith("Secret_", StringComparison.OrdinalIgnoreCase));
        var triumphFlags = BuildFlagSubset(unlockedFlags, x => x.StartsWith("T_", StringComparison.OrdinalIgnoreCase));

        var export = new SaveAchievementMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            Summary: new AchievementMapSummary(
                SavedAchievementCategoryCount: savedCategories.Count,
                CatalogAchievementRowCount: achievementCatalog.Count,
                CatalogMatchedCategoryCount: catalogMatchedCategories,
                CatalogUnmatchedCategoryCount: catalogUnmatchedCategories,
                EstimatedUnlockedCount: totalUnlockedEstimate,
                EstimatedAchievementPoints: totalPointsEstimate,
                RecentAchievementCount: recent.Count,
                FailedTriumphCount: failedTriumphs.Count,
                ClassFlagCount: classFlags.Count,
                SecretFlagCount: secretFlags.Count,
                TriumphFlagCount: triumphFlags.Count
            ),
            DerivedResources: new AchievementDerivedResources(
                AchievCount: totalUnlockedEstimate,
                AchievPoints: totalPointsEstimate,
                AchievSecrets: secretFlags.Count(x => x.Value),
                AchievTriumphs: triumphFlags.Count(x => x.Value)
            ),
            Categories: categoryEntries,
            RecentAchievements: recent,
            FailedTriumphs: failedTriumphs,
            ClassFlags: classFlags,
            SecretFlags: secretFlags,
            TriumphFlags: triumphFlags,
            Notes: new[]
            {
                "AchievementsSave is source-confirmed as AchievementSave(key, level), where level is the count of unlocked rows in the achievement category.",
                "EstimatedUnlockedCount is the sum of saved category unlocked counts.",
                "EstimatedAchievementPoints is derived by matching saved categories to Achievements.bytes.txt rows and summing Points for the first N rows in each category.",
                "RAchieves is the recent-achievement display/history list, not the full achievement state.",
                "Triumphs.TriumphFail stores failed triumph keys; unlocked triumph achievement flags are inferred from AchievementsSave categories starting with T_.",
                "Class/secret/triumph flags are derived from unlocked achievement category keys."
            }
        );

        var json = JsonSerializer.Serialize(
            export,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save achievement map");
        Console.WriteLine("--------------------");
        Console.WriteLine($"Save:       {savePath}");
        Console.WriteLine($"Workspace:  {workspacePath}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Categories: {savedCategories.Count}");
        Console.WriteLine($"Catalog rows: {achievementCatalog.Count}");
        Console.WriteLine($"Estimated unlocked: {totalUnlockedEstimate}");
        Console.WriteLine($"Estimated points:   {totalPointsEstimate}");
        Console.WriteLine($"Recent:     {recent.Count}");
        Console.WriteLine($"Failed triumphs: {failedTriumphs.Count}");
        Console.WriteLine($"Class flags:   {classFlags.Count}");
        Console.WriteLine($"Secret flags:  {secretFlags.Count}");
        Console.WriteLine($"Triumph flags: {triumphFlags.Count}");
    }

    private static List<AchievementSaveEntry> ReadAchievementSaveArray(
        JsonElement root,
        string propertyName,
        Dictionary<int, string> achievementKeyMap)
    {
        var result = new List<AchievementSaveEntry>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            var keyId = GetInt(item, "key");
            var level = GetInt(item, "level");

            var key = achievementKeyMap.TryGetValue(keyId, out var mapped)
                ? mapped
                : keyId.ToString();

            result.Add(
                new AchievementSaveEntry(
                    KeyId: keyId,
                    Key: key,
                    UnlockedLevelCount: level
                )
            );
        }

        return result;
    }

    private static List<TriumphFailEntry> ReadTriumphFail(
        JsonElement root,
        Dictionary<int, string> achievementKeyMap)
    {
        var result = new List<TriumphFailEntry>();

        if (!root.TryGetProperty("Triumphs", out var triumphs)
            || triumphs.ValueKind != JsonValueKind.Object
            || !triumphs.TryGetProperty("TriumphFail", out var fail)
            || fail.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in fail.EnumerateArray())
        {
            var keyId = ReadIntElement(item);
            var key = achievementKeyMap.TryGetValue(keyId, out var mapped)
                ? mapped
                : keyId.ToString();

            result.Add(new TriumphFailEntry(keyId, key));
        }

        return result;
    }

    private static Dictionary<string, bool> BuildFlagSubset(
        Dictionary<string, bool> flags,
        Func<string, bool> predicate)
    {
        return flags
            .Where(x => predicate(x.Key))
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsLikelyClassFlag(string key)
    {
        if (key.StartsWith("Secret_", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("T_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nonClass = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Click",
            "Autoclick",
            "Mana",
            "VoidMana",
            "VoidEntities",
            "HeroLevel",
            "PetLevel",
            "TotalBuildings",
            "TotalUpgrades",
            "PPS",
            "Souls",
            "Ascends",
            "Bats",
            "Catalysts",
            "HeroSkins",
            "PetSkins",
            "Backgrounds",
            "Orbs",
            "PlayedTime",
            "PlayedIdle",
            "PlayedOffline",
            "SpellCasts",
            "Shards",
            "Achieves",
            "AchievPoints",
            "Challenges",
            "ResourcesCollected",
            "ItemsUnlocked",
            "Keys",
            "Experiments",
            "EchoMaxLevel",
            "Tier1",
            "Tier2",
            "Tier3",
            "Tier4",
            "Tier5",
            "Tier6",
            "Tier7",
            "Tier8",
            "Tier9"
        };

        return !nonClass.Contains(key);
    }

    private static List<AchievementCatalogRow> LoadAchievementCatalog(string workspacePath)
    {
        var result = new List<AchievementCatalogRow>();
        var file = FindAchievementCatalogFile(workspacePath);

        if (file is null)
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            result.Add(
                new AchievementCatalogRow(
                    Key: GetString(item, "Key"),
                    Level: GetInt(item, "Level"),
                    Name: GetString(item, "Name"),
                    Points: GetInt(item, "Points"),
                    Condition: GetString(item, "Condition"),
                    Parameter: GetString(item, "Parameter"),
                    Argument: GetString(item, "Argument")
                )
            );
        }

        return result;
    }

    private static string? FindAchievementCatalogFile(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "raw_files", "Assets", "Resources", "jsonfiles", "Achievements.bytes.txt"),
            Path.Combine(workspacePath, "Assets", "Resources", "jsonfiles", "Achievements.bytes.txt"),
            Path.Combine(WorkspacePaths.ResolveExportRoot(workspacePath), "Assets", "Resources", "jsonfiles", "Achievements.bytes.txt"),
            Path.Combine(WorkspacePaths.ResolveExportRoot(workspacePath), "raw_files", "Assets", "Resources", "jsonfiles", "Achievements.bytes.txt")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        var roots = new[]
        {
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath)
        };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var file = Directory
                .GetFiles(root, "Achievements.bytes.txt", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (file is not null)
            {
                return Path.GetFullPath(file);
            }
        }

        return null;
    }

    private static Dictionary<int, string> LoadEnumMap(
        string workspacePath,
        string fileName,
        string enumName)
    {
        var result = new Dictionary<int, string>();
        var file = WorkspacePaths.FindSourceFile(workspacePath, fileName);

        if (file is null)
        {
            return result;
        }

        var text = File.ReadAllText(file);

        var match = Regex.Match(
            text,
            @"enum\s+" + Regex.Escape(enumName) + @"\s*\{(?<body>[\s\S]*?)\}",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            return result;
        }

        var current = 0;

        foreach (var raw in match.Groups["body"].Value.Split(','))
        {
            var part = raw.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var pieces = part.Split('=');
            var key = pieces[0].Trim();

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
    }

    private static string DecodeSaveString(string input)
    {
        var cleaned = Regex.Replace(input, @"\s+", "");
        var bytes = Convert.FromBase64String(cleaned);

        using var inputStream = new MemoryStream(bytes);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => value.GetRawText()
        };
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return ReadIntElement(value);
    }

    private static int ReadIntElement(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0;
    }

    private sealed record SaveAchievementMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        AchievementMapSummary Summary,
        AchievementDerivedResources DerivedResources,
        IReadOnlyList<AchievementCategorySaveEntry> Categories,
        IReadOnlyList<AchievementSaveEntry> RecentAchievements,
        IReadOnlyList<TriumphFailEntry> FailedTriumphs,
        IReadOnlyDictionary<string, bool> ClassFlags,
        IReadOnlyDictionary<string, bool> SecretFlags,
        IReadOnlyDictionary<string, bool> TriumphFlags,
        IReadOnlyList<string> Notes
    );

    private sealed record AchievementMapSummary(
        int SavedAchievementCategoryCount,
        int CatalogAchievementRowCount,
        int CatalogMatchedCategoryCount,
        int CatalogUnmatchedCategoryCount,
        int EstimatedUnlockedCount,
        int EstimatedAchievementPoints,
        int RecentAchievementCount,
        int FailedTriumphCount,
        int ClassFlagCount,
        int SecretFlagCount,
        int TriumphFlagCount
    );

    private sealed record AchievementDerivedResources(
        int AchievCount,
        int AchievPoints,
        int AchievSecrets,
        int AchievTriumphs
    );

    private sealed record AchievementCategorySaveEntry(
        int KeyId,
        string Key,
        int UnlockedLevelCount,
        int CatalogRowCount,
        bool CatalogMatched,
        int EstimatedPoints,
        IReadOnlyList<AchievementUnlockedRow> UnlockedRows
    );

    private sealed record AchievementUnlockedRow(
        string Key,
        int Level,
        string Name,
        int Points,
        string Condition,
        string Parameter,
        string Argument
    );

    private sealed record AchievementSaveEntry(
        int KeyId,
        string Key,
        int UnlockedLevelCount
    );

    private sealed record AchievementCatalogRow(
        string Key,
        int Level,
        string Name,
        int Points,
        string Condition,
        string Parameter,
        string Argument
    );

    private sealed record TriumphFailEntry(
        int KeyId,
        string Key
    );
}
