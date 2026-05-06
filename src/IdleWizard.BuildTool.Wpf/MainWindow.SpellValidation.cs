using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    private void LoadSpellValidationIfExists(string scenarioPath)
    {
        try
        {
            var validationPath = GetSpellValidationOutputPathFromScenario(scenarioPath);

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
                var phaseName = GetFlexibleString(phase, "PhaseName", "phaseName");

                if (!phase.TryGetProperty("Spells", out var spells)
                    && !phase.TryGetProperty("spells", out spells))
                {
                    continue;
                }

                if (spells.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                var targetCollection =
                    phaseName.Contains("Burst", StringComparison.OrdinalIgnoreCase)
                        ? BurstSpells
                        : phaseName.Contains("Void", StringComparison.OrdinalIgnoreCase)
                            ? VoidSpells
                            : null;

                if (targetCollection is null)
                {
                    continue;
                }

                foreach (var spell in spells.EnumerateArray())
                {
                    var configuredName = GetFlexibleString(spell, "ConfiguredName", "configuredName");
                    var name = GetFlexibleString(spell, "Name", "name");
                    var variant = GetFlexibleString(spell, "Variant", "variant");
                    var enhancedSelected = GetFlexibleString(spell, "EnhancedSelected", "enhancedSelected");
                    var classAllowed = GetFlexibleString(spell, "ClassAllowed", "classAllowed");
                    var levelSatisfied = GetFlexibleString(spell, "LevelSatisfied", "levelSatisfied");
                    var warning = GetFlexibleString(spell, "Warning", "warning");

                    var chip = targetCollection.FirstOrDefault(x =>
                        x.Name.Equals(configuredName, StringComparison.OrdinalIgnoreCase)
                        || x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                    if (chip is null)
                    {
                        continue;
                    }

                    chip.Name = string.IsNullOrWhiteSpace(name) ? configuredName : name;

                    if (!string.IsNullOrWhiteSpace(warning)
                        || classAllowed.Equals("False", StringComparison.OrdinalIgnoreCase)
                        || levelSatisfied.Equals("False", StringComparison.OrdinalIgnoreCase))
                    {
                        chip.Badge = "Invalid";
                        chip.BadgeBrush = "#F4CCCC";
                    }
                    else if (enhancedSelected.Equals("True", StringComparison.OrdinalIgnoreCase)
                        || variant.Equals("Enhanced", StringComparison.OrdinalIgnoreCase))
                    {
                        chip.Badge = "Enhanced";
                        chip.BadgeBrush = "#FFE699";
                    }
                    else
                    {
                        chip.Badge = "Base";
                        chip.BadgeBrush = "#C6EFCE";
                    }
                }
            }

            DataContext = null;
            DataContext = this;
        }
        catch (Exception ex)
        {
            AppendLog("Could not load spell validation JSON:");
            AppendLog(ex.Message);
        }
    }

    private static string GetSpellValidationOutputPathFromScenario(string scenarioPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("spellValidationOutputJson", out var output)
            && !root.TryGetProperty("SpellValidationOutputJson", out output))
        {
            return "";
        }

        if (output.ValueKind != JsonValueKind.String)
        {
            return "";
        }

        var raw = output.GetString();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "";
        }

        return ResolvePathRelativeToFile(scenarioPath, raw);
    }

    private static string GetFlexibleString(JsonElement element, string pascalName, string camelName)
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
