using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveCatalystMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-catalyst-map .\save_export.txt .\iw_workspace_vNext [save_catalyst_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_catalyst_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var buildingNames = LoadBuildingNames(workspacePath);

        if (!root.TryGetProperty("Catalysts", out var catalysts)
            || catalysts.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine("Save does not contain Catalysts object.");
            return;
        }

        var totals = new CatalystTotals(
            TotalCatalysts: ReadBigNumberScientific(catalysts, "Total"),
            TotalGreenCatalysts: ReadBigNumberScientific(catalysts, "tA"),
            FreeGreenCatalysts: ReadBigNumberScientific(catalysts, "fA"),
            TotalBlueCatalysts: ReadBigNumberScientific(catalysts, "tM"),
            FreeBlueCatalysts: ReadBigNumberScientific(catalysts, "fM"),
            TotalRedCatalysts: ReadBigNumberScientific(catalysts, "tR"),
            FreeRedCatalysts: ReadBigNumberScientific(catalysts, "fR"),
            CatalystTradeIsAll: GetBool(catalysts, "IsAll")
        );

        var assignments = new List<CatalystAssignment>();

        if (catalysts.TryGetProperty("Catalysts", out var assigned)
            && assigned.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in assigned.EnumerateArray())
            {
                var tier = GetInt(record, "t");
                var buildingName = ResolveBuildingName(tier, buildingNames);

                assignments.Add(
                    new CatalystAssignment(
                        Tier: tier,
                        BuildingName: buildingName,
                        GreenCatalysts: GetUlongString(record, "a"),
                        BlueCatalysts: GetUlongString(record, "m"),
                        RedCatalysts: GetUlongString(record, "r"),
                        GreenFormulaInput: "ACatalyst",
                        BlueFormulaInput: "MCatalyst",
                        RedFormulaInput: "RCatalyst"
                    )
                );
            }
        }

        var export = new SaveCatalystMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            Totals: totals,
            Assignments: assignments.OrderBy(x => x.Tier).ToList(),
            FormulaNotes: new[]
            {
                "Source-confirmed from BuildingManager.SaveCatalysts and BuildingManager.LoadCatalysts.",
                "Catalysts.Catalysts[].t is building tier.",
                "Catalysts.Catalysts[].a is assigned green catalysts and loads into Building.ACatalyst.",
                "Catalysts.Catalysts[].m is assigned blue catalysts and loads into Building.MCatalyst.",
                "Catalysts.Catalysts[].r is assigned red catalysts and loads into Building.RCatalyst.",
                "GetGreenIncome(tier) uses (ACatalyst + 1) * GreenIncome * AllIncome * AllIncomeOverCap.",
                "GetBlueIncome(tier) uses (MCatalyst + 1) * BlueIncome * AllIncome * AllIncomeOverCap.",
                "GetRedIncome(tier) uses (RCatalyst + 1) * RedIncome * AllIncome * AllIncomeOverCap.",
                "Exact meaning of GreenIncome/BlueIncome/RedIncome/AllIncome modifiers still needs modifier-source mapping."
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

        Console.WriteLine("Save catalyst map");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Save:        {savePath}");
        Console.WriteLine($"Workspace:   {workspacePath}");
        Console.WriteLine($"Output:      {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Assignments: {assignments.Count}");
        Console.WriteLine("");
        Console.WriteLine("Totals:");
        Console.WriteLine($"  Total catalysts: {totals.TotalCatalysts}");
        Console.WriteLine($"  Green: total={totals.TotalGreenCatalysts}, free={totals.FreeGreenCatalysts}");
        Console.WriteLine($"  Blue:  total={totals.TotalBlueCatalysts}, free={totals.FreeBlueCatalysts}");
        Console.WriteLine($"  Red:   total={totals.TotalRedCatalysts}, free={totals.FreeRedCatalysts}");
        Console.WriteLine("");
        Console.WriteLine("Assignments:");

        foreach (var assignment in assignments.OrderBy(x => x.Tier))
        {
            Console.WriteLine(
                $"  Tier {assignment.Tier} {assignment.BuildingName}: " +
                $"G={assignment.GreenCatalysts}, B={assignment.BlueCatalysts}, R={assignment.RedCatalysts}"
            );
        }
    }

    private static string ResolveBuildingName(int tier, IReadOnlyList<string> buildingNames)
    {
        var index = tier - 1;

        if (index >= 0 && index < buildingNames.Count)
        {
            return buildingNames[index];
        }

        return "Building Tier " + tier;
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
                result.Add(
                    FirstNonEmpty(
                        GetString(building, "Name"),
                        GetString(building, "Key"),
                        GetString(building, "ID"),
                        "Building " + result.Count
                    )
                );
            }
        }
        catch
        {
            return new List<string>();
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

    private static string ReadBigNumberScientific(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out var mantissa)
            && value.TryGetProperty("Exponent", out var exponent))
        {
            return ReadScalar(mantissa) + "e" + ReadScalar(exponent);
        }

        return ReadScalar(value);
    }

    private static string GetUlongString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "0";
        }

        return ReadScalar(value);
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

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return ReadScalar(value);
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
            _ => value.GetRawText()
        };
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

    private sealed record SaveCatalystMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        CatalystTotals Totals,
        IReadOnlyList<CatalystAssignment> Assignments,
        IReadOnlyList<string> FormulaNotes
    );

    private sealed record CatalystTotals(
        string TotalCatalysts,
        string TotalGreenCatalysts,
        string FreeGreenCatalysts,
        string TotalBlueCatalysts,
        string FreeBlueCatalysts,
        string TotalRedCatalysts,
        string FreeRedCatalysts,
        bool CatalystTradeIsAll
    );

    private sealed record CatalystAssignment(
        int Tier,
        string BuildingName,
        string GreenCatalysts,
        string BlueCatalysts,
        string RedCatalysts,
        string GreenFormulaInput,
        string BlueFormulaInput,
        string RedFormulaInput
    );
}
