using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveSummaryCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-summary .\save_export.txt .\iw_workspace_vNext [save_summary.json]");
            return;
        }

        var savePath = args[1];
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_summary.json";

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

        var heroId = GetInt(root, "Hero");
        var petId = GetInt(root, "Pet");

        var heroName = heroEnum.TryGetValue(heroId, out var h) ? h : heroId.ToString();
        var petName = petEnum.TryGetValue(petId, out var p) ? p : petId.ToString();

        var chosenSpells = new List<object>();

        if (root.TryGetProperty("ChoosenSpells", out var chosen)
            && chosen.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in chosen.EnumerateArray())
            {
                var spellId = item.ValueKind == JsonValueKind.Number
                    ? item.GetInt32()
                    : -1;

                chosenSpells.Add(new
                {
                    Id = spellId,
                    Key = spellEnum.TryGetValue(spellId, out var spellName) ? spellName : spellId.ToString()
                });
            }
        }

        var interestingKeys = root
            .EnumerateObject()
            .Select(x => x.Name)
            .Where(name =>
                name.Contains("Item", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Equip", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Enchant", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Dust", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Craft", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Attribute", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Intelligence", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Insight", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Spellcraft", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Wisdom", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Dominance", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Patience", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Mastery", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Empathy", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Versatility", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToList();

        var summary = new
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            SaveFile = Path.GetFullPath(savePath),
            Workspace = workspacePath,
            ExportRoot = WorkspacePaths.ResolveExportRoot(workspacePath),

            Basic = new
            {
                SaveTime = GetString(root, "SaveTime"),
                HeroId = heroId,
                Hero = heroName,
                PetId = petId,
                Pet = petName,
                PetMaxLevel = GetInt(root, "PetMaxLevel"),
                PetMaxLevelAllTime = GetInt(root, "PetMaxLevelAllTime"),
                HeroMaxLevelAllTime = GetInt(root, "HeroMaxLevelAllTime"),
                Ascends = GetInt(root, "Ascends"),
                AscendsRealm = GetInt(root, "AscendsRealm")
            },

            Resources = new
            {
                Mana = ReadBigNumber(root, "Mana"),
                VMana = ReadBigNumber(root, "VMana"),
                Souls = ReadBigNumber(root, "Souls"),
                ManaAllTime = ReadBigNumber(root, "ManaAllTime"),
                ManaRealm = ReadBigNumber(root, "ManaRealm"),
                VoidManaAllTime = ReadBigNumber(root, "VoidManaAllTime"),
                VoidManaRealm = ReadBigNumber(root, "VoidManaRealm"),
                ShardsPool = ReadBigNumber(root, "ShardsPool"),
                EDE = ReadBigNumber(root, "EDE")
            },

            Spells = new
            {
                ChoosenSpells = chosenSpells,
                Stance = GetInt(root, "Stance"),
                AccumCasts = ReadBigNumber(root, "AccumCasts")
            },

            Progress = new
            {
                BoughtUpgrades = GetInt(root, "BoughtUpgrades"),
                UpgradesCount = GetArrayCount(root, "Upgrades"),
                BuildingLevelsCount = GetArrayCount(root, "BuildingLevels"),
                CatalystsPresent = root.TryGetProperty("Catalysts", out _)
            },

            PotentialMappingKeys = interestingKeys,

            Notes = new[]
            {
                "This file is a safe summary. It does not include the full decoded save JSON.",
                "Next step is mapping save fields to scenario globalContext, phases, spellbar, pet, attributes, equipment, and enchantments.",
                "ChoosenSpells spelling follows the game save field name."
            }
        };

        var json = JsonSerializer.Serialize(
            summary,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save summary");
        Console.WriteLine("------------");
        Console.WriteLine($"Save:        {Path.GetFullPath(savePath)}");
        Console.WriteLine($"Workspace:   {workspacePath}");
        Console.WriteLine($"Output:      {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Hero:        {heroName} ({heroId})");
        Console.WriteLine($"Pet:         {petName} ({petId})");
        Console.WriteLine($"Pet max lvl: {GetInt(root, "PetMaxLevel")}");
        Console.WriteLine($"Spells:      {chosenSpells.Count}");
        Console.WriteLine($"Map keys:    {interestingKeys.Count}");
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

        var body = match.Groups["body"].Value;
        var current = 0;

        foreach (var raw in body.Split(','))
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

    private static object? ReadBigNumber(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var mantissa = GetDouble(value, "Mantissa");
            var exponent = GetInt(value, "Exponent");

            return new
            {
                Mantissa = mantissa,
                Exponent = exponent,
                Scientific = $"{mantissa}e{exponent}"
            };
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetRawText();
        }

        return value.ToString();
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
}
