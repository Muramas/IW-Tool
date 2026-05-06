using System.Text.Json;

namespace IdleWizard.BuildTool.Core.Data;

public sealed class RawGameDataLoader
{
    public IReadOnlyList<RawDataFileSummary> SummarizeWorkspace(string workspacePath)
    {
        var workspace = new DirectoryInfo(workspacePath);
        var rawFiles = new DirectoryInfo(Path.Combine(workspace.FullName, "raw_files"));

        if (!rawFiles.Exists)
        {
            throw new DirectoryNotFoundException(
                $"Missing raw_files directory: {rawFiles.FullName}"
            );
        }

        var files = rawFiles
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(
                file =>
                    (file.Name.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase) || file.Name.EndsWith(".bytes.txt", StringComparison.OrdinalIgnoreCase))
                    || file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(file => file.FullName)
            .ToList();

        var summaries = new List<RawDataFileSummary>();

        foreach (var file in files)
        {
            summaries.Add(SummarizeFile(rawFiles.FullName, file));
        }

        return summaries;
    }

    private static RawDataFileSummary SummarizeFile(
        string rawRoot,
        FileInfo file)
    {
        var relative = Path
            .GetRelativePath(rawRoot, file.FullName)
            .Replace("\\", "/");

        var text = File.ReadAllText(file.FullName);
        var trimmed = text.TrimStart();

        if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
        {
            return new RawDataFileSummary(
                relative,
                file.Length,
                false,
                "not_json",
                "$",
                0,
                Array.Empty<string>(),
                "File does not start with JSON object or array."
            );
        }

        try
        {
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;

            var rootType = root.ValueKind.ToString();
            var recordPath = "$";
            var recordCount = 0;
            var fields = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            if (root.ValueKind == JsonValueKind.Array)
            {
                recordPath = "$";
                recordCount = root.GetArrayLength();
                AddFieldsFromArray(root, fields);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var foundArray = false;

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        recordPath = "$." + property.Name;
                        recordCount = property.Value.GetArrayLength();
                        AddFieldsFromArray(property.Value, fields);
                        foundArray = true;
                        break;
                    }
                }

                if (!foundArray)
                {
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        foreach (var nested in property.Value.EnumerateObject())
                        {
                            if (nested.Value.ValueKind == JsonValueKind.Array)
                            {
                                recordPath = "$." + property.Name + "." + nested.Name;
                                recordCount = nested.Value.GetArrayLength();
                                AddFieldsFromArray(nested.Value, fields);
                                foundArray = true;
                                break;
                            }
                        }

                        if (foundArray)
                        {
                            break;
                        }
                    }
                }

                if (!foundArray)
                {
                    recordPath = "$";
                    recordCount = root.EnumerateObject().Count();

                    foreach (var property in root.EnumerateObject())
                    {
                        fields.Add(property.Name);
                    }
                }
            }

            return new RawDataFileSummary(
                relative,
                file.Length,
                true,
                rootType,
                recordPath,
                recordCount,
                fields.ToList(),
                null
            );
        }
        catch (Exception ex)
        {
            return new RawDataFileSummary(
                relative,
                file.Length,
                false,
                "parse_error",
                "$",
                0,
                Array.Empty<string>(),
                ex.Message
            );
        }
    }

    private static void AddFieldsFromArray(
        JsonElement array,
        SortedSet<string> fields)
    {
        var inspected = 0;

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in item.EnumerateObject())
                {
                    fields.Add(property.Name);
                }
            }

            inspected++;

            if (inspected >= 25)
            {
                break;
            }
        }
    }
}

