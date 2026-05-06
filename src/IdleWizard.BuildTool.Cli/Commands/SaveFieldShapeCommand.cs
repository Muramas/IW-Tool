using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveFieldShapeCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --save-field-shape .\save_export.txt [output.json] [Field1 Field2 ...]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);

        var outputPath = ".\\save_field_shape.json";
        var fieldStartIndex = 2;

        if (args.Length >= 3 && args[2].EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            outputPath = args[2];
            fieldStartIndex = 3;
        }

        var requestedFields = args.Skip(fieldStartIndex).ToList();

        if (requestedFields.Count == 0)
        {
            requestedFields = new List<string>
            {
                "Upgrades",
                "Catalysts",
                "BuildingLevels",
                "ClassTime",
                "SpellShards",
                "OtherSpellShards",
                "SpellUses",
                "SpellUsesTR",
                "AccumCasts",
                "EDE",
                "ManaRealm",
                "ManaSession",
                "VoidManaRealm",
                "VoidManaSession",
                "ShardsPool",
                "Craft"
            };
        }

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine("Decoded save root is not a JSON object.");
            return;
        }

        var shapes = new List<FieldShape>();

        foreach (var field in requestedFields)
        {
            if (!root.TryGetProperty(field, out var value))
            {
                shapes.Add(
                    new FieldShape(
                        field,
                        "Missing",
                        0,
                        0,
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        "",
                        "Field not present in save."
                    )
                );

                continue;
            }

            shapes.Add(BuildShape(field, value));
        }

        var export = new SaveFieldShapeExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            requestedFields,
            shapes,
            new[]
            {
                "This is a developer mapping artifact, not live UI data.",
                "Array samples help identify whether values are IDs, counters, levels, or indexed enum data.",
                "Object property lists help identify nested save structures that need dedicated mappers."
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

        Console.WriteLine("Save field shape");
        Console.WriteLine("----------------");
        Console.WriteLine($"Save:   {savePath}");
        Console.WriteLine($"Output: {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Fields: {shapes.Count}");
        Console.WriteLine("");

        foreach (var shape in shapes)
        {
            Console.WriteLine($"{shape.Name}: {shape.JsonKind}");

            if (shape.ArrayLength > 0)
            {
                Console.WriteLine($"  Array length: {shape.ArrayLength}");
                Console.WriteLine($"  First: {string.Join(", ", shape.FirstValues)}");
                Console.WriteLine($"  Last:  {string.Join(", ", shape.LastValues)}");
            }

            if (shape.ObjectPropertyCount > 0)
            {
                Console.WriteLine($"  Properties: {string.Join(", ", shape.ObjectProperties)}");
            }

            if (!string.IsNullOrWhiteSpace(shape.BigNumberScientific))
            {
                Console.WriteLine($"  BigNumber: {shape.BigNumberScientific}");
            }

            Console.WriteLine($"  Notes: {shape.Notes}");
            Console.WriteLine("");
        }
    }

    private static FieldShape BuildShape(string name, JsonElement value)
    {
        var jsonKind = value.ValueKind.ToString();
        var arrayLength = 0;
        var objectPropertyCount = 0;
        var objectProperties = new List<string>();
        var firstValues = new List<string>();
        var lastValues = new List<string>();
        var bigNumberScientific = "";
        var notes = "";

        if (value.ValueKind == JsonValueKind.Array)
        {
            arrayLength = value.GetArrayLength();

            var values = value
                .EnumerateArray()
                .Select(PreviewValue)
                .ToList();

            firstValues = values.Take(12).ToList();
            lastValues = values.Skip(Math.Max(0, values.Count - 12)).ToList();

            notes = GuessArrayNotes(name, values);
        }
        else if (value.ValueKind == JsonValueKind.Object)
        {
            var props = value.EnumerateObject().ToList();
            objectPropertyCount = props.Count;
            objectProperties = props.Select(x => x.Name).Take(24).ToList();

            if (LooksLikeBigNumber(value))
            {
                var mantissa = GetDouble(value, "Mantissa");
                var exponent = GetInt(value, "Exponent");
                bigNumberScientific = mantissa + "e" + exponent;
                notes = "Looks like BigNumber { Mantissa, Exponent }.";
            }
            else
            {
                notes = GuessObjectNotes(name, props);
            }
        }
        else
        {
            firstValues.Add(PreviewValue(value));
            notes = "Scalar field.";
        }

        return new FieldShape(
            name,
            jsonKind,
            arrayLength,
            objectPropertyCount,
            objectProperties,
            firstValues,
            lastValues,
            bigNumberScientific,
            notes
        );
    }

    private static string GuessArrayNotes(string name, IReadOnlyList<string> values)
    {
        if (name.Equals("Upgrades", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely list of purchased upgrade IDs. Needs upgrade ID catalog mapping.";
        }

        if (name.Equals("BuildingLevels", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely fixed-index building levels. Needs building enum/order mapping.";
        }

        if (name.Equals("ClassTime", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely fixed-index class time array aligned to HeroesNames enum.";
        }

        if (name.Equals("SpellShards", StringComparison.OrdinalIgnoreCase)
            || name.Equals("OtherSpellShards", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely spell shard counts indexed by selected/current spell arrays. Needs spell index mapping.";
        }

        if (name.Equals("SpellUses", StringComparison.OrdinalIgnoreCase)
            || name.Equals("SpellUsesTR", StringComparison.OrdinalIgnoreCase))
        {
            return "Likely spell use counters. Needs index-to-spell mapping.";
        }

        if (values.Count > 0 && values.All(x => int.TryParse(x, out _)))
        {
            return "Integer array. Need source mapping to identify index meaning.";
        }

        return "Array shape captured. Needs source mapping.";
    }

    private static string GuessObjectNotes(string name, IReadOnlyList<JsonProperty> props)
    {
        if (name.Equals("Catalysts", StringComparison.OrdinalIgnoreCase))
        {
            return "Catalyst structure. Needs source mapping for Total/tA/fA/tM/fM/tR/fR fields.";
        }

        if (name.Equals("Craft", StringComparison.OrdinalIgnoreCase))
        {
            return "Crafting structure. Contains item inventory, craft skill exp, costs, and progression fields.";
        }

        if (props.Any(x => x.Name.Equals("Mantissa", StringComparison.OrdinalIgnoreCase))
            && props.Any(x => x.Name.Equals("Exponent", StringComparison.OrdinalIgnoreCase)))
        {
            return "Looks like BigNumber object.";
        }

        return "Object shape captured. Needs property-level mapping.";
    }

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        return value.TryGetProperty("Mantissa", out _)
            && value.TryGetProperty("Exponent", out _);
    }

    private static string PreviewValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => Shorten(value.GetString() ?? ""),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => "Array(" + value.GetArrayLength() + ")",
            JsonValueKind.Object => "Object(" + value.EnumerateObject().Count() + ")",
            _ => Shorten(value.GetRawText())
        };
    }

    private static string Shorten(string value)
    {
        return value.Length <= 80
            ? value
            : value.Substring(0, 80) + "...";
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

    private static int GetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
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

    private static double GetDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
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

    private sealed record SaveFieldShapeExport(
        string GeneratedAtUtc,
        string SaveFile,
        IReadOnlyList<string> RequestedFields,
        IReadOnlyList<FieldShape> Shapes,
        IReadOnlyList<string> Notes
    );

    private sealed record FieldShape(
        string Name,
        string JsonKind,
        int ArrayLength,
        int ObjectPropertyCount,
        IReadOnlyList<string> ObjectProperties,
        IReadOnlyList<string> FirstValues,
        IReadOnlyList<string> LastValues,
        string BigNumberScientific,
        string Notes
    );
}
