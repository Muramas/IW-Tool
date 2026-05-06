using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveProgressMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-progress-map .\save_export.txt .\iw_workspace_vNext [save_progress_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_progress_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var heroEnum = LoadEnumMap(workspacePath, "HeroesNames.cs", "HeroesNames");
        var buildingNames = LoadBuildingNames(workspacePath);

        var classTimes = MapClassTime(root, heroEnum);
        var buildings = MapBuildingLevels(root, buildingNames);
        var catalysts = MapCatalysts(root);
        var resources = MapResources(root);
        var progress = MapProgress(root);

        var export = new SaveProgressMapExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            classTimes,
            buildings,
            catalysts,
            resources,
            progress,
            new[]
            {
                "ClassTime is mapped by HeroesNames enum index.",
                "BuildingLevels is mapped by array position. Building names are best-effort from Buildings data if available.",
                "Catalyst save semantics are source-confirmed from BuildingManager.SaveCatalysts/LoadCatalysts. Formula effects still need CalculateCatalysts/source mapping.",
                "This file is developer/calculation context, not live UI content."
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

        Console.WriteLine("Save progress map");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Save:       {savePath}");
        Console.WriteLine($"Workspace:  {workspacePath}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Classes:    {classTimes.Count}");
        Console.WriteLine($"Buildings:  {buildings.Count}");
        Console.WriteLine($"Catalysts:  {catalysts.Count}");
        Console.WriteLine($"Resources:  {resources.Count}");
        Console.WriteLine("");

        Console.WriteLine("Top class times:");
        foreach (var row in classTimes.OrderByDescending(x => x.Seconds).Take(8))
        {
            Console.WriteLine($"  {row.ClassName}: {row.Seconds} sec");
        }

        Console.WriteLine("");
        Console.WriteLine("Building levels:");
        foreach (var row in buildings)
        {
            Console.WriteLine($"  [{row.Index}] {row.Name}: {row.Level}");
        }
    }

    private static List<ClassTimeMapEntry> MapClassTime(
        JsonElement root,
        Dictionary<int, string> heroEnum)
    {
        var result = new List<ClassTimeMapEntry>();

        if (!root.TryGetProperty("ClassTime", out var classTime)
            || classTime.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var index = 0;

        foreach (var item in classTime.EnumerateArray())
        {
            var seconds = ReadIntElement(item);
            var className = heroEnum.TryGetValue(index, out var name)
                ? name
                : "Class " + index;

            result.Add(
                new ClassTimeMapEntry(
                    index,
                    className,
                    seconds,
                    FormatDuration(seconds)
                )
            );

            index++;
        }

        return result;
    }

    private static List<BuildingLevelMapEntry> MapBuildingLevels(
        JsonElement root,
        IReadOnlyList<string> buildingNames)
    {
        var result = new List<BuildingLevelMapEntry>();

        if (!root.TryGetProperty("BuildingLevels", out var buildingLevels)
            || buildingLevels.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var index = 0;

        foreach (var item in buildingLevels.EnumerateArray())
        {
            var level = ReadIntElement(item);
            var name = index < buildingNames.Count
                ? buildingNames[index]
                : "Building " + index;

            result.Add(
                new BuildingLevelMapEntry(
                    index,
                    name,
                    level
                )
            );

            index++;
        }

        return result;
    }

    private static List<CatalystMapEntry> MapCatalysts(JsonElement root)
    {
        var result = new List<CatalystMapEntry>();

        if (!root.TryGetProperty("Catalysts", out var catalysts)
            || catalysts.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        // Source-confirmed from BuildingManager.SaveCatalysts / LoadCatalysts:
        // tA/fA = TotalGreen / FreeGreen
        // tM/fM = TotalBlue / FreeBlue
        // tR/fR = TotalRed / FreeRed
        // Catalysts[].t = building tier
        // Catalysts[].a = green catalysts assigned
        // Catalysts[].m = blue catalysts assigned
        // Catalysts[].r = red catalysts assigned

        AddCatalystAggregate(result, catalysts, "Total", "TotalCatalysts", "Total catalyst amount.");
        AddCatalystAggregate(result, catalysts, "tA", "TotalGreenCatalysts", "Source-confirmed: BuildingManager.TotalGreen.");
        AddCatalystAggregate(result, catalysts, "fA", "FreeGreenCatalysts", "Source-confirmed: BuildingManager.FreeGreenCatalysts.");
        AddCatalystAggregate(result, catalysts, "tM", "TotalBlueCatalysts", "Source-confirmed: BuildingManager.TotalBlue.");
        AddCatalystAggregate(result, catalysts, "fM", "FreeBlueCatalysts", "Source-confirmed: BuildingManager.FreeBlueCatalysts.");
        AddCatalystAggregate(result, catalysts, "tR", "TotalRedCatalysts", "Source-confirmed: BuildingManager.TotalRed.");
        AddCatalystAggregate(result, catalysts, "fR", "FreeRedCatalysts", "Source-confirmed: BuildingManager.FreeRedCatalysts.");

        if (catalysts.TryGetProperty("IsAll", out var isAll))
        {
            result.Add(
                new CatalystMapEntry(
                    "CatalystTradeIsAll",
                    ReadScalar(isAll),
                    "Boolean"
                )
            );
        }

        if (catalysts.TryGetProperty("Catalysts", out var assigned)
            && assigned.ValueKind == JsonValueKind.Array)
        {
            result.Add(
                new CatalystMapEntry(
                    "AssignedCatalystTierRecords",
                    assigned.GetArrayLength().ToString(),
                    "Count"
                )
            );

            foreach (var record in assigned.EnumerateArray())
            {
                var tier = GetInt(record, "t");
                var green = ReadCatalystUnsigned(record, "a");
                var blue = ReadCatalystUnsigned(record, "m");
                var red = ReadCatalystUnsigned(record, "r");

                result.Add(
                    new CatalystMapEntry(
                        "Tier " + tier + " GreenCatalysts",
                        green,
                        "AssignedCatalysts"
                    )
                );

                result.Add(
                    new CatalystMapEntry(
                        "Tier " + tier + " BlueCatalysts",
                        blue,
                        "AssignedCatalysts"
                    )
                );

                result.Add(
                    new CatalystMapEntry(
                        "Tier " + tier + " RedCatalysts",
                        red,
                        "AssignedCatalysts"
                    )
                );
            }
        }

        return result;
    }

    private static void AddCatalystAggregate(
        List<CatalystMapEntry> result,
        JsonElement catalysts,
        string saveField,
        string displayName,
        string note)
    {
        if (!catalysts.TryGetProperty(saveField, out var value))
        {
            return;
        }

        var mappedValue = value.ValueKind == JsonValueKind.Object && LooksLikeBigNumber(value)
            ? ReadBigNumberScientific(value)
            : ReadScalar(value);

        result.Add(
            new CatalystMapEntry(
                displayName,
                mappedValue,
                note
            )
        );
    }

    private static string ReadCatalystUnsigned(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty(propertyName, out var value))
        {
            return "0";
        }

        return ReadScalar(value);
    }
    private static List<ResourceMapEntry> MapResources(JsonElement root)
    {
        var names = new[]
        {
            "Mana",
            "ManaAllTime",
            "ManaRealm",
            "ManaSession",
            "VMana",
            "VoidManaAllTime",
            "VoidManaRealm",
            "VoidManaSession",
            "ShardsPool",
            "EDE",
            "AccumCasts"
        };

        var result = new List<ResourceMapEntry>();

        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Object && LooksLikeBigNumber(value))
            {
                result.Add(
                    new ResourceMapEntry(
                        name,
                        ReadBigNumberScientific(value),
                        GetInt(value, "Exponent"),
                        "BigNumber"
                    )
                );
            }
            else
            {
                result.Add(
                    new ResourceMapEntry(
                        name,
                        ReadScalar(value),
                        0,
                        value.ValueKind.ToString()
                    )
                );
            }
        }

        return result;
    }

    private static List<ProgressMapEntry> MapProgress(JsonElement root)
    {
        var names = new[]
        {
            "Ascends",
            "AscendsRealm",
            "BoughtUpgrades",
            "PetMaxLevel",
            "PetMaxLevelAllTime",
            "HeroMaxLevelAllTime",
            "ApprenticeMaxLevelRealm",
            "TotalBuildings"
        };

        var result = new List<ProgressMapEntry>();

        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            result.Add(
                new ProgressMapEntry(
                    name,
                    ReadScalar(value)
                )
            );
        }

        return result;
    }

    private static List<string> LoadBuildingNames(string workspacePath)
    {
        var result = new List<string>();

        var file = WorkspacePaths.FindDataFile(workspacePath, "Buildings");

        if (file is null)
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var building in doc.RootElement.EnumerateArray())
            {
                var name = FirstNonEmpty(
                    GetString(building, "Name"),
                    GetString(building, "Key"),
                    GetString(building, "ID"),
                    "Building " + result.Count
                );

                result.Add(name);
            }
        }
        catch
        {
            return new List<string>();
        }

        return result;
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

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

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

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out _)
            && value.TryGetProperty("Exponent", out _);
    }

    private static string ReadBigNumberScientific(JsonElement value)
    {
        var mantissa = GetDouble(value, "Mantissa");
        var exponent = GetInt(value, "Exponent");

        return mantissa + "e" + exponent;
    }

    private static string ReadScalar(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Object => LooksLikeBigNumber(value) ? ReadBigNumberScientific(value) : value.GetRawText(),
            JsonValueKind.Array => "Array length " + value.GetArrayLength(),
            _ => value.GetRawText()
        };
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

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return ReadScalar(value);
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return ReadIntElement(value);
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

    private static string FormatDuration(int seconds)
    {
        if (seconds <= 0)
        {
            return "0s";
        }

        var span = TimeSpan.FromSeconds(seconds);

        if (span.TotalDays >= 1)
        {
            return ((int)span.TotalDays) + "d " + span.Hours + "h " + span.Minutes + "m";
        }

        if (span.TotalHours >= 1)
        {
            return ((int)span.TotalHours) + "h " + span.Minutes + "m";
        }

        if (span.TotalMinutes >= 1)
        {
            return ((int)span.TotalMinutes) + "m " + span.Seconds + "s";
        }

        return seconds + "s";
    }

    private sealed record SaveProgressMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        IReadOnlyList<ClassTimeMapEntry> ClassTimes,
        IReadOnlyList<BuildingLevelMapEntry> BuildingLevels,
        IReadOnlyList<CatalystMapEntry> Catalysts,
        IReadOnlyList<ResourceMapEntry> Resources,
        IReadOnlyList<ProgressMapEntry> Progress,
        IReadOnlyList<string> Notes
    );

    private sealed record ClassTimeMapEntry(
        int Index,
        string ClassName,
        int Seconds,
        string Display
    );

    private sealed record BuildingLevelMapEntry(
        int Index,
        string Name,
        int Level
    );

    private sealed record CatalystMapEntry(
        string Name,
        string Value,
        string ValueKind
    );

    private sealed record ResourceMapEntry(
        string Name,
        string Scientific,
        int Exponent,
        string ValueKind
    );

    private sealed record ProgressMapEntry(
        string Name,
        string Value
    );
}

