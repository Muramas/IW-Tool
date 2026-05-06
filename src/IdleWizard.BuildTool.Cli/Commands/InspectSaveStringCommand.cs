using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class InspectSaveStringCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --inspect-save-string .\save_export.txt [save_inspect.json]");
            return;
        }

        var inputPath = args[1];
        var outputPath = args.Length >= 3
            ? args[2]
            : ".\\save_inspect.json";

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Save string file not found: {inputPath}");
            return;
        }

        var rawInput = File.ReadAllText(inputPath);
        var cleaned = Regex.Replace(rawInput, @"\s+", "");

        var base64Decoded = TryBase64Decode(cleaned, out var decodedBytes, out var base64Error);
        var gzipDecoded = false;
        var gzipError = "";
        byte[] decompressedBytes = Array.Empty<byte>();

        if (base64Decoded)
        {
            gzipDecoded = TryGzipDecompress(decodedBytes, out decompressedBytes, out gzipError);
        }

        var payloadBytes = gzipDecoded
            ? decompressedBytes
            : base64Decoded
                ? decodedBytes
                : Array.Empty<byte>();

        var utf8Text = TryDecodeUtf8(payloadBytes, out var textPreview);
        var jsonDetected = false;
        string rootKind = "";
        List<string> topLevelProperties = new();

        if (utf8Text)
        {
            jsonDetected = TryInspectJson(textPreview.FullText, out rootKind, out topLevelProperties);
        }

        var printableStrings = ExtractPrintableStrings(payloadBytes)
            .Take(200)
            .ToList();

        var export = new SaveInspectionResult(
            DateTime.UtcNow.ToString("O"),
            Path.GetFullPath(inputPath),
            rawInput.Length,
            cleaned.Length,
            LooksLikeGzipBase64(cleaned),
            base64Decoded,
            base64Error,
            base64Decoded ? decodedBytes.Length : 0,
            gzipDecoded,
            gzipError,
            gzipDecoded ? decompressedBytes.Length : 0,
            utf8Text,
            textPreview.Preview,
            jsonDetected,
            rootKind,
            topLevelProperties,
            printableStrings,
            new[]
            {
                "This inspection intentionally does not write the full decoded save.",
                "If Base64 and GZip succeed, the save likely uses Base64-encoded GZip data.",
                "If JSON is not detected, next step is to inspect the game source for save serialization/deserialization."
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

        Console.WriteLine("Save string inspection");
        Console.WriteLine("----------------------");
        Console.WriteLine($"Input:             {Path.GetFullPath(inputPath)}");
        Console.WriteLine($"Output:            {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Input chars:       {rawInput.Length}");
        Console.WriteLine($"Cleaned chars:     {cleaned.Length}");
        Console.WriteLine($"Looks H4sI/GZip:   {LooksLikeGzipBase64(cleaned)}");
        Console.WriteLine($"Base64 decoded:    {base64Decoded}");
        Console.WriteLine($"Base64 bytes:      {(base64Decoded ? decodedBytes.Length : 0)}");
        Console.WriteLine($"GZip decoded:      {gzipDecoded}");
        Console.WriteLine($"Decompressed bytes:{(gzipDecoded ? decompressedBytes.Length : 0)}");
        Console.WriteLine($"UTF8 text:         {utf8Text}");
        Console.WriteLine($"JSON detected:     {jsonDetected}");
        Console.WriteLine($"JSON root:         {rootKind}");
        Console.WriteLine($"Printable strings: {printableStrings.Count}");
    }

    private static bool LooksLikeGzipBase64(string value)
    {
        return value.StartsWith("H4sI", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBase64Decode(string value, out byte[] bytes, out string error)
    {
        bytes = Array.Empty<byte>();
        error = "";

        try
        {
            bytes = Convert.FromBase64String(value);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryGzipDecompress(byte[] input, out byte[] output, out string error)
    {
        output = Array.Empty<byte>();
        error = "";

        try
        {
            using var inputStream = new MemoryStream(input);
            using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            gzip.CopyTo(outputStream);
            output = outputStream.ToArray();
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool TryDecodeUtf8(byte[] bytes, out TextInspection text)
    {
        text = new TextInspection("", "");

        if (bytes.Length == 0)
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(bytes);
            var preview = decoded.Length <= 4000
                ? decoded
                : decoded.Substring(0, 4000);

            text = new TextInspection(decoded, preview);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryInspectJson(
        string text,
        out string rootKind,
        out List<string> topLevelProperties)
    {
        rootKind = "";
        topLevelProperties = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(text);
            rootKind = doc.RootElement.ValueKind.ToString();

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in doc.RootElement.EnumerateObject())
                {
                    topLevelProperties.Add(property.Name);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> ExtractPrintableStrings(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            yield break;
        }

        var builder = new StringBuilder();

        foreach (var b in bytes)
        {
            var c = (char)b;

            if (c >= 32 && c <= 126)
            {
                builder.Append(c);
            }
            else
            {
                if (builder.Length >= 4)
                {
                    yield return builder.ToString();
                }

                builder.Clear();
            }
        }

        if (builder.Length >= 4)
        {
            yield return builder.ToString();
        }
    }

    private sealed record TextInspection(
        string FullText,
        string Preview
    );

    private sealed record SaveInspectionResult(
        string GeneratedAtUtc,
        string InputPath,
        int RawInputLength,
        int CleanedInputLength,
        bool LooksLikeGzipBase64,
        bool Base64Decoded,
        string Base64Error,
        int Base64DecodedByteLength,
        bool GzipDecoded,
        string GzipError,
        int GzipDecodedByteLength,
        bool Utf8TextDetected,
        string Utf8Preview,
        bool JsonDetected,
        string JsonRootKind,
        IReadOnlyList<string> TopLevelJsonProperties,
        IReadOnlyList<string> PrintableStrings,
        IReadOnlyList<string> Warnings
    );
}
