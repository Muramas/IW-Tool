using System.Text.Json;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ScoreSlotWeightedFileCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --score-slot-weighted-file .\scenario.json");
            return;
        }

        var scenarioPath = args[1];

        if (!File.Exists(scenarioPath))
        {
            Console.WriteLine($"Scenario file not found: {scenarioPath}");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = doc.RootElement;

        var generatedArgs = new List<string>
        {
            "--score-slot-weighted",
            GetRequiredString(root, "workspace"),
            GetRequiredString(root, "slot"),
        };

        if (root.TryGetProperty("objectives", out var objectives)
            && objectives.ValueKind == JsonValueKind.Object)
        {
            foreach (var objective in objectives.EnumerateObject())
            {
                generatedArgs.Add(
                    objective.Name + "@" + GetJsonScalarAsString(objective.Value)
                );
            }
        }
        else
        {
            Console.WriteLine("Scenario file missing required object: objectives");
            return;
        }

        if (root.TryGetProperty("currentValues", out var currentValues)
            && currentValues.ValueKind == JsonValueKind.Object)
        {
            foreach (var value in currentValues.EnumerateObject())
            {
                generatedArgs.Add(
                    value.Name + "=" + GetJsonScalarAsString(value.Value)
                );
            }
        }

        if (TryGetString(root, "efficiency", out var efficiency))
        {
            generatedArgs.Add("--efficiency=" + efficiency);
        }

        if (TryGetString(root, "gilding", out var gilding))
        {
            generatedArgs.Add("--gilding=" + gilding);
        }

        if (TryGetString(root, "tier", out var tier))
        {
            generatedArgs.Add("--tier=" + tier);
        }

        if (TryGetString(root, "top", out var top))
        {
            generatedArgs.Add("--top=" + top);
        }

        if (TryGetString(root, "equipped", out var equipped))
        {
            generatedArgs.Add("--equipped=" + equipped);
        }

        if (TryGetString(root, "outputJson", out var outputJson))
        {
            generatedArgs.Add("--json-out=" + outputJson);
        }

        Console.WriteLine($"Scenario: {scenarioPath}");
        Console.WriteLine("Expanded command:");
        Console.WriteLine("  " + string.Join(" ", generatedArgs.Select(QuoteIfNeeded)));
        Console.WriteLine("");

        ScoreSlotWeightedCommand.Run(generatedArgs.ToArray());
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        if (!TryGetString(root, propertyName, out var value))
        {
            throw new InvalidOperationException(
                $"Scenario file missing required property: {propertyName}"
            );
        }

        return value;
    }

    private static bool TryGetString(
        JsonElement root,
        string propertyName,
        out string value)
    {
        value = "";

        if (!root.TryGetProperty(propertyName, out var element))
        {
            return false;
        }

        value = GetJsonScalarAsString(element);
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string GetJsonScalarAsString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => element.GetRawText()
        };
    }

    private static string QuoteIfNeeded(string value)
    {
        if (value.Contains(' ') || value.Contains('"'))
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        return value;
    }
}