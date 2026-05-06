using System;
using System.IO;
using System.Text.Json;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    private void LoadPetValidationIfExists(string scenarioPath)
    {
        try
        {
            var validationPath = GetPetValidationOutputPathFromScenario(scenarioPath);

            if (string.IsNullOrWhiteSpace(validationPath) || !File.Exists(validationPath))
            {
                return;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(validationPath));
            var root = doc.RootElement;

            if (!root.TryGetProperty("Phases", out var phases)
                && !root.TryGetProperty("phases", out phases))
            {
                return;
            }

            if (phases.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var phase in phases.EnumerateArray())
            {
                var phaseName = GetPetFlexibleString(phase, "PhaseName", "phaseName");
                var name = GetPetFlexibleString(phase, "Name", "name");
                var configuredPet = GetPetFlexibleString(phase, "ConfiguredPet", "configuredPet");
                var rawPetLevel = GetPetFlexibleString(phase, "RawPetLevel", "rawPetLevel");
                var found = GetPetFlexibleString(phase, "Found", "found");
                var levelWithinMax = GetPetFlexibleString(phase, "LevelWithinMax", "levelWithinMax");
                var warning = GetPetFlexibleString(phase, "Warning", "warning");

                var displayName = string.IsNullOrWhiteSpace(name) ? configuredPet : name;
                var valid =
                    found.Equals("True", StringComparison.OrdinalIgnoreCase)
                    && levelWithinMax.Equals("True", StringComparison.OrdinalIgnoreCase)
                    && string.IsNullOrWhiteSpace(warning);

                var prefix = valid ? "[Valid]" : "[Invalid]";
                var text = $"{prefix} Pet: {displayName}  Lv {rawPetLevel}";

                if (!string.IsNullOrWhiteSpace(warning))
                {
                    text += $"  - {warning}";
                }

                if (phaseName.Contains("Burst", StringComparison.OrdinalIgnoreCase))
                {
                    BurstPetDisplay = text;
                }
                else if (phaseName.Contains("Void", StringComparison.OrdinalIgnoreCase))
                {
                    VoidPetDisplay = text;
                }
            }

            DataContext = null;
            DataContext = this;
        }
        catch (Exception ex)
        {
            AppendLog("Could not load pet validation JSON:");
            AppendLog(ex.Message);
        }
    }

    private static string GetPetValidationOutputPathFromScenario(string scenarioPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = doc.RootElement;

        if (root.TryGetProperty("petValidationOutputJson", out var output)
            || root.TryGetProperty("PetValidationOutputJson", out output))
        {
            if (output.ValueKind == JsonValueKind.String)
            {
                var raw = output.GetString();

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    return ResolvePathRelativeToFile(scenarioPath, raw);
                }
            }
        }

        var scenarioDir = Path.GetDirectoryName(Path.GetFullPath(scenarioPath)) ?? Environment.CurrentDirectory;
        return Path.Combine(scenarioDir, "pet_validation_head_hero.json");
    }

    private static string GetPetFlexibleString(JsonElement element, string pascalName, string camelName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        if (!element.TryGetProperty(pascalName, out var value)
            && !element.TryGetProperty(camelName, out value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "True",
            JsonValueKind.False => "False",
            JsonValueKind.Null => "",
            _ => value.GetRawText()
        };
    }
}
