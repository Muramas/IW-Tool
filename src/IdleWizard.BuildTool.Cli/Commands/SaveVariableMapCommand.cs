using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveVariableMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-variable-map .\save_export.txt .\iw_workspace_vNext [save_variable_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_variable_map.json";

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

        var mappings = new List<SaveVariableMapping>();
        var potential = new List<PotentialSaveMapping>();

        var heroId = GetInt(root, "Hero");
        var className = heroEnum.TryGetValue(heroId, out var heroName)
            ? heroName
            : heroId.ToString();

        AddMapping(
            mappings,
            "Hero",
            "globalContext.class",
            className,
            "High",
            true,
            "Hero enum mapped through HeroesNames.cs."
        );

        var petId = GetInt(root, "Pet");
        var petKey = petEnum.TryGetValue(petId, out var petName)
            ? petName
            : petId.ToString();

        AddMapping(
            mappings,
            "Pet",
            "saveImport.detectedPet.key",
            petKey,
            "High",
            false,
            "Detected current pet. Phase pet assignment should remain user-controlled for now."
        );

        var petMaxLevel = GetInt(root, "PetMaxLevel");

        if (petMaxLevel > 0)
        {
            AddMapping(
                mappings,
                "PetMaxLevel",
                "globalContext.maxPetLevel",
                petMaxLevel.ToString(),
                "High",
                true,
                "Direct save field."
            );
        }

        var petMaxLevelAllTime = GetInt(root, "PetMaxLevelAllTime");

        if (petMaxLevelAllTime > 0)
        {
            AddMapping(
                mappings,
                "PetMaxLevelAllTime",
                "progress.petMaxLevelAllTime",
                petMaxLevelAllTime.ToString(),
                "High",
                false,
                "Progress value. Useful for future progression/achievement calculations."
            );
        }

        var heroMaxLevelAllTime = GetInt(root, "HeroMaxLevelAllTime");

        if (heroMaxLevelAllTime > 0)
        {
            AddMapping(
                mappings,
                "HeroMaxLevelAllTime",
                "globalContext.characterLevel",
                heroMaxLevelAllTime.ToString(),
                "Medium",
                true,
                "Currently used as character level until exact current-level save field is confirmed."
            );
        }

        var manaAllTimeExponent = ReadBigNumberExponent(root, "ManaAllTime");

        if (manaAllTimeExponent > 0)
        {
            AddMapping(
                mappings,
                "ManaAllTime.Exponent",
                "globalContext.totalManaLog",
                manaAllTimeExponent.ToString(),
                "High",
                true,
                "Exponent approximates log10 total mana for current formula inputs."
            );
        }

        var ascends = GetInt(root, "Ascends");

        if (ascends > 0)
        {
            AddMapping(
                mappings,
                "Ascends",
                "progress.ascensionsTotal",
                ascends.ToString(),
                "High",
                false,
                "Progress tracking. Not wired into formulas yet."
            );
        }

        var ascendsRealm = GetInt(root, "AscendsRealm");

        if (ascendsRealm > 0)
        {
            AddMapping(
                mappings,
                "AscendsRealm",
                "progress.ascensionsRealm",
                ascendsRealm.ToString(),
                "High",
                false,
                "Progress tracking. Not wired into formulas yet."
            );
        }

        var saveTime = GetString(root, "SaveTime");

        if (!string.IsNullOrWhiteSpace(saveTime))
        {
            AddMapping(
                mappings,
                "SaveTime",
                "saveImport.saveTimeUtc",
                saveTime,
                "High",
                false,
                "Metadata only."
            );
        }

        var spellbarCount = GetArrayCount(root, "ChoosenSpells");

        if (spellbarCount > 0)
        {
            AddMapping(
                mappings,
                "ChoosenSpells",
                "saveImport.spellbar",
                spellbarCount + " selected spells",
                "High",
                false,
                "Spellbar import is available, but phase spell assignment should remain user-controlled."
            );
        }

        if (root.TryGetProperty("ItemPresets", out _))
        {
            AddMapping(
                mappings,
                "ItemPresets",
                "saveImport.presets",
                "present",
                "High",
                false,
                "Equipment preset import is available. User chooses preset-to-phase mapping."
            );
        }

        if (root.TryGetProperty("Craft", out var craft)
            && craft.ValueKind == JsonValueKind.Object)
        {
            potential.Add(
                new PotentialSaveMapping(
                    "Craft.Items",
                    "equipment item tier/enchant inventory",
                    GetCraftItemCount(craft).ToString() + " craft items",
                    "High",
                    "Already used for imported equipment tier/enchant. Could also support item availability and enchant-aware recommendation search."
                )
            );

            potential.Add(
                new PotentialSaveMapping(
                    "Craft.CSkillExp / Craft.GSkillExp",
                    "crafting progression",
                    "present",
                    "Medium",
                    "Potentially relevant to crafting/enchant calculations, but formula mapping needs source review."
                )
            );
        }

        if (root.TryGetProperty("EDE", out _))
        {
            potential.Add(
                new PotentialSaveMapping(
                    "EDE",
                    "Experiment / dust / efficiency context",
                    ReadBigNumberScientific(root, "EDE"),
                    "NeedsReview",
                    "Potentially formula-relevant, but exact internal tool variable mapping needs source review."
                )
            );
        }

        if (root.TryGetProperty("Upgrades", out _))
        {
            potential.Add(
                new PotentialSaveMapping(
                    "Upgrades",
                    "unlocked upgrades / progress modifiers",
                    GetArrayCount(root, "Upgrades") + " upgrades",
                    "NeedsReview",
                    "Could unlock modifiers and formulas. Needs upgrade ID mapping."
                )
            );
        }

        if (root.TryGetProperty("Catalysts", out _))
        {
            potential.Add(
                new PotentialSaveMapping(
                    "Catalysts",
                    "catalyst progression",
                    "present",
                    "NeedsReview",
                    "Likely progression/formula relevant, but not mapped yet."
                )
            );
        }

        var export = new SaveVariableMapExport(
            DateTime.UtcNow.ToString("O"),
            savePath,
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            mappings,
            potential,
            new[]
            {
                "High confidence mappings can be applied directly to scenario/globalContext.",
                "Medium confidence mappings are useful but may need exact save-field confirmation.",
                "NeedsReview mappings likely matter for formulas but require game-code/source mapping before calculation use."
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

        Console.WriteLine("Save variable map");
        Console.WriteLine("-----------------");
        Console.WriteLine($"Save:              {savePath}");
        Console.WriteLine($"Workspace:         {workspacePath}");
        Console.WriteLine($"Output:            {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Mappings:          {mappings.Count}");
        Console.WriteLine($"Potential mappings:{potential.Count}");
        Console.WriteLine("");
        Console.WriteLine("Mapped variables:");

        foreach (var mapping in mappings)
        {
            Console.WriteLine($"  {mapping.SavePath} -> {mapping.ToolPath} = {mapping.Value} [{mapping.Confidence}]");
        }
    }

    private static void AddMapping(
        List<SaveVariableMapping> mappings,
        string savePath,
        string toolPath,
        string value,
        string confidence,
        bool usedByCalculation,
        string note)
    {
        mappings.Add(
            new SaveVariableMapping(
                savePath,
                toolPath,
                value,
                confidence,
                usedByCalculation,
                note
            )
        );
    }

    private static int GetCraftItemCount(JsonElement craft)
    {
        if (!craft.TryGetProperty("Items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return items.GetArrayLength();
    }

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return "";
        }

        var mantissa = GetDouble(value, "Mantissa");
        var exponent = GetInt(value, "Exponent");

        return mantissa + "e" + exponent;
    }

    private static int ReadBigNumberExponent(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Object)
        {
            return 0;
        }

        return GetInt(value, "Exponent");
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

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
        }

        return result;
    }

    private static string GetString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : value.GetRawText();
    }

    private static int GetArrayCount(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return value.GetArrayLength();
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

    private static double GetDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out number))
        {
            return number;
        }

        return 0.0;
    }

    private sealed record SaveVariableMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        IReadOnlyList<SaveVariableMapping> Mappings,
        IReadOnlyList<PotentialSaveMapping> PotentialMappings,
        IReadOnlyList<string> Notes
    );

    private sealed record SaveVariableMapping(
        string SavePath,
        string ToolPath,
        string Value,
        string Confidence,
        bool UsedByCalculation,
        string Note
    );

    private sealed record PotentialSaveMapping(
        string SavePath,
        string PossibleToolPath,
        string Value,
        string Confidence,
        string Note
    );
}
