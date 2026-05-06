using System.Text.Json;

namespace IdleWizard.BuildTool.Core.Data;

public sealed class GameDataRepository
{
    public IReadOnlyList<GameDataRecord> LoadRecords(
        string workspacePath,
        string fileNameHint)
    {
        var rawRoot = Path.Combine(workspacePath, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            throw new DirectoryNotFoundException(
                $"Missing raw_files directory: {rawRoot}"
            );
        }

        var candidates = FindMatchingFiles(rawRoot, fileNameHint);

        if (candidates.Count == 0)
        {
            return Array.Empty<GameDataRecord>();
        }

        var file = ChooseBestFileMatch(candidates, fileNameHint);

        var relative = Path
            .GetRelativePath(rawRoot, file)
            .Replace("\\", "/");

        var text = File.ReadAllText(file);
        using var doc = JsonDocument.Parse(text);

        var root = doc.RootElement;
        var records = new List<GameDataRecord>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            var index = 0;

            foreach (var element in root.EnumerateArray())
            {
                records.Add(
                    new GameDataRecord(
                        relative,
                        index,
                        ExtractFields(element)
                    )
                );

                index++;
            }

            return records;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            var index = 0;

            foreach (var property in root.EnumerateObject())
            {
                records.Add(
                    new GameDataRecord(
                        relative,
                        index,
                        new Dictionary<string, string>
                        {
                            ["Key"] = property.Name,
                            ["Value"] = SummarizeJson(property.Value),
                        }
                    )
                );

                index++;
            }

            return records;
        }

        return records;
    }

    public IReadOnlyList<string> ListMatchingFiles(
        string workspacePath,
        string fileNameHint)
    {
        var rawRoot = Path.Combine(workspacePath, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            throw new DirectoryNotFoundException(
                $"Missing raw_files directory: {rawRoot}"
            );
        }

        return FindMatchingFiles(rawRoot, fileNameHint)
            .Select(
                path => Path
                    .GetRelativePath(rawRoot, path)
                    .Replace("\\", "/")
            )
            .OrderBy(path => path)
            .ToList();
    }

    private static List<string> FindMatchingFiles(
        string rawRoot,
        string fileNameHint)
    {
        return Directory
            .EnumerateFiles(rawRoot, "*", SearchOption.AllDirectories)
            .Where(
                path =>
                    (
                        (path.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".bytes.txt", StringComparison.OrdinalIgnoreCase))
                        || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    )
                    && Path
                        .GetFileName(path)
                        .Contains(fileNameHint, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(path => path)
            .ToList();
    }

    private static string ChooseBestFileMatch(
        IReadOnlyList<string> candidates,
        string fileNameHint)
    {
        // Prefer exact logical data file names.
        // Examples:
        //   Items            -> Items.bytes.txt
        //   Spells           -> Spells.bytes.txt
        //   Upgrades         -> Upgrades.bytes.txt
        //   RealmUpgrades    -> RealmUpgrades.bytes.txt
        var exactBytesName = fileNameHint + ".bytes.txt";
        var exactRawBytesName = fileNameHint + ".bytes";
        var exactJsonName = fileNameHint + ".json";

        var exact = candidates.FirstOrDefault(
            path =>
                Path.GetFileName(path).Equals(
                    exactBytesName,
                    StringComparison.OrdinalIgnoreCase
                )
                || Path.GetFileName(path).Equals(
                    exactRawBytesName,
                    StringComparison.OrdinalIgnoreCase
                )
                || Path.GetFileName(path).Equals(
                    exactJsonName,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        if (exact is not null)
        {
            return exact;
        }

        return candidates[0];
    }

    private static IReadOnlyDictionary<string, string> ExtractFields(
        JsonElement element)
    {
        var fields = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        if (element.ValueKind != JsonValueKind.Object)
        {
            fields["_value"] = SummarizeJson(element);
            return fields;
        }

        foreach (var property in element.EnumerateObject())
        {
            fields[property.Name] = SummarizeJson(property.Value);
        }

        return fields;
    }

    private static string SummarizeJson(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Array => SummarizeContainer(element),
            JsonValueKind.Object => SummarizeContainer(element),
            _ => element.GetRawText(),
        };
    }

    private static string SummarizeContainer(JsonElement element)
    {
        var raw = element.GetRawText();

        if (raw.Length <= 240)
        {
            return raw;
        }

        return raw[..240] + "...";
    }
}
