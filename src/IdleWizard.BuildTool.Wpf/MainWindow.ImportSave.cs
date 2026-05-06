using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    private sealed class PresetChoice
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public int ItemCount { get; set; }

        public string Display => $"[{Index}] {Name} ({ItemCount} items)";
    }

    private sealed class PhasePresetSelector
    {
        public string PhaseName { get; set; } = "";
        public ComboBox ComboBox { get; set; } = new();
    }

    private void ImportSave_Click(object sender, RoutedEventArgs e)
    {
        var presetChoices = new List<PresetChoice>();
        var phaseSelectors = new List<PhasePresetSelector>();

        var lastSaveInputPath = "";
        var lastOptionsOutputPath = "";

        var dialog = new Window
        {
            Title = "Import Idle Wizard Save",
            Owner = this,
            Width = 1080,
            Height = 820,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18))
        };

        var root = new Grid
        {
            Margin = new Thickness(14)
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(180) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = "Import Idle Wizard Save",
            Foreground = Brushes.White,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        };

        Grid.SetRow(title, 0);
        root.Children.Add(title);

        var saveTextBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(28, 28, 28)),
            Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 235)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            Padding = new Thickness(8)
        };

        Grid.SetRow(saveTextBox, 1);
        root.Children.Add(saveTextBox);

        var topButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 10)
        };

        var loadFileButton = new Button
        {
            Content = "Load From File",
            Width = 130,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var decodeButton = new Button
        {
            Content = "Decode Save",
            Width = 120,
            Margin = new Thickness(0, 0, 8, 0),
            FontWeight = FontWeights.SemiBold
        };

        var applyButton = new Button
        {
            Content = "Apply Import",
            Width = 120,
            Margin = new Thickness(0, 0, 8, 0),
            FontWeight = FontWeights.SemiBold,
            IsEnabled = false
        };

        var closeButton = new Button
        {
            Content = "Close",
            Width = 90
        };

        topButtons.Children.Add(loadFileButton);
        topButtons.Children.Add(decodeButton);
        topButtons.Children.Add(applyButton);
        topButtons.Children.Add(closeButton);

        Grid.SetRow(topButtons, 2);
        root.Children.Add(topButtons);

        var bodyGrid = new Grid();
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        var previewBox = new TextBox
        {
            Text = "Decoded save preview will appear here.",
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Background = new SolidColorBrush(Color.FromRgb(24, 32, 42)),
            Foreground = new SolidColorBrush(Color.FromRgb(235, 240, 250)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 95, 125)),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 10, 0)
        };

        Grid.SetColumn(previewBox, 0);
        bodyGrid.Children.Add(previewBox);

        var rightScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var importPanel = new StackPanel();

        importPanel.Children.Add(MakeImportHeader("Import Options"));

        importPanel.Children.Add(new CheckBox
        {
            Content = "Character / realm data",
            IsChecked = true,
            IsEnabled = false,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 5)
        });

        importPanel.Children.Add(new CheckBox
        {
            Content = "Pet max level",
            IsChecked = true,
            IsEnabled = false,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 5)
        });

        importPanel.Children.Add(new CheckBox
        {
            Content = "Equipment presets",
            IsChecked = true,
            IsEnabled = false,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 5)
        });

        importPanel.Children.Add(new CheckBox
        {
            Content = "Current spellbar into phases (coming later)",
            IsChecked = false,
            IsEnabled = false,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            Margin = new Thickness(0, 0, 0, 5)
        });

        importPanel.Children.Add(new CheckBox
        {
            Content = "Current pet into phases (coming later)",
            IsChecked = false,
            IsEnabled = false,
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            Margin = new Thickness(0, 0, 0, 14)
        });

        importPanel.Children.Add(MakeImportHeader("Phase Equipment Mapping"));

        var phaseMappingPanel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        importPanel.Children.Add(phaseMappingPanel);

        importPanel.Children.Add(new TextBlock
        {
            Text = "Choose which saved equipment preset should be applied to each phase. Preset names are user-defined, so the tool does not guess intent.",
            Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 190)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });

        rightScroll.Content = importPanel;
        Grid.SetColumn(rightScroll, 1);
        bodyGrid.Children.Add(rightScroll);

        Grid.SetRow(bodyGrid, 3);
        root.Children.Add(bodyGrid);

        var footer = new TextBlock
        {
            Text = "v0.3: applies class, max pet level, chosen equipment presets, and attaches save calculation context summary. Current run character level is not imported yet.",
            Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
            FontSize = 12,
            Margin = new Thickness(0, 10, 0, 0)
        };

        Grid.SetRow(footer, 4);
        root.Children.Add(footer);

        loadFileButton.Click += (_, _) =>
        {
            var picker = new OpenFileDialog
            {
                Title = "Load Idle Wizard save string",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (picker.ShowDialog(dialog) == true)
            {
                saveTextBox.Text = File.ReadAllText(picker.FileName);
            }
        };

        closeButton.Click += (_, _) => dialog.Close();

        decodeButton.Click += async (_, _) =>
        {
            var saveString = saveTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(saveString))
            {
                MessageBox.Show(
                    dialog,
                    "Paste or load a save export string first.",
                    "Import Save",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            decodeButton.IsEnabled = false;
            applyButton.IsEnabled = false;
            previewBox.Text = "Decoding save...";

            try
            {
                var repoRoot = FindRepoRootForImportSave();
                var cliProject = Path.Combine(repoRoot, "src", "IdleWizard.BuildTool.Cli", "IdleWizard.BuildTool.Cli.csproj");

                lastSaveInputPath = Path.Combine(repoRoot, "save_import_dialog_input.txt");
                lastOptionsOutputPath = Path.Combine(repoRoot, "save_import_dialog_options.json");
                var workspacePath = Path.Combine(repoRoot, "iw_workspace_vNext");

                File.WriteAllText(lastSaveInputPath, saveString);

                var args =
                    "run --project \"" + cliProject + "\" -- --save-import-options \"" +
                    lastSaveInputPath + "\" \"" +
                    workspacePath + "\" \"" +
                    lastOptionsOutputPath + "\"";

                AppendLog("");
                AppendLog("Running save import preview:");
                AppendLog("dotnet " + args);

                var output = await RunDotnetForImportSaveAsync(repoRoot, args);
                AppendLog(output);

                if (!File.Exists(lastOptionsOutputPath))
                {
                    previewBox.Text =
                        "Save import failed. CLI did not create save_import_dialog_options.json." +
                        Environment.NewLine +
                        Environment.NewLine +
                        output;

                    return;
                }

                previewBox.Text = BuildSaveImportPreview(lastOptionsOutputPath);

                presetChoices.Clear();
                presetChoices.AddRange(LoadPresetChoices(lastOptionsOutputPath));

                phaseSelectors.Clear();
                phaseMappingPanel.Children.Clear();

                foreach (var phaseName in LoadScenarioPhaseNamesFromCurrentPath())
                {
                    var selector = BuildPhasePresetSelector(phaseName, presetChoices);
                    phaseSelectors.Add(selector.Selector);
                    phaseMappingPanel.Children.Add(selector.Row);
                }

                applyButton.IsEnabled = presetChoices.Count > 0 && phaseSelectors.Count > 0;
            }
            catch (Exception ex)
            {
                previewBox.Text = ex.ToString();
                AppendLog("Import Save preview failed:");
                AppendLog(ex.ToString());
            }
            finally
            {
                decodeButton.IsEnabled = true;
            }
        };

        applyButton.Click += async (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(lastSaveInputPath) || !File.Exists(lastSaveInputPath))
            {
                MessageBox.Show(
                    dialog,
                    "Decode a save before applying import.",
                    "Import Save",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            if (phaseSelectors.Count == 0)
            {
                MessageBox.Show(
                    dialog,
                    "No phase preset mappings are available.",
                    "Import Save",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            var confirmed = MessageBox.Show(
                dialog,
                "Apply this save import to a new scenario file and load it?",
                "Confirm Import",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (confirmed != MessageBoxResult.Yes)
            {
                return;
            }

            applyButton.IsEnabled = false;

            try
            {
                var repoRoot = FindRepoRootForImportSave();
                var cliProject = Path.Combine(repoRoot, "src", "IdleWizard.BuildTool.Cli", "IdleWizard.BuildTool.Cli.csproj");

                var scenarioInPath = ResolveScenarioPathForImportSave();
                var scenarioOutPath = Path.Combine(repoRoot, "scenario_imported_from_dialog.json");
                var calculationContextPath = Path.Combine(repoRoot, "save_calculation_context.json");
                var workspacePath = Path.Combine(repoRoot, "iw_workspace_vNext");

                var mappingArgs = new List<string>();

                foreach (var selector in phaseSelectors)
                {
                    if (selector.ComboBox.SelectedItem is not PresetChoice preset)
                    {
                        continue;
                    }

                    mappingArgs.Add("\"" + selector.PhaseName + "=" + preset.Index + "\"");
                }

                var args =
                    "run --project \"" + cliProject + "\" -- --generate-scenario-from-save \"" +
                    lastSaveInputPath + "\" \"" +
                    workspacePath + "\" \"" +
                    scenarioInPath + "\" \"" +
                    scenarioOutPath + "\" " +
                    string.Join(" ", mappingArgs);

                AppendLog("");
                AppendLog("Applying save import:");
                AppendLog("dotnet " + args);

                var output = await RunDotnetForImportSaveAsync(repoRoot, args);
                AppendLog(output);

                var contextArgs =
                    "run --project \"" + cliProject + "\" -- --save-calculation-context \"" +
                    lastSaveInputPath + "\" \"" +
                    workspacePath + "\" \"" +
                    calculationContextPath + "\"";

                AppendLog("");
                AppendLog("Generating save calculation context:");
                AppendLog("dotnet " + contextArgs);

                var contextOutput = await RunDotnetForImportSaveAsync(repoRoot, contextArgs);
                AppendLog(contextOutput);

                if (!File.Exists(scenarioOutPath))
                {
                    MessageBox.Show(
                        dialog,
                        "Import command finished but did not create scenario_imported_from_dialog.json.",
                        "Import Save",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );

                    return;
                }

                if (File.Exists(calculationContextPath))
                {
                    AttachCalculationContextSummaryToScenario(
                        scenarioOutPath,
                        calculationContextPath,
                        repoRoot
                    );
                }

                ScenarioPathTextBox.Text = scenarioOutPath;
                LoadScenario_Click(this, new RoutedEventArgs());

                MessageBox.Show(
                    dialog,
                    "Import applied and scenario loaded.",
                    "Import Save",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                dialog.Close();
            }
            catch (Exception ex)
            {
                AppendLog("Apply import failed:");
                AppendLog(ex.ToString());

                MessageBox.Show(
                    dialog,
                    ex.ToString(),
                    "Apply Import Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                applyButton.IsEnabled = true;
            }
        };

        dialog.Content = root;
        dialog.ShowDialog();
    }

    private static TextBlock MakeImportHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            Foreground = Brushes.White,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };
    }

    private static (Grid Row, PhasePresetSelector Selector) BuildPhasePresetSelector(
        string phaseName,
        IReadOnlyList<PresetChoice> presetChoices)
    {
        var row = new Grid
        {
            Margin = new Thickness(0, 0, 0, 8)
        };

        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new TextBlock
        {
            Text = phaseName,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0)
        };

        var combo = new ComboBox
        {
            ItemsSource = presetChoices,
            DisplayMemberPath = "Display",
            SelectedIndex = presetChoices.Count > 0 ? 0 : -1,
            MinWidth = 260
        };

        Grid.SetColumn(label, 0);
        Grid.SetColumn(combo, 1);

        row.Children.Add(label);
        row.Children.Add(combo);

        return (
            row,
            new PhasePresetSelector
            {
                PhaseName = phaseName,
                ComboBox = combo
            }
        );
    }

    private List<string> LoadScenarioPhaseNamesFromCurrentPath()
    {
        var result = new List<string>();
        var scenarioPath = ResolveScenarioPathForImportSave();

        if (!File.Exists(scenarioPath))
        {
            result.Add("Burst");
            result.Add("Void Mana");
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("phases", out var phases)
            || phases.ValueKind != JsonValueKind.Array)
        {
            result.Add("Burst");
            result.Add("Void Mana");
            return result;
        }

        foreach (var phase in phases.EnumerateArray())
        {
            var name = GetImportString(phase, "name");

            if (!string.IsNullOrWhiteSpace(name))
            {
                result.Add(name);
            }
        }

        if (result.Count == 0)
        {
            result.Add("Burst");
            result.Add("Void Mana");
        }

        return result;
    }

    private string ResolveScenarioPathForImportSave()
    {
        var raw = ScenarioPathTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = ".\\scenario_head_hero.json";
        }

        if (Path.IsPathRooted(raw))
        {
            return Path.GetFullPath(raw);
        }

        var repoRoot = FindRepoRootForImportSave();
        return Path.GetFullPath(Path.Combine(repoRoot, raw));
    }

    private static List<PresetChoice> LoadPresetChoices(string optionsPath)
    {
        var result = new List<PresetChoice>();

        using var doc = JsonDocument.Parse(File.ReadAllText(optionsPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("Presets", out var presets)
            || presets.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var preset in presets.EnumerateArray())
        {
            result.Add(
                new PresetChoice
                {
                    Index = GetImportInt(preset, "Index"),
                    Name = GetImportString(preset, "Name"),
                    ItemCount = GetImportInt(preset, "ItemCount")
                }
            );
        }

        return result;
    }

    private static string BuildSaveImportPreview(string optionsPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(optionsPath));
        var root = doc.RootElement;

        var builder = new StringBuilder();

        var detectedClass = TryGetImportObject(root, "DetectedClass");
        var detectedPet = TryGetImportObject(root, "DetectedPet");

        builder.AppendLine("Detected Save");
        builder.AppendLine("-------------");
        builder.AppendLine("Class: " + GetImportString(detectedClass, "Name") + " (" + GetImportString(detectedClass, "Id") + ")");
        builder.AppendLine("Pet: " + GetImportString(detectedPet, "Name") + " (" + GetImportString(detectedPet, "Key") + ", " + GetImportString(detectedPet, "Id") + ")");
        builder.AppendLine("Pet Max Level: " + GetImportString(root, "PetMaxLevel"));
        builder.AppendLine();

        builder.AppendLine("Equipment Presets");
        builder.AppendLine("-----------------");

        if (root.TryGetProperty("Presets", out var presets)
            && presets.ValueKind == JsonValueKind.Array)
        {
            foreach (var preset in presets.EnumerateArray())
            {
                builder.AppendLine(
                    "[" + GetImportString(preset, "Index") + "] " +
                    GetImportString(preset, "Name") +
                    " (" + GetImportString(preset, "ItemCount") + " items)"
                );
            }
        }

        builder.AppendLine();
        builder.AppendLine("Spellbar");
        builder.AppendLine("--------");

        if (root.TryGetProperty("Spellbar", out var spellbar)
            && spellbar.ValueKind == JsonValueKind.Array)
        {
            foreach (var spell in spellbar.EnumerateArray())
            {
                builder.AppendLine(
                    "[" + GetImportString(spell, "Position") + "] " +
                    GetImportString(spell, "Name") +
                    " (" + GetImportString(spell, "Key") + ")"
                );
            }
        }

        return builder.ToString();
    }

    private static void AttachCalculationContextSummaryToScenario(
        string scenarioPath,
        string calculationContextPath,
        string repoRoot)
    {
        var scenarioNode = JsonNode.Parse(File.ReadAllText(scenarioPath));
        var contextNode = JsonNode.Parse(File.ReadAllText(calculationContextPath));

        if (scenarioNode is not JsonObject scenarioObject
            || contextNode is not JsonObject contextObject)
        {
            return;
        }

        var relativeContextPath = Path.GetRelativePath(repoRoot, calculationContextPath);
        scenarioObject["saveCalculationContextFile"] = ".\\" + relativeContextPath.Replace('/', '\\');

        var summary = new JsonObject();

        if (contextObject["Character"] is JsonObject character)
        {
            summary["class"] = CloneValue(character["ClassName"]);
            summary["pet"] = CloneValue(character["PetKey"]);
            summary["heroMaxLevelAllTime"] = CloneValue(character["HeroMaxLevelAllTime"]);
            summary["maxPetLevel"] = CloneValue(character["MaxPetLevel"]);
            summary["petMaxLevelAllTime"] = CloneValue(character["PetMaxLevelAllTime"]);
        }

        if (contextObject["Progress"] is JsonObject progress)
        {
            summary["totalManaLog"] = CloneValue(progress["TotalManaLog"]);
            summary["ascends"] = CloneValue(progress["Ascends"]);
            summary["ascendsRealm"] = CloneValue(progress["AscendsRealm"]);
        }

        if (contextObject["Buildings"] is JsonArray buildings)
        {
            summary["buildingCount"] = buildings.Count;
        }

        if (contextObject["Spells"] is JsonObject spells
            && spells["Spellbar"] is JsonArray spellbar)
        {
            summary["spellbarCount"] = spellbar.Count;
        }

        if (contextObject["Craft"] is JsonObject craft)
        {
            summary["craftItemCount"] = CloneValue(craft["ItemCount"]);
        }

        if (contextObject["Catalysts"] is JsonObject catalysts
            && catalysts["Assignments"] is JsonArray catalystAssignments)
        {
            summary["catalystAssignedTiers"] = catalystAssignments.Count;
        }

        summary["generatedAtUtc"] = DateTime.UtcNow.ToString("O");
        summary["currentCharacterLevelMapped"] = false;
        summary["currentCharacterLevelNote"] = "Current run character level is not mapped yet. HeroMaxLevelAllTime is stored separately.";

        scenarioObject["saveCalculationContextSummary"] = summary;

        File.WriteAllText(
            scenarioPath,
            scenarioObject.ToJsonString(
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                }
            )
        );
    }

    private static JsonNode? CloneValue(JsonNode? node)
    {
        return node?.DeepClone();
    }

    private static JsonElement TryGetImportObject(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        return default;
    }

    private static string GetImportString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out var value))
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

    private static int GetImportInt(JsonElement element, string propertyName)
    {
        var text = GetImportString(element, propertyName);
        return int.TryParse(text, out var value) ? value : 0;
    }

    private static async Task<string> RunDotnetForImportSaveAsync(
        string workingDirectory,
        string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            return "Failed to start dotnet process.";
        }

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return stdout + Environment.NewLine + stderr;
    }

    private static string FindRepoRootForImportSave()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);

        while (dir is not null)
        {
            var cliProject = Path.Combine(
                dir.FullName,
                "src",
                "IdleWizard.BuildTool.Cli",
                "IdleWizard.BuildTool.Cli.csproj"
            );

            if (File.Exists(cliProject))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Environment.CurrentDirectory;
    }
}
