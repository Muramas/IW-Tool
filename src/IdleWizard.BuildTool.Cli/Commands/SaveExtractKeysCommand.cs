using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveExtractKeysCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --save-extract-keys .\save_export.txt .\save_extract.json Key1 Key2 Key3");
            return;
        }

        var savePath = args[1];
        var outputPath = args[2];
        var requestedKeys = args.Skip(3).ToList();

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var extracted = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        foreach (var key in requestedKeys)
        {
            if (root.TryGetProperty(key, out var value))
            {
                extracted[key] = value.Clone();
            }
            else
            {
                missing.Add(key);
            }
        }

        var export = new
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            SaveFile = Path.GetFullPath(savePath),
            RequestedKeys = requestedKeys,
            MissingKeys = missing,
            Extracted = extracted,
            Notes = new[]
            {
                "This file contains only explicitly requested save sections.",
                "Use this to inspect equipment, item presets, craft data, spellbar data, and other import mappings.",
                "Do not paste full save exports publicly."
            }
        };

        var json = JsonSerializer.Serialize(
            export,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save extract keys");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Save:       {Path.GetFullPath(savePath)}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Extracted:  {extracted.Count}");
        Console.WriteLine($"Missing:    {missing.Count}");

        if (missing.Count > 0)
        {
            Console.WriteLine("Missing keys:");
            foreach (var key in missing)
            {
                Console.WriteLine($"  {key}");
            }
        }
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
}
