using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveUnlockMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-unlock-map .\save_export.txt .\iw_workspace_vNext [save_unlock_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_unlock_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var upgradeCatalog = LoadUpgradeCatalog(workspacePath);

        var purchasedUpgradeIds = ReadIntArray(root, "Upgrades");
        var mappedUpgrades = purchasedUpgradeIds
            .Select(id =>
            {
                upgradeCatalog.TryGetValue(id, out var catalog);

                return new UpgradeUnlockEntry(
                    Id: id,
                    Name: catalog?.Name ?? "",
                    Key: catalog?.Key ?? "",
                    Category: catalog?.Category ?? "",
                    CatalogMatched: catalog is not null,
                    MappingStatus: catalog is null ? "IdOnly" : "CatalogMatched",
                    Notes: catalog is null
                        ? "Purchased upgrade ID from save. Needs source/catalog mapping."
                        : "Purchased upgrade matched to catalog."
                );
            })
            .OrderBy(x => x.Id)
            .ToList();

        var achievementSections = BuildAchievementSections(root);
        var challengeSections = BuildChallengeSections(root);
        var progress = BuildUnlockProgress(root);

        var export = new SaveUnlockMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            UpgradeSummary: new UpgradeUnlockSummary(
                PurchasedUpgradeCount: purchasedUpgradeIds.Count,
                CatalogMatchedCount: mappedUpgrades.Count(x => x.CatalogMatched),
                CatalogUnmatchedCount: mappedUpgrades.Count(x => !x.CatalogMatched),
                FirstPurchasedUpgradeIds: purchasedUpgradeIds.Take(40).ToList(),
                LastPurchasedUpgradeIds: purchasedUpgradeIds.Skip(Math.Max(0, purchasedUpgradeIds.Count - 40)).ToList()
            ),
            PurchasedUpgrades: mappedUpgrades,
            AchievementSections: achievementSections,
            ChallengeSections: challengeSections,
            Progress: progress,
            SourceMappingNotes: new[]
            {
                "Upgrades[] is source-save data and appears to be purchased upgrade IDs.",
                "Upgrade ID effects are not formula-ready until mapped against game source/catalog definitions.",
                "Achievement and challenge sections are exported by shape first; exact formula relevance requires source mapping.",
                "This command is a developer mapping artifact, not live UI data."
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

        Console.WriteLine("Save unlock map");
        Console.WriteLine("---------------");
        Console.WriteLine($"Save:        {savePath}");
        Console.WriteLine($"Workspace:   {workspacePath}");
        Console.WriteLine($"Output:      {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Upgrades:    {mappedUpgrades.Count}");
        Console.WriteLine($"Matched:     {mappedUpgrades.Count(x => x.CatalogMatched)}");
        Console.WriteLine($"Unmatched:   {mappedUpgrades.Count(x => !x.CatalogMatched)}");
        Console.WriteLine($"Achievements sections: {achievementSections.Count}");
        Console.WriteLine($"Challenge sections:    {challengeSections.Count}");
        Console.WriteLine("");
        Console.WriteLine("First purchased upgrade IDs:");
        Console.WriteLine("  " + string.Join(", ", purchasedUpgradeIds.Take(40)));
    }

    private static List<SaveSectionShape> BuildAchievementSections(JsonElement root)
    {
        var candidates = new[]
        {
            "AchievementsSave",
            "RAchieves",
            "RecentlyAchieved",
            "AchievUnlocked",
            "AchievsPoints",
            "Triumphs",
            "TriumphsSave"
        };

        var result = new List<SaveSectionShape>();

        foreach (var name in candidates)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            result.Add(BuildSectionShape(name, value, GuessAchievementNotes(name, value)));
        }

        return result;
    }

    private static List<SaveSectionShape> BuildChallengeSections(JsonElement root)
    {
        var result = new List<SaveSectionShape>();

        foreach (var property in root.EnumerateObject())
        {
            var name = property.Name;

            if (!name.Contains("Challenge", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Trial", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("Realm", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(
                BuildSectionShape(
                    name,
                    property.Value,
                    "Challenge/trial/realm-related save field. Needs source mapping for formula relevance."
                )
            );
        }

        return result
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static List<UnlockProgressEntry> BuildUnlockProgress(JsonElement root)
    {
        var fields = new[]
        {
            "Ascends",
            "AscendsRealm",
            "BoughtUpgrades",
            "HeroMaxLevelAllTime",
            "PetMaxLevel",
            "PetMaxLevelAllTime",
            "ApprenticeMaxLevelRealm",
            "Bats",
            "BatsE",
            "BatsR",
            "BatsOnly",
            "ClassTime",
            "BuildingLevels"
        };

        var result = new List<UnlockProgressEntry>();

        foreach (var field in fields)
        {
            if (!root.TryGetProperty(field, out var value))
            {
                continue;
            }

            result.Add(
                new UnlockProgressEntry(
                    Name: field,
                    JsonKind: value.ValueKind.ToString(),
                    Value: BuildPreview(value),
                    Notes: GuessProgressNotes(field)
                )
            );
        }

        return result;
    }

    private static SaveSectionShape BuildSectionShape(
        string name,
        JsonElement value,
        string notes)
    {
        var objectProperties = new List<string>();
        var firstValues = new List<string>();
        var lastValues = new List<string>();

        var arrayLength = value.ValueKind == JsonValueKind.Array
            ? value.GetArrayLength()
            : 0;

        var objectPropertyCount = value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().Count()
            : 0;

        if (value.ValueKind == JsonValueKind.Object)
        {
            objectProperties = value
                .EnumerateObject()
                .Select(x => x.Name)
                .Take(30)
                .ToList();
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            var values = value.EnumerateArray()
                .Select(PreviewValue)
                .ToList();

            firstValues = values.Take(20).ToList();
            lastValues = values.Skip(Math.Max(0, values.Count - 20)).ToList();
        }

        return new SaveSectionShape(
            Name: name,
            JsonKind: value.ValueKind.ToString(),
            ArrayLength: arrayLength,
            ObjectPropertyCount: objectPropertyCount,
            ObjectProperties: objectProperties,
            FirstValues: firstValues,
            LastValues: lastValues,
            Preview: BuildPreview(value),
            Notes: notes
        );
    }

    private static Dictionary<int, UpgradeCatalogEntry> LoadUpgradeCatalog(string workspacePath)
    {
        var result = new Dictionary<int, UpgradeCatalogEntry>();

        // Best effort: try exported data first.
        var dataCandidates = new[]
        {
            "Upgrades",
            "Upgrade",
            "UpgradesData",
            "UpgradeData"
        };

        foreach (var candidate in dataCandidates)
        {
            var file = WorkspacePaths.FindDataFile(workspacePath, candidate);

            if (file is null)
            {
                continue;
            }

            TryLoadUpgradeCatalogFromJson(file, result);

            if (result.Count > 0)
            {
                return result;
            }
        }

        // Fallback: scan source text for simple ID/name patterns.
        var sourceRoot = Path.Combine(
            WorkspacePaths.ResolveExportRoot(workspacePath),
            "Scripts",
            "Assembly-CSharp"
        );

        if (Directory.Exists(sourceRoot))
        {
            foreach (var file in Directory.GetFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
            {
                TryLoadUpgradeCatalogFromSource(file, result);
            }
        }

        return result;
    }

    private static void TryLoadUpgradeCatalogFromJson(
        string file,
        Dictionary<int, UpgradeCatalogEntry> result)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var item in doc.RootElement.EnumerateArray())
            {
                var id = FirstNonZero(
                    GetInt(item, "Id"),
                    GetInt(item, "ID"),
                    GetInt(item, "id")
                );

                if (id == 0)
                {
                    continue;
                }

                var name = FirstNonEmpty(
                    GetString(item, "Name"),
                    GetString(item, "name"),
                    GetString(item, "Title"),
                    GetString(item, "title")
                );

                var key = FirstNonEmpty(
                    GetString(item, "Key"),
                    GetString(item, "key")
                );

                var category = FirstNonEmpty(
                    GetString(item, "Category"),
                    GetString(item, "category"),
                    GetString(item, "Type"),
                    GetString(item, "type")
                );

                result[id] = new UpgradeCatalogEntry(id, key, name, category);
            }
        }
        catch
        {
            // Best effort only.
        }
    }

    private static void TryLoadUpgradeCatalogFromSource(
        string file,
        Dictionary<int, UpgradeCatalogEntry> result)
    {
        try
        {
            var text = File.ReadAllText(file);

            if (!text.Contains("Upgrade", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Conservative fallback. This catches obvious initializer fragments like ID = 123.
            foreach (Match match in Regex.Matches(
                text,
                @"(?<name>[A-Za-z0-9_ ]{3,80})[\s\S]{0,180}?\bID\s*=\s*(?<id>\d+)",
                RegexOptions.IgnoreCase))
            {
                if (!int.TryParse(match.Groups["id"].Value, out var id) || id == 0)
                {
                    continue;
                }

                if (result.ContainsKey(id))
                {
                    continue;
                }

                var rawName = Regex.Replace(match.Groups["name"].Value, @"\s+", " ").Trim();

                result[id] = new UpgradeCatalogEntry(
                    Id: id,
                    Key: "",
                    Name: rawName,
                    Category: "SourceScan"
                );
            }
        }
        catch
        {
            // Best effort only.
        }
    }

    private static List<int> ReadIntArray(JsonElement root, string propertyName)
    {
        var result = new List<int>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number))
            {
                result.Add(number);
            }
            else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out number))
            {
                result.Add(number);
            }
        }

        return result;
    }

    private static string GuessAchievementNotes(string name, JsonElement value)
    {
        if (name.Equals("AchievementsSave", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely achievement category/row unlock save data. Needs AchievementManager source mapping.";
        }

        if (name.Equals("RAchieves", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely realm achievement save data. Needs AchievementManager source mapping.";
        }

        if (name.Equals("RecentlyAchieved", StringComparison.OrdinalIgnoreCase))
        {
            return "Recent achievement queue/history. Probably not formula-critical.";
        }

        if (name.Equals("AchievUnlocked", StringComparison.OrdinalIgnoreCase)
            || name.Equals("AchievsPoints", StringComparison.OrdinalIgnoreCase))
        {
            return "Achievement summary counter. May affect unlocks or rewards if source says so.";
        }

        return "Achievement-related save section. Needs source mapping.";
    }

    private static string GuessProgressNotes(string name)
    {
        return name switch
        {
            "Ascends" => "Total ascensions. Already useful progression context.",
            "AscendsRealm" => "Realm ascensions. Already useful progression context.",
            "BoughtUpgrades" => "Purchased upgrade count/statistic. Compare with Upgrades[] length.",
            "HeroMaxLevelAllTime" => "All-time max hero level, not current run character level.",
            "PetMaxLevel" => "Current max pet level statistic.",
            "PetMaxLevelAllTime" => "All-time max pet level statistic.",
            "ApprenticeMaxLevelRealm" => "Realm max apprentice level. Source-confirmed statistic field.",
            "ClassTime" => "Fixed-index class-time array mapped by HeroesNames enum.",
            "BuildingLevels" => "Fixed-index building level array mapped by building data order.",
            _ => "Progress/unlock-related save field."
        };
    }

    private static string BuildPreview(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => Shorten(value.GetString() ?? "", 140),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Array => "Array length " + value.GetArrayLength(),
            JsonValueKind.Object => LooksLikeBigNumber(value)
                ? ReadBigNumberScientific(value)
                : "Object properties: " + string.Join(", ", value.EnumerateObject().Select(x => x.Name).Take(12)),
            _ => Shorten(value.GetRawText(), 140)
        };
    }

    private static string PreviewValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => Shorten(value.GetString() ?? "", 80),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => "Array(" + value.GetArrayLength() + ")",
            JsonValueKind.Object => LooksLikeBigNumber(value)
                ? ReadBigNumberScientific(value)
                : "Object(" + value.EnumerateObject().Count() + ")",
            _ => Shorten(value.GetRawText(), 80)
        };
    }

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out _)
            && value.TryGetProperty("Exponent", out _);
    }

    private static string ReadBigNumberScientific(JsonElement value)
    {
        return GetDouble(value, "Mantissa") + "e" + GetInt(value, "Exponent");
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

    private static int FirstNonZero(params int[] values)
    {
        foreach (var value in values)
        {
            if (value != 0)
            {
                return value;
            }
        }

        return 0;
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

        return "";
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength
            ? value
            : value.Substring(0, maxLength) + "...";
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

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0.0;
    }

    private sealed record SaveUnlockMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        UpgradeUnlockSummary UpgradeSummary,
        IReadOnlyList<UpgradeUnlockEntry> PurchasedUpgrades,
        IReadOnlyList<SaveSectionShape> AchievementSections,
        IReadOnlyList<SaveSectionShape> ChallengeSections,
        IReadOnlyList<UnlockProgressEntry> Progress,
        IReadOnlyList<string> SourceMappingNotes
    );

    private sealed record UpgradeUnlockSummary(
        int PurchasedUpgradeCount,
        int CatalogMatchedCount,
        int CatalogUnmatchedCount,
        IReadOnlyList<int> FirstPurchasedUpgradeIds,
        IReadOnlyList<int> LastPurchasedUpgradeIds
    );

    private sealed record UpgradeUnlockEntry(
        int Id,
        string Name,
        string Key,
        string Category,
        bool CatalogMatched,
        string MappingStatus,
        string Notes
    );

    private sealed record UpgradeCatalogEntry(
        int Id,
        string Key,
        string Name,
        string Category
    );

    private sealed record SaveSectionShape(
        string Name,
        string JsonKind,
        int ArrayLength,
        int ObjectPropertyCount,
        IReadOnlyList<string> ObjectProperties,
        IReadOnlyList<string> FirstValues,
        IReadOnlyList<string> LastValues,
        string Preview,
        string Notes
    );

    private sealed record UnlockProgressEntry(
        string Name,
        string JsonKind,
        string Value,
        string Notes
    );
}
