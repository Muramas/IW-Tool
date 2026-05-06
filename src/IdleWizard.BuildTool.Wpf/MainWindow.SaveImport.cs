using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace IdleWizard.BuildTool.Wpf;

public partial class MainWindow
{
    public string SaveImportHeader { get; set; } = "Save Import";
    public string SaveImportClassDisplay { get; set; } = "Class: none";
    public string SaveImportPetDisplay { get; set; } = "Pet: none";
    public string SaveImportPetMaxDisplay { get; set; } = "Pet Max Level: none";

    public ObservableCollection<string> SaveImportPresetRows { get; } = new();
    public ObservableCollection<string> SaveImportSpellbarRows { get; } = new();

    private void LoadSaveImportSummaryIfExists(string scenarioPath)
    {
        try
        {
            SaveImportPresetRows.Clear();
            SaveImportSpellbarRows.Clear();

            if (!File.Exists(scenarioPath))
            {
                SaveImportClassDisplay = "Class: none";
                SaveImportPetDisplay = "Pet: none";
                SaveImportPetMaxDisplay = "Pet Max Level: none";
                return;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            var root = doc.RootElement;

            if (!root.TryGetProperty("saveImport", out var saveImport)
                || saveImport.ValueKind != JsonValueKind.Object)
            {
                SaveImportClassDisplay = "Class: no save import";
                SaveImportPetDisplay = "Pet: no save import";
                SaveImportPetMaxDisplay = "Pet Max Level: no save import";
                return;
            }

            var detectedClass = TryGetSaveImportObject(saveImport, "detectedClass");
            var detectedPet = TryGetSaveImportObject(saveImport, "detectedPet");

            var className = GetSaveImportString(detectedClass, "name");
            var classId = GetSaveImportString(detectedClass, "id");

            var petName = GetSaveImportString(detectedPet, "name");
            var petKey = GetSaveImportString(detectedPet, "key");
            var petId = GetSaveImportString(detectedPet, "id");

            var petMaxLevel = GetSaveImportString(saveImport, "petMaxLevel");

            SaveImportClassDisplay = $"Class: {className} ({classId})";
            SaveImportPetDisplay = $"Pet: {petName} ({petKey}, {petId})";
            SaveImportPetMaxDisplay = $"Pet Max Level: {petMaxLevel}";

            if (saveImport.TryGetProperty("presets", out var presets)
                && presets.ValueKind == JsonValueKind.Array)
            {
                foreach (var preset in presets.EnumerateArray())
                {
                    var index = GetSaveImportString(preset, "Index");
                    var name = GetSaveImportString(preset, "Name");
                    var itemCount = GetSaveImportString(preset, "ItemCount");

                    if (string.IsNullOrWhiteSpace(index))
                    {
                        index = GetSaveImportString(preset, "index");
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = GetSaveImportString(preset, "name");
                    }

                    if (string.IsNullOrWhiteSpace(itemCount))
                    {
                        itemCount = GetSaveImportString(preset, "itemCount");
                    }

                    SaveImportPresetRows.Add($"[{index}] {name} ({itemCount} items)");
                }
            }

            if (saveImport.TryGetProperty("spellbar", out var spellbar)
                && spellbar.ValueKind == JsonValueKind.Array)
            {
                foreach (var spell in spellbar.EnumerateArray())
                {
                    var position = GetSaveImportString(spell, "Position");
                    var name = GetSaveImportString(spell, "Name");
                    var key = GetSaveImportString(spell, "Key");

                    if (string.IsNullOrWhiteSpace(position))
                    {
                        position = GetSaveImportString(spell, "position");
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = GetSaveImportString(spell, "name");
                    }

                    if (string.IsNullOrWhiteSpace(key))
                    {
                        key = GetSaveImportString(spell, "key");
                    }

                    SaveImportSpellbarRows.Add($"[{position}] {name} ({key})");
                }
            }

            AppendLog("");
            AppendLog("Save import detected:");
            AppendLog($"  {SaveImportClassDisplay}");
            AppendLog($"  {SaveImportPetDisplay}");
            AppendLog($"  {SaveImportPetMaxDisplay}");
            AppendLog($"  Presets: {SaveImportPresetRows.Count}");
            AppendLog($"  Spellbar: {SaveImportSpellbarRows.Count}");

            DataContext = null;
            DataContext = this;
        }
        catch (Exception ex)
        {
            AppendLog("Could not load save import summary:");
            AppendLog(ex.Message);
        }
    }

    private static JsonElement TryGetSaveImportObject(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        return default;
    }

    private static string GetSaveImportString(JsonElement element, string propertyName)
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
}
