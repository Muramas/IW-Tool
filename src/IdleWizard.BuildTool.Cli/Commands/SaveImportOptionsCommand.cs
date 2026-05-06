using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveImportOptionsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-import-options .\save_export.txt .\iw_workspace_vNext [save_import_options.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_import_options.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var heroEnum = LoadEnumMap(workspacePath, "HeroesNames.cs", "HeroesNames");
        var petEnum = LoadEnumMap(workspacePath, "PetNames.cs", "PetNames");
        var spellEnum = LoadEnumMap(workspacePath, "Spells.cs", "Spells");
        var spellCatalog = LoadSpellCatalog(workspacePath);
        var petDisplayNames = LoadPetDisplayNames(workspacePath);

        var heroId = GetInt(root, "Hero");
        var petId = GetInt(root, "Pet");

        var classKey = heroEnum.TryGetValue(heroId, out var heroName)
            ? heroName
            : heroId.ToString();

        var petKey = petEnum.TryGetValue(petId, out var petEnumName)
            ? petEnumName
            : petId.ToString();

        var petName = petDisplayNames.TryGetValue(petKey, out var displayName)
            ? displayName
            : petKey;

        var petMaxLevel = GetInt(root, "PetMaxLevel");

        var presets = LoadPresetOptions(root, classKey);
        var chosenSpells = LoadChosenSpells(root, spellEnum, spellCatalog);

        var export = new SaveImportOptionsExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            new DetectedClassOption(heroId, classKey),
            new DetectedPetOption(petId, petKey, petName),
            petMaxLevel,
            presets,
            chosenSpells,
            new[]
            {
                "Preset names are user-defined. The tool should display them as choices, not infer phase intent from names.",
                "The GUI should let the user choose which preset maps to each phase.",
                "Save spellbar is imported as observed selected spells, not automatically assigned to specific phases yet."
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

        Console.WriteLine("Save import options");
        Console.WriteLine("-------------------");
        Console.WriteLine($"Save:          {savePath}");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"Output:        {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Class:         {classKey} ({heroId})");
        Console.WriteLine($"Pet:           {petName} ({petKey}, {petId})");
        Console.WriteLine($"Pet max level: {petMaxLevel}");
        Console.WriteLine($"Presets:       {presets.Count}");
        Console.WriteLine($"Spellbar:      {chosenSpells.Count}");

        Console.WriteLine("");
        Console.WriteLine("Presets:");
        foreach (var preset in presets)
        {
            Console.WriteLine($"  [{preset.Index}] {preset.Name}");
        }

        Console.WriteLine("");
        Console.WriteLine("Spellbar:");
        foreach (var spell in chosenSpells)
        {
            Console.WriteLine($"  [{spell.Position}] {spell.Name} ({spell.Key})");
        }
    }

    private static List<PresetOption> LoadPresetOptions(JsonElement root, string classKey)
    {
        var result = new List<PresetOption>();

        if (!root.TryGetProperty("ItemPresets", out var itemPresets)
            || itemPresets.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!itemPresets.TryGetProperty("prime", out var prime)
            || prime.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!prime.TryGetProperty(classKey, out var classPresets)
            || classPresets.ValueKind != JsonValueKind.String)
        {
            return result;
        }

        var raw = classPresets.GetString() ?? "";

        var pattern = new Regex(
            @"#(?<index>\d+)#(?<name>[^@#]*)@(?<items>[^#]*)",
            RegexOptions.Compiled
        );

        foreach (Match match in pattern.Matches(raw))
        {
            var index = int.Parse(match.Groups["index"].Value);
            var name = match.Groups["name"].Value;
            var itemText = match.Groups["items"].Value;

            var itemCount = itemText
                .Split(';', StringSplitOptions.None)
                .Count(x => !string.IsNullOrWhiteSpace(x));

            result.Add(
                new PresetOption(
                    index,
                    string.IsNullOrWhiteSpace(name) ? $"Preset {index}" : name,
                    itemCount
                )
            );
        }

        return result
            .OrderBy(x => x.Index)
            .ToList();
    }

    private static List<SaveSpellbarOption> LoadChosenSpells(
        JsonElement root,
        Dictionary<int, string> spellEnum,
        Dictionary<string, SpellCatalogEntry> spellCatalog)
    {
        var result = new List<SaveSpellbarOption>();

        if (!root.TryGetProperty("ChoosenSpells", out var chosenSpells)
            || chosenSpells.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var position = 0;

        foreach (var spellElement in chosenSpells.EnumerateArray())
        {
            var id = spellElement.ValueKind == JsonValueKind.Number
                ? spellElement.GetInt32()
                : -1;

            var key = spellEnum.TryGetValue(id, out var enumKey)
                ? enumKey
                : id.ToString();

            spellCatalog.TryGetValue(key, out var spell);

            result.Add(
                new SaveSpellbarOption(
                    position,
                    id,
                    key,
                    spell?.Name ?? key,
                    spell?.SpellType ?? "",
                    spell?.TypeBehavior ?? "",
                    spell?.LevelRequirement ?? ""
                )
            );

            position++;
        }

        return result;
    }

    private static Dictionary<string, SpellCatalogEntry> LoadSpellCatalog(string workspacePath)
    {
        var result = new Dictionary<string, SpellCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        var file = WorkspacePaths.FindDataFile(workspacePath, "Spells");

        if (file is null)
        {
            return result;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var spell in doc.RootElement.EnumerateArray())
        {
            var key = Get(spell, "Key");

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = new SpellCatalogEntry(
                key,
                Get(spell, "Name"),
                Get(spell, "SpellType"),
                Get(spell, "TypeBehavior"),
                Get(spell, "Requirements")
            );
        }

        return result;
    }

    private static Dictionary<string, string> LoadPetDisplayNames(string workspacePath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);

        if (!Directory.Exists(sourceRoot))
        {
            return result;
        }

        var keyPattern = new Regex(
            @"NameKey\s*=\s*PetNames\.([A-Za-z0-9_]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        var namePattern = new Regex(
            @"(?:base\.)?Name\s*=\s*""(?<name>[^""]+)""",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            var keyMatch = keyPattern.Match(text);

            if (!keyMatch.Success)
            {
                continue;
            }

            var key = keyMatch.Groups[1].Value;

            var windowStart = Math.Max(0, keyMatch.Index - 800);
            var windowLength = Math.Min(text.Length - windowStart, 1800);
            var window = text.Substring(windowStart, windowLength);

            var nameMatch = namePattern.Match(window);

            result[key] = nameMatch.Success
                ? nameMatch.Groups["name"].Value
                : key;
        }

        return result;
    }

    private static Dictionary<int, string> LoadEnumMap(
        string workspacePath,
        string fileName,
        string enumName)
    {
        var result = new Dictionary<int, string>();
        var file = WorkspacePaths.FindSourceFile(workspacePath, fileName);

        if (file is null)
        {
            return result;
        }

        var text = File.ReadAllText(file);

        var match = Regex.Match(
            text,
            @"enum\s+" + Regex.Escape(enumName) + @"\s*\{(?<body>[\s\S]*?)\}",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
        {
            return result;
        }

        var current = 0;

        foreach (var raw in match.Groups["body"].Value.Split(','))
        {
            var part = raw.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var pieces = part.Split('=');
            var key = pieces[0].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
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

    private static int GetInt(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0;
    }

    private static string Get(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            _ => value.GetRawText()
        };
    }

    private sealed record SaveImportOptionsExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        DetectedClassOption DetectedClass,
        DetectedPetOption DetectedPet,
        int PetMaxLevel,
        IReadOnlyList<PresetOption> Presets,
        IReadOnlyList<SaveSpellbarOption> Spellbar,
        IReadOnlyList<string> Warnings
    );

    private sealed record DetectedClassOption(
        int Id,
        string Name
    );

    private sealed record DetectedPetOption(
        int Id,
        string Key,
        string Name
    );

    private sealed record PresetOption(
        int Index,
        string Name,
        int ItemCount
    );

    private sealed record SaveSpellbarOption(
        int Position,
        int Id,
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        string LevelRequirement
    );

    private sealed record SpellCatalogEntry(
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        string LevelRequirement
    );
}
