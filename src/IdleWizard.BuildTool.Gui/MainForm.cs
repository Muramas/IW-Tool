using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;

namespace IdleWizard.BuildTool.Gui;

public sealed class MainForm : Form
{
    private readonly TextBox _scenarioPathTextBox;
    private readonly Button _browseScenarioButton;
    private readonly Button _loadScenarioButton;
    private readonly Button _runAnalysisButton;
    private readonly Button _openResultButton;

    private readonly DataGridView _globalGrid;
    private readonly DataGridView _burstGrid;
    private readonly DataGridView _voidManaGrid;
    private readonly DataGridView _resultsGrid;

    private readonly TextBox _detailsTextBox;
    private readonly TextBox _logTextBox;

    private string? _lastResultPath;

    public MainForm()
    {
        Text = "Idle Wizard Build Tool - Build Optimizer";
        Width = 1650;
        Height = 950;
        StartPosition = FormStartPosition.CenterScreen;

        BackColor = Color.FromArgb(28, 28, 28);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(6),
            BackColor = Color.FromArgb(28, 28, 28)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 67));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 33));

        Controls.Add(root);

        // Top toolbar
        var toolbar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1,
            BackColor = Color.FromArgb(45, 45, 45)
        };

        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

        root.Controls.Add(toolbar, 0, 0);

        toolbar.Controls.Add(MakeToolbarLabel("Scenario"), 0, 0);

        _scenarioPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = @".\scenario_head_hero.json",
            BorderStyle = BorderStyle.FixedSingle
        };

        _browseScenarioButton = new Button
        {
            Text = "Browse",
            Dock = DockStyle.Fill
        };

        _loadScenarioButton = new Button
        {
            Text = "Load",
            Dock = DockStyle.Fill
        };

        _runAnalysisButton = new Button
        {
            Text = "Analyze",
            Dock = DockStyle.Fill
        };

        _openResultButton = new Button
        {
            Text = "Open JSON",
            Dock = DockStyle.Fill
        };

        toolbar.Controls.Add(_scenarioPathTextBox, 1, 0);
        toolbar.Controls.Add(_browseScenarioButton, 2, 0);
        toolbar.Controls.Add(_loadScenarioButton, 3, 0);
        toolbar.Controls.Add(_runAnalysisButton, 4, 0);
        toolbar.Controls.Add(_openResultButton, 5, 0);
        toolbar.Controls.Add(MakeToolbarLabel("GUI v0.2"), 6, 0);

        // Main spreadsheet area
        var mainGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(28, 28, 28)
        };

        mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 325));
        mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        mainGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        root.Controls.Add(mainGrid, 0, 1);

        _globalGrid = CreateSheetGrid();
        _burstGrid = CreateSheetGrid();
        _voidManaGrid = CreateSheetGrid();

        mainGrid.Controls.Add(CreateTitledPanel("General / Realm / Attributes", _globalGrid), 0, 0);
        mainGrid.Controls.Add(CreateTitledPanel("Burst Phase", _burstGrid), 1, 0);
        mainGrid.Controls.Add(CreateTitledPanel("Void Mana Phase", _voidManaGrid), 2, 0);

        // Bottom area: results + details + log
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.FromArgb(28, 28, 28)
        };

        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 72));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 28));

        root.Controls.Add(bottom, 0, 2);

        _resultsGrid = CreateResultsGrid();

        _detailsTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            WordWrap = false,
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(245, 245, 245)
        };

        _logTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            WordWrap = false,
            Font = new Font("Consolas", 8),
            BackColor = Color.FromArgb(240, 240, 240)
        };

        bottom.Controls.Add(CreateTitledPanel("Ranked Recommendations", _resultsGrid), 0, 0);
        bottom.Controls.Add(CreateTitledPanel("Selected Item Details", _detailsTextBox), 1, 0);
        bottom.SetColumnSpan(_logTextBox, 2);
        bottom.Controls.Add(CreateTitledPanel("Log", _logTextBox), 0, 1);

        _browseScenarioButton.Click += BrowseScenario;
        _loadScenarioButton.Click += (_, _) => LoadScenarioIntoSheets();
        _runAnalysisButton.Click += async (_, _) => await RunAnalysisAsync();
        _openResultButton.Click += OpenResultJson;
        _resultsGrid.SelectionChanged += ResultsGridSelectionChanged;

        LoadScenarioIntoSheets();
    }

    private static Label MakeToolbarLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Padding = new Padding(6, 0, 0, 0)
        };
    }

    private static Control CreateTitledPanel(string title, Control content)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Margin = new Padding(3),
            BackColor = Color.FromArgb(28, 28, 28)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.FromArgb(190, 190, 190),
            ForeColor = Color.Black,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Padding = new Padding(6, 0, 0, 0)
        };

        content.Dock = DockStyle.Fill;

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(content, 0, 1);

        return panel;
    }

    private static DataGridView CreateSheetGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            MultiSelect = false,
            BackgroundColor = Color.FromArgb(215, 215, 215),
            BorderStyle = BorderStyle.None,
            GridColor = Color.FromArgb(150, 150, 150),
            Font = new Font("Segoe UI", 9)
        };

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(205, 205, 205);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.DefaultCellStyle.BackColor = Color.FromArgb(220, 220, 220);
        grid.DefaultCellStyle.ForeColor = Color.Black;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(170, 205, 235);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        grid.RowTemplate.Height = 24;

        grid.Columns.Add("field", "Field");
        grid.Columns.Add("value", "Value");
        grid.Columns.Add("extra", "Extra");

        grid.Columns[0].FillWeight = 35;
        grid.Columns[1].FillWeight = 50;
        grid.Columns[2].FillWeight = 15;

        return grid;
    }

    private static DataGridView CreateResultsGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = Color.FromArgb(245, 245, 245),
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9)
        };

        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(205, 205, 205);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(170, 205, 235);
        grid.DefaultCellStyle.SelectionForeColor = Color.Black;

        grid.Columns.Add("rank", "Rank");
        grid.Columns.Add("itemId", "Item ID");
        grid.Columns.Add("tier", "Tier");
        grid.Columns.Add("name", "Item");
        grid.Columns.Add("weightedScore", "Weighted Score");
        grid.Columns.Add("vsEquipped", "Vs Equipped");
        grid.Columns.Add("verified", "Verified");

        grid.Columns[0].FillWeight = 8;
        grid.Columns[1].FillWeight = 10;
        grid.Columns[2].FillWeight = 8;
        grid.Columns[3].FillWeight = 34;
        grid.Columns[4].FillWeight = 18;
        grid.Columns[5].FillWeight = 18;
        grid.Columns[6].FillWeight = 10;

        return grid;
    }

    private void BrowseScenario(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select scenario JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(_scenarioPathTextBox.Text)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _scenarioPathTextBox.Text = dialog.FileName;
            LoadScenarioIntoSheets();
        }
    }

    private void LoadScenarioIntoSheets()
    {
        try
        {
            _globalGrid.Rows.Clear();
            _burstGrid.Rows.Clear();
            _voidManaGrid.Rows.Clear();

            var scenarioPath = ResolvePath(_scenarioPathTextBox.Text);

            if (!File.Exists(scenarioPath))
            {
                FillFallbackLayout();
                return;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            var root = doc.RootElement;

            FillGlobalGrid(root);
            FillPhaseGrids(root);
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
            FillFallbackLayout();
        }
    }

    private void FillFallbackLayout()
    {
        AddSection(_globalGrid, "General");
        AddRow(_globalGrid, "Class", "");
        AddRow(_globalGrid, "Ascension", "");
        AddRow(_globalGrid, "Production Source", "");
        AddRow(_globalGrid, "Character Level", "");
        AddRow(_globalGrid, "Enchanting Dust", "");
        AddRow(_globalGrid, "Max Enchantment", "");

        AddSection(_burstGrid, "Burst Phase");
        AddRow(_burstGrid, "Pet", "");
        AddRow(_burstGrid, "Spell 1", "");
        AddRow(_burstGrid, "Head", "");

        AddSection(_voidManaGrid, "Void Mana Phase");
        AddRow(_voidManaGrid, "Pet", "");
        AddRow(_voidManaGrid, "Spell 1", "");
        AddRow(_voidManaGrid, "Head", "");
    }

    private void FillGlobalGrid(JsonElement root)
    {
        AddSection(_globalGrid, "General");

        if (root.TryGetProperty("globalContext", out var global)
            && global.ValueKind == JsonValueKind.Object)
        {
            AddKnownGlobal(global, "class", "Class");
            AddKnownGlobal(global, "ascension", "Ascension");
            AddKnownGlobal(global, "productionSource", "Production Source");
            AddKnownGlobal(global, "characterLevel", "Character Level");
            AddKnownGlobal(global, "maxPetLevel", "Maximum Pet Level");
            AddKnownGlobal(global, "legacy", "Legacy");
            AddKnownGlobal(global, "totalManaLog", "Total Mana log");
            AddKnownGlobal(global, "enchantingDust", "Enchanting Dust");
            AddKnownGlobal(global, "maxEnchantment", "Max Enchantment");
            AddKnownGlobal(global, "challengeEnchantBonusLevels", "Challenge Enchant Bonus");
        }
        else
        {
            AddRow(_globalGrid, "Class", "");
            AddRow(_globalGrid, "Ascension", "");
            AddRow(_globalGrid, "Production Source", "");
            AddRow(_globalGrid, "Character Level", "");
            AddRow(_globalGrid, "Enchanting Dust", "");
            AddRow(_globalGrid, "Max Enchantment", "");
        }

        AddSection(_globalGrid, "Objectives");

        if (root.TryGetProperty("objectives", out var objectives)
            && objectives.ValueKind == JsonValueKind.Object)
        {
            foreach (var objective in objectives.EnumerateObject())
            {
                AddRow(_globalGrid, objective.Name, GetJsonScalarAsString(objective.Value), "weight");
            }
        }

        AddSection(_globalGrid, "Current Values");

        if (root.TryGetProperty("currentValues", out var currentValues)
            && currentValues.ValueKind == JsonValueKind.Object)
        {
            foreach (var value in currentValues.EnumerateObject())
            {
                AddRow(_globalGrid, value.Name, GetJsonScalarAsString(value.Value));
            }
        }
    }

    private void AddKnownGlobal(JsonElement global, string jsonName, string label)
    {
        if (global.TryGetProperty(jsonName, out var value))
        {
            AddRow(_globalGrid, label, GetJsonScalarAsString(value));
        }
        else
        {
            AddRow(_globalGrid, label, "");
        }
    }

    private void FillPhaseGrids(JsonElement root)
    {
        if (!root.TryGetProperty("phases", out var phases)
            || phases.ValueKind != JsonValueKind.Array)
        {
            AddSection(_burstGrid, "Burst Phase");
            AddRow(_burstGrid, "Pet", "");
            AddRow(_burstGrid, "Spell 1", "");
            AddRow(_burstGrid, "Head", "");

            AddSection(_voidManaGrid, "Void Mana Phase");
            AddRow(_voidManaGrid, "Pet", "");
            AddRow(_voidManaGrid, "Spell 1", "");
            AddRow(_voidManaGrid, "Head", "");
            return;
        }

        var phaseList = phases.EnumerateArray().ToList();

        FillPhaseGrid(
            _burstGrid,
            phaseList.FirstOrDefault(p => GetString(p, "name").Contains("Burst", StringComparison.OrdinalIgnoreCase)),
            "Burst Phase"
        );

        FillPhaseGrid(
            _voidManaGrid,
            phaseList.FirstOrDefault(p =>
                GetString(p, "name").Contains("Void", StringComparison.OrdinalIgnoreCase)
                || GetString(p, "name").Contains("VM", StringComparison.OrdinalIgnoreCase)),
            "Void Mana Phase"
        );
    }

    private void FillPhaseGrid(DataGridView grid, JsonElement phase, string fallbackName)
    {
        var name = phase.ValueKind == JsonValueKind.Object
            ? GetString(phase, "name")
            : fallbackName;

        if (string.IsNullOrWhiteSpace(name))
        {
            name = fallbackName;
        }

        AddSection(grid, name);

        if (phase.ValueKind != JsonValueKind.Object)
        {
            AddRow(grid, "Pet", "");
            AddRow(grid, "Spell 1", "");
            AddRow(grid, "Head", "");
            return;
        }

        AddRow(grid, "Phase Weight", GetString(phase, "phaseWeight"));
        AddRow(grid, "Pet", GetString(phase, "pet"), GetString(phase, "petLevel"));

        AddSection(grid, "Spells");

        if (phase.TryGetProperty("spells", out var spells)
            && spells.ValueKind == JsonValueKind.Array)
        {
            var index = 1;

            foreach (var spell in spells.EnumerateArray())
            {
                AddRow(grid, $"Spell {index}", GetJsonScalarAsString(spell));
                index++;
            }
        }

        AddSection(grid, "Skills");

        if (phase.TryGetProperty("skills", out var skills)
            && skills.ValueKind == JsonValueKind.Array)
        {
            var index = 1;

            foreach (var skill in skills.EnumerateArray())
            {
                AddRow(grid, $"Skill {index}", GetJsonScalarAsString(skill));
                index++;
            }
        }

        AddSection(grid, "Equipment");

        if (phase.TryGetProperty("equipment", out var equipment)
            && equipment.ValueKind == JsonValueKind.Object)
        {
            foreach (var slot in equipment.EnumerateObject())
            {
                var item = slot.Value;

                var itemId = GetString(item, "itemId");
                var tier = GetString(item, "tier");
                var enchant = GetString(item, "enchantLevel");
                var bonus = GetString(item, "bonusEnchantLevel");

                var extra = "";

                if (!string.IsNullOrWhiteSpace(enchant) || !string.IsNullOrWhiteSpace(bonus))
                {
                    extra = $"{enchant}+{bonus}".Trim('+');
                }

                AddRow(grid, slot.Name, $"Item {itemId} / Tier {tier}", extra);
            }
        }
    }

    private static void AddSection(DataGridView grid, string title)
    {
        var rowIndex = grid.Rows.Add(title, "", "");
        var row = grid.Rows[rowIndex];

        row.DefaultCellStyle.BackColor = Color.FromArgb(180, 180, 180);
        row.DefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
    }

    private static void AddRow(DataGridView grid, string field, string value, string extra = "")
    {
        var rowIndex = grid.Rows.Add(field, value, extra);
        var row = grid.Rows[rowIndex];

        if (field.Contains("Enchant", StringComparison.OrdinalIgnoreCase)
            || field.Contains("Dust", StringComparison.OrdinalIgnoreCase))
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(220, 235, 245);
        }
        else
        {
            row.DefaultCellStyle.BackColor = Color.FromArgb(225, 225, 225);
        }
    }

    private async Task RunAnalysisAsync()
    {
        try
        {
            _runAnalysisButton.Enabled = false;
            _logTextBox.Clear();
            _resultsGrid.Rows.Clear();
            _detailsTextBox.Clear();

            var scenarioPath = ResolvePath(_scenarioPathTextBox.Text);

            if (!File.Exists(scenarioPath))
            {
                MessageBox.Show(this, $"Scenario file not found:\n{scenarioPath}", "Missing scenario", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var resultPath = GetOutputJsonPathFromScenario(scenarioPath);
            _lastResultPath = resultPath;

            AppendLog($"Scenario: {scenarioPath}");
            AppendLog($"Expected result JSON: {resultPath}");
            AppendLog("");

            var repoRoot = FindRepoRoot();

            var cliProject = Path.Combine(
                repoRoot,
                "src",
                "IdleWizard.BuildTool.Cli",
                "IdleWizard.BuildTool.Cli.csproj"
            );

            if (!File.Exists(cliProject))
            {
                MessageBox.Show(this, $"CLI project not found:\n{cliProject}", "Missing CLI project", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var arguments =
                $"run --project \"{cliProject}\" -- --score-slot-weighted-file \"{scenarioPath}\"";

            AppendLog("Running:");
            AppendLog("dotnet " + arguments);
            AppendLog("");

            var output = await RunProcessAsync(repoRoot, "dotnet", arguments);
            AppendLog(output);

            if (!File.Exists(resultPath))
            {
                MessageBox.Show(this, $"Result JSON was not created:\n{resultPath}", "Missing result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadResultJson(resultPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _runAnalysisButton.Enabled = true;
        }
    }

    private void LoadResultJson(string resultPath)
    {
        _resultsGrid.Rows.Clear();
        _detailsTextBox.Clear();

        using var doc = JsonDocument.Parse(File.ReadAllText(resultPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results)
            || results.ValueKind != JsonValueKind.Array)
        {
            MessageBox.Show(this, "Result JSON does not contain a results array.", "Invalid result", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (var result in results.EnumerateArray())
        {
            var rank = GetString(result, "rank");
            var itemId = GetString(result, "itemId");
            var tier = GetString(result, "tier");
            var name = GetString(result, "name");
            var weightedScore = GetString(result, "weightedScore");
            var vsEquipped = GetString(result, "vsEquippedScoreDelta");
            var verified = GetString(result, "fullyVerified");

            var rowIndex = _resultsGrid.Rows.Add(
                rank,
                itemId,
                tier,
                name,
                weightedScore,
                vsEquipped,
                verified
            );

            var row = _resultsGrid.Rows[rowIndex];
            row.Tag = result.GetRawText();

            if (double.TryParse(vsEquipped, out var delta))
            {
                if (delta > 0)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(210, 245, 210);
                }
                else if (delta < 0)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(245, 210, 210);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(245, 240, 200);
                }
            }
        }

        if (_resultsGrid.Rows.Count > 0)
        {
            _resultsGrid.Rows[0].Selected = true;
        }

        AppendLog("");
        AppendLog($"Loaded result JSON: {resultPath}");
        AppendLog($"Rows: {_resultsGrid.Rows.Count}");
    }

    private void ResultsGridSelectionChanged(object? sender, EventArgs e)
    {
        if (_resultsGrid.SelectedRows.Count == 0)
        {
            return;
        }

        var raw = _resultsGrid.SelectedRows[0].Tag as string;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        using var doc = JsonDocument.Parse(raw);
        var element = doc.RootElement;

        var builder = new StringBuilder();

        builder.AppendLine($"{GetString(element, "name")}  |  Item {GetString(element, "itemId")}  |  Tier {GetString(element, "tier")}");
        builder.AppendLine($"Weighted Score: {GetString(element, "weightedScore")}");
        builder.AppendLine($"Vs Equipped:    {GetString(element, "vsEquippedScoreDelta")}");
        builder.AppendLine($"Verified:       {GetString(element, "fullyVerified")}");
        builder.AppendLine();

        if (element.TryGetProperty("objectiveContributions", out var contributions)
            && contributions.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine("Objective Contributions");
            builder.AppendLine("-----------------------");

            foreach (var objective in contributions.EnumerateObject())
            {
                var value = objective.Value;

                builder.AppendLine(objective.Name);
                builder.AppendLine($"  before:       {GetString(value, "before")}");
                builder.AppendLine($"  after:        {GetString(value, "after")}");
                builder.AppendLine($"  ratio:        {GetString(value, "ratio")}");
                builder.AppendLine($"  log10 ratio:  {GetString(value, "log10Ratio")}");
                builder.AppendLine($"  weight:       {GetString(value, "weight")}");
                builder.AppendLine($"  contribution: {GetString(value, "contribution")}");
                builder.AppendLine($"  direct:       {GetString(value, "directlyTouched")}");
                builder.AppendLine();
            }
        }

        if (element.TryGetProperty("changedResources", out var changedResources)
            && changedResources.ValueKind == JsonValueKind.Object)
        {
            builder.AppendLine("Changed Resources");
            builder.AppendLine("-----------------");

            foreach (var resource in changedResources.EnumerateObject())
            {
                var value = resource.Value;

                builder.AppendLine($"{resource.Name}: {GetString(value, "before")} -> {GetString(value, "after")}");
            }
        }

        _detailsTextBox.Text = builder.ToString();
    }

    private void OpenResultJson(object? sender, EventArgs e)
    {
        var path = _lastResultPath;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show(this, "No result JSON has been generated yet.", "No result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(
            new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            }
        );
    }

    private static async Task<string> RunProcessAsync(
        string workingDirectory,
        string fileName,
        string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };

        var output = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                output.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        output.AppendLine($"Process exit code: {process.ExitCode}");

        return output.ToString();
    }

    private static string FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;

        while (!string.IsNullOrWhiteSpace(current))
        {
            var candidate = Path.Combine(
                current,
                "src",
                "IdleWizard.BuildTool.Cli",
                "IdleWizard.BuildTool.Cli.csproj"
            );

            if (File.Exists(candidate))
            {
                return current;
            }

            var parent = Directory.GetParent(current);

            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        var localCandidate = Path.Combine(
            Environment.CurrentDirectory,
            "src",
            "IdleWizard.BuildTool.Cli",
            "IdleWizard.BuildTool.Cli.csproj"
        );

        if (File.Exists(localCandidate))
        {
            return Environment.CurrentDirectory;
        }

        throw new DirectoryNotFoundException(
            "Could not locate repository root containing src\\IdleWizard.BuildTool.Cli\\IdleWizard.BuildTool.Cli.csproj"
        );
    }

    private static string GetOutputJsonPathFromScenario(string scenarioPath)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var root = doc.RootElement;

        if (root.TryGetProperty("outputJson", out var outputJson)
            && outputJson.ValueKind == JsonValueKind.String)
        {
            var raw = outputJson.GetString();

            if (!string.IsNullOrWhiteSpace(raw))
            {
                return ResolvePathRelativeToFile(scenarioPath, raw);
            }
        }

        return Path.Combine(
            Path.GetDirectoryName(scenarioPath) ?? Environment.CurrentDirectory,
            "results.json"
        );
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.GetFullPath(path);
    }

    private static string ResolvePathRelativeToFile(string baseFile, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(baseFile))
            ?? Environment.CurrentDirectory;

        return Path.GetFullPath(Path.Combine(directory, path));
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return GetJsonScalarAsString(value);
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

    private void AppendLog(string text)
    {
        _logTextBox.AppendText(text + Environment.NewLine);
    }
}