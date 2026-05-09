using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveCalculationContextCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-calculation-context .\save_export.txt .\iw_workspace_vNext [save_calculation_context.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\save_calculation_context.json";

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
        var buildingNames = LoadBuildingNames(workspacePath);

        var heroId = GetInt(root, "Hero");
        var petId = GetInt(root, "Pet");

        var className = heroEnum.TryGetValue(heroId, out var heroName)
            ? heroName
            : heroId.ToString();

        var petKey = petEnum.TryGetValue(petId, out var petName)
            ? petName
            : petId.ToString();

        var context = new SaveCalculationContextExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            Character: new CharacterCalculationContext(
                HeroId: heroId,
                ClassName: className,
                PetId: petId,
                PetKey: petKey,
                HeroMaxLevelAllTime: GetInt(root, "HeroMaxLevelAllTime"),
                MaxPetLevel: GetInt(root, "PetMaxLevel"),
                PetMaxLevelAllTime: GetInt(root, "PetMaxLevelAllTime"),
                CharExp: ReadBigNumberScientific(root, "CharExp"),
                CharExpMult: ReadBigNumberScientific(root, "CharExpMult"),
                HeroPlayedTime: GetInt(root, "HeroPlayedTime"),
                HeroSkipedPlayedTime: ReadBigNumberScientific(root, "HeroSkipedPlayedTime"),
                CurrentCharacterLevelMapped: false,
                CurrentCharacterLevelNote: "Current run character level is not directly mapped. HeroMaxLevelAllTime is all-time max; CharExp may be zero while runtime level is produced by StartingLevel/AddLevel/effects."
            ),
            Progress: BuildProgress(root),
            ImprintHeritage: BuildImprintHeritageContext(root, workspacePath),
            Resources: BuildResources(root),
            Buildings: BuildBuildings(root, buildingNames),
            Catalysts: BuildCatalysts(root, buildingNames),
            Spells: BuildSpellContext(root, spellEnum, spellCatalog),
            Craft: BuildCraftContext(root),
            Trial: BuildTrialContext(root),
            UpgradeTargets: BuildUpgradeTargetReference(),
        RealmMemoryAppliedEffects: BuildRealmMemoryAppliedEffectsReference(),
            Notes: new[]
            {
                "This is an internal calculation context generated from the save.",
                "Spellbar, spell shards, OtherSpellShards, SpellUses, and SpellUsesTR are source-confirmed from SaveData.cs.",
                "Catalyst save semantics are source-confirmed from BuildingManager.SaveCatalysts/LoadCatalysts.",
                "Trial.Completed is source-confirmed as Trial.completed, while Trial.TotalCompleted is Trial.totalCompleted.",
                "BuildingLevels are mapped by building array order and building data names.",
                "HeroMaxLevelAllTime is all-time maximum hero level, not current run character level.",
                "Current run character level is intentionally not mapped until the StartingLevel/AddLevel/runtime effect stack is modeled.",
                "UpgradeTargets are not loaded from diagnostic files by this standalone command. Build-scoped mapping outputs should be generated under runtime/builds/<build-id>.",
            }
        );

        var json = JsonSerializer.Serialize(
            context,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        File.WriteAllText(outputPath, json);

        Console.WriteLine("Save calculation context");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Save:       {savePath}");
        Console.WriteLine($"Workspace:  {workspacePath}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Class:      {context.Character.ClassName}");
        Console.WriteLine($"Pet:        {context.Character.PetKey}");
        Console.WriteLine($"Buildings:  {context.Buildings.Count}");
        Console.WriteLine($"Spellbar:   {context.Spells.Spellbar.Count}");
        Console.WriteLine($"CraftItems: {context.Craft.ItemCount}");
        Console.WriteLine($"Catalysts:  {context.Catalysts.Assignments.Count} assigned tiers");
        Console.WriteLine($"Trial:      completed={context.Trial.Completed}, totalCompleted={context.Trial.TotalCompleted}");
        Console.WriteLine($"UpgradeTargets: {context.UpgradeTargets.TargetCount} targets / {context.UpgradeTargets.UpgradeCount} upgrades");
    }

    private static ProgressCalculationContext BuildProgress(JsonElement root)
    {
        return new ProgressCalculationContext(
            Ascends: GetInt(root, "Ascends"),
            AscendsRealm: GetInt(root, "AscendsRealm"),
            TotalManaLog: ReadBigNumberExponent(root, "ManaAllTime"),
            SaveTimeUtc: GetString(root, "SaveTime")
        );
    }

    private static List<ResourceCalculationEntry> BuildResources(JsonElement root)
    {
        var names = new[]
        {
            "Mana",
            "ManaAllTime",
            "ManaRealm",
            "ManaSession",
            "VMana",
            "VoidManaAllTime",
            "VoidManaRealm",
            "VoidManaSession",
            "VoidManaCollect",
            "Souls",
            "ShardsPool",
            "ShardsTotal",
            "ShardsRealm",
            "EDE",
            "AccumCasts",
            "TimeTotal",
            "TimeRealm",
            "TimeSession",
            "TimeIdleRealm",
            "TimeOfflineRealm",
            "SkipedTimeTotal",
            "SkipedTimeRealm",
            "SkipedTimeSession",
            "AutoClicks",
            "AutoClicksTotal",
            "AutoClicksRealm",
            "Clicks",
            "ClicksRealm",
            "CastSpell",
            "CastSpellTotal",
            "CastSpellRealm",
            "TotalBuildings",
            "BoughtUpgrades",
            "ClickableCollect",
            "ClickableCollectRealm"
        };

        var result = new List<ResourceCalculationEntry>();

        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (LooksLikeBigNumber(value))
            {
                result.Add(
                    new ResourceCalculationEntry(
                        Name: name,
                        Scientific: ReadBigNumberScientific(value),
                        Mantissa: GetDouble(value, "Mantissa"),
                        Exponent: GetInt(value, "Exponent"),
                        ValueKind: "BigNumber"
                    )
                );
            }
            else
            {
                result.Add(
                    new ResourceCalculationEntry(
                        Name: name,
                        Scientific: ReadScalar(value),
                        Mantissa: 0.0,
                        Exponent: 0,
                        ValueKind: value.ValueKind.ToString()
                    )
                );
            }
        }

        return result;
    }

    private static List<BuildingCalculationEntry> BuildBuildings(
        JsonElement root,
        IReadOnlyList<string> buildingNames)
    {
        var result = new List<BuildingCalculationEntry>();

        if (!root.TryGetProperty("BuildingLevels", out var levels)
            || levels.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var index = 0;

        foreach (var item in levels.EnumerateArray())
        {
            var level = ReadIntElement(item);
            var tier = index + 1;

            var name = index < buildingNames.Count
                ? buildingNames[index]
                : "Building " + index;

            result.Add(
                new BuildingCalculationEntry(
                    Index: index,
                    Tier: tier,
                    Name: name,
                    Level: level
                )
            );

            index++;
        }

        return result;
    }

    private static CatalystCalculationContext BuildCatalysts(
        JsonElement root,
        IReadOnlyList<string> buildingNames)
    {
        if (!root.TryGetProperty("Catalysts", out var catalysts)
            || catalysts.ValueKind != JsonValueKind.Object)
        {
            return new CatalystCalculationContext(
                Totals: new CatalystTotals("", "", "", "", "", "", "", false),
                Assignments: Array.Empty<CatalystAssignmentCalculationEntry>()
            );
        }

        var totals = new CatalystTotals(
            TotalCatalysts: ReadBigNumberScientific(catalysts, "Total"),
            TotalGreenCatalysts: ReadBigNumberScientific(catalysts, "tA"),
            FreeGreenCatalysts: ReadBigNumberScientific(catalysts, "fA"),
            TotalBlueCatalysts: ReadBigNumberScientific(catalysts, "tM"),
            FreeBlueCatalysts: ReadBigNumberScientific(catalysts, "fM"),
            TotalRedCatalysts: ReadBigNumberScientific(catalysts, "tR"),
            FreeRedCatalysts: ReadBigNumberScientific(catalysts, "fR"),
            CatalystTradeIsAll: GetBool(catalysts, "IsAll")
        );

        var assignments = new List<CatalystAssignmentCalculationEntry>();

        if (catalysts.TryGetProperty("Catalysts", out var assigned)
            && assigned.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in assigned.EnumerateArray())
            {
                var tier = GetInt(record, "t");
                var buildingName = ResolveBuildingName(tier, buildingNames);

                assignments.Add(
                    new CatalystAssignmentCalculationEntry(
                        Tier: tier,
                        BuildingName: buildingName,
                        GreenCatalysts: GetStringValue(record, "a"),
                        BlueCatalysts: GetStringValue(record, "m"),
                        RedCatalysts: GetStringValue(record, "r"),
                        GreenFormulaInput: "ACatalyst",
                        BlueFormulaInput: "MCatalyst",
                        RedFormulaInput: "RCatalyst"
                    )
                );
            }
        }

        return new CatalystCalculationContext(
            Totals: totals,
            Assignments: assignments.OrderBy(x => x.Tier).ToList()
        );
    }

    private static SpellCalculationContext BuildSpellContext(
        JsonElement root,
        Dictionary<int, string> spellEnum,
        Dictionary<string, SpellCatalogEntry> spellCatalog)
    {
        var chosenSpellIds = ReadIntArray(root, "ChoosenSpells");
        var spellShards = ReadDoubleArray(root, "SpellShards");
        var autocast = ReadIntArray(root, "Autocast");
        var otherSpellShards = ReadKeyValueArray(root, "OtherSpellShards");
        var spellUses = ReadKeyValueAverageArray(root, "SpellUses");
        var spellUsesTR = ReadKeyValueAverageArray(root, "SpellUsesTR");

        var spellbar = new List<SpellbarCalculationEntry>();

        for (var position = 0; position < chosenSpellIds.Count; position++)
        {
            var spellId = chosenSpellIds[position];
            var enumKey = spellEnum.TryGetValue(spellId, out var key)
                ? key
                : spellId.ToString();

            spellCatalog.TryGetValue(enumKey, out var catalog);

            var shardProgress = position < spellShards.Count
                ? spellShards[position]
                : 0.0;

            var autocastMode = position < autocast.Count
                ? autocast[position]
                : 0;

            spellUses.TryGetValue(spellId, out var useStats);
            spellUsesTR.TryGetValue(spellId, out var runStats);

            spellbar.Add(
                new SpellbarCalculationEntry(
                    Position: position,
                    SpellId: spellId,
                    Key: enumKey,
                    Name: catalog?.Name ?? enumKey,
                    SpellType: catalog?.SpellType ?? "",
                    TypeBehavior: catalog?.TypeBehavior ?? "",
                    ShardProgress: shardProgress,
                    AutocastMode: autocastMode,
                    LifetimeUses: useStats.Value,
                    LifetimeAverage: useStats.Average,
                    ThisRunUses: runStats.Value,
                    ThisRunAverage: runStats.Average
                )
            );
        }

        var otherShards = new List<OtherSpellShardCalculationEntry>();

        foreach (var pair in otherSpellShards.OrderBy(x => x.Key))
        {
            var enumKey = spellEnum.TryGetValue(pair.Key, out var key)
                ? key
                : pair.Key.ToString();

            spellCatalog.TryGetValue(enumKey, out var catalog);

            otherShards.Add(
                new OtherSpellShardCalculationEntry(
                    SpellId: pair.Key,
                    Key: enumKey,
                    Name: catalog?.Name ?? enumKey,
                    ShardProgress: pair.Value
                )
            );
        }

        return new SpellCalculationContext(
            Spellbar: spellbar,
            OtherSpellShards: otherShards,
            AccumCasts: root.TryGetProperty("AccumCasts", out var accum) && LooksLikeBigNumber(accum)
                ? ReadBigNumberScientific(accum)
                : ""
        );
    }

    private static CraftCalculationContext BuildCraftContext(JsonElement root)
    {
        if (!root.TryGetProperty("Craft", out var craft)
            || craft.ValueKind != JsonValueKind.Object)
        {
            return new CraftCalculationContext(0, Array.Empty<CraftItemCalculationEntry>());
        }

        if (!craft.TryGetProperty("Items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return new CraftCalculationContext(0, Array.Empty<CraftItemCalculationEntry>());
        }

        var result = new List<CraftItemCalculationEntry>();

        foreach (var item in items.EnumerateArray())
        {
            result.Add(
                new CraftItemCalculationEntry(
                    ItemId: GetStringValue(item, "id"),
                    Tier: GetInt(item, "tier"),
                    Enchant: GetInt(item, "enchant"),
                    Favorite: GetBool(item, "fav"),
                    Progress: GetDouble(item, "progress")
                )
            );
        }

        return new CraftCalculationContext(
            ItemCount: result.Count,
            Items: result
        );
    }

    private static TrialCalculationContext BuildTrialContext(JsonElement root)
    {
        if (!root.TryGetProperty("Trial", out var trial)
            || trial.ValueKind != JsonValueKind.Object)
        {
            return new TrialCalculationContext(0, 0, 0, 0, 0.0, false, 0.0, false);
        }

        var patienceProgress = 0.0;
        var patienceAuto = false;

        if (trial.TryGetProperty("tol", out var tol)
            && tol.ValueKind == JsonValueKind.Object)
        {
            patienceProgress = GetDouble(tol, "Progress");
            patienceAuto = GetBool(tol, "Auto");
        }

        return new TrialCalculationContext(
            Completed: GetInt(trial, "completed"),
            TotalCompleted: GetInt(trial, "totalCompleted"),
            Tries: GetInt(trial, "tries"),
            Keys: GetInt(trial, "keys"),
            Timer: GetDouble(trial, "timer"),
            TrialOfSkillActive: GetBool(trial, "tos"),
            PatienceProgress: patienceProgress,
            PatienceAuto: patienceAuto
        );
    }

    private static RealmMemoryAppliedEffectsReference BuildRealmMemoryAppliedEffectsReference()
    {
        return new RealmMemoryAppliedEffectsReference(
            File: "",
            OwnedTargetCount: 0,
            BaseAllBuildingsProfitOwnedMultiplier: "1",
            BaseAllBuildingsProfitOwnedBonusPercent: "0",
            RealmIncomeOwnedMultiplier: "1",
            RealmIncomeOwnedBonusPercent: "0",
            FormulaStatus: "NotLoadedInStandaloneContext",
            Notes: "Realm memory applied effects are populated by --calculate-build when writing to a build calculations folder. This standalone context command does not read diagnostics folders."
        );
    }
    private static UpgradeTargetReference BuildUpgradeTargetReference()
    {
        return new UpgradeTargetReference(
            File: "",
            TargetCount: 0,
            UpgradeCount: 0,
            TopTargets: Array.Empty<UpgradeTargetReferenceEntry>(),
            Notes: "Upgrade target aggregate data is not loaded by this standalone context command. Future mapping commands should write build-scoped outputs under runtime/builds/<build-id>."
        );
    }
    private static List<int> ReadIntArray(JsonElement root, string propertyName)
    {
        var result = new List<int>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            result.Add(ReadIntElement(item));
        }

        return result;
    }

    private static List<double> ReadDoubleArray(JsonElement root, string propertyName)
    {
        var result = new List<double>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            result.Add(ReadDoubleElement(item));
        }

        return result;
    }

    private static Dictionary<int, double> ReadKeyValueArray(JsonElement root, string propertyName)
    {
        var result = new Dictionary<int, double>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            result[GetInt(item, "key")] = GetDouble(item, "value");
        }

        return result;
    }

    private static Dictionary<int, SpellUseStats> ReadKeyValueAverageArray(JsonElement root, string propertyName)
    {
        var result = new Dictionary<int, SpellUseStats>();

        if (!root.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in value.EnumerateArray())
        {
            result[GetInt(item, "key")] = new SpellUseStats(
                Value: GetDouble(item, "value"),
                Average: GetDouble(item, "aver")
            );
        }

        return result;
    }

    private static string ResolveBuildingName(int tier, IReadOnlyList<string> buildingNames)
    {
        var index = tier - 1;

        if (index >= 0 && index < buildingNames.Count)
        {
            return buildingNames[index];
        }

        return "Building Tier " + tier;
    }

    private static List<string> LoadBuildingNames(string workspacePath)
    {
        var result = new List<string>();
        var file = WorkspacePaths.FindDataFile(workspacePath, "Buildings");

        if (file is null)
        {
            return result;
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var building in doc.RootElement.EnumerateArray())
            {
                result.Add(
                    FirstNonEmpty(
                        GetString(building, "Name"),
                        GetString(building, "Key"),
                        GetString(building, "ID"),
                        "Building " + result.Count
                    )
                );
            }
        }
        catch
        {
            return new List<string>();
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

            if (pieces.Length > 1 && int.TryParse(pieces[1].Trim(), out var explicitValue))
            {
                current = explicitValue;
            }

            result[current] = key;
            current++;
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
            var key = GetString(spell, "Key");

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            result[key] = new SpellCatalogEntry(
                Key: key,
                Name: GetString(spell, "Name"),
                SpellType: GetString(spell, "SpellType"),
                TypeBehavior: GetString(spell, "TypeBehavior")
            );
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

    private static bool LooksLikeBigNumber(JsonElement value)
    {
        return value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out _)
            && value.TryGetProperty("Exponent", out _);
    }

    private static int ReadBigNumberExponent(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || !LooksLikeBigNumber(value))
        {
            return 0;
        }

        return GetInt(value, "Exponent");
    }

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return LooksLikeBigNumber(value)
            ? ReadBigNumberScientific(value)
            : ReadScalar(value);
    }

    private static string ReadBigNumberScientific(JsonElement value)
    {
        return GetDouble(value, "Mantissa") + "e" + GetInt(value, "Exponent");
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return ReadScalar(value);
    }

    private static string GetStringValue(JsonElement element, string propertyName)
    {
        return GetString(element, propertyName);
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0;
        }

        return ReadIntElement(value);
    }

    private static double GetDouble(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return 0.0;
        }

        return ReadDoubleElement(value);
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static int ReadIntElement(JsonElement value)
    {
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

    private static double ReadDoubleElement(JsonElement value)
    {
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

    private static string ReadScalar(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "",
            JsonValueKind.Object => LooksLikeBigNumber(value) ? ReadBigNumberScientific(value) : value.GetRawText(),
            JsonValueKind.Array => "Array length " + value.GetArrayLength(),
            _ => value.GetRawText()
        };
    }

    private static string FirstNonEmpty(params string[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return "";
    }


    private static ImprintHeritageCalculationContext BuildImprintHeritageContext(JsonElement root, string workspacePath)
    {
        var totalMemories = ReadIhTotalMemories(root);
        var ownedLevels = ReadIhOwnedMemoryUpgradeLevels(root);
        var realmUpgradesFile = FindIhRealmUpgradesFile(workspacePath);

        var imprints = new List<ImprintHeritageCalculationUpgrade>();
        var heritage = new List<ImprintHeritageCalculationUpgrade>();
        var notes = new List<string>
        {
            "Imprint/Heritage context is generated from save Memories state and RealmUpgrades catalog.",
            "Imprint and Heritage pools are duplicated independent pools from $.Memories.TotalMemories.",
            "Owned levels are read from $.Memories.Upgrades.",
            "Imprints are detected by RealmUpgrades Group == Imprint.",
            "Heritage records are detected by Heritage text in Name, Group, or Description.",
            "Spend formula assumption: Spend(level N) = N / 2 * (2 * Cost + (N - 1) * CostD); NextCost = Cost + N * CostD.",
            "EffectDescriptors classify raw A/M/P fields only. No final effect formula is applied here."
        };

        if (string.IsNullOrWhiteSpace(realmUpgradesFile) || !File.Exists(realmUpgradesFile))
        {
            notes.Add("RealmUpgrades.bytes was not found. Imprint/Heritage catalog records were not populated.");

            return new ImprintHeritageCalculationContext(
                TotalMemoriesPath: "$.Memories.TotalMemories",
                OwnedMemoryUpgradeLevelsPath: "$.Memories.Upgrades",
                RealmUpgradesFile: realmUpgradesFile ?? string.Empty,
                TotalMemories: totalMemories,
                PoolRule: new ImprintHeritageCalculationPoolRule(
                    ImprintPoolStartsAt: totalMemories,
                    HeritagePoolStartsAt: totalMemories,
                    PoolsAreIndependent: true
                ),
                Summary: new ImprintHeritageCalculationSummary(
                    ImprintTotalRecords: 0,
                    ImprintOwnedRecords: 0,
                    ImprintSpent: 0m,
                    ImprintRemaining: totalMemories,
                    HeritageTotalRecords: 0,
                    HeritageOwnedRecords: 0,
                    HeritageSpent: 0m,
                    HeritageRemaining: totalMemories,
                    ImprintEffectDescriptorCount: 0,
                    ImprintOwnedEffectDescriptorCount: 0,
                    HeritageEffectDescriptorCount: 0,
                    HeritageOwnedEffectDescriptorCount: 0
                ),
                Imprints: Array.Empty<ImprintHeritageCalculationUpgrade>(),
                Heritage: Array.Empty<ImprintHeritageCalculationUpgrade>(),
                ImprintEffectDescriptors: Array.Empty<ImprintHeritageEffectDescriptor>(),
                HeritageEffectDescriptors: Array.Empty<ImprintHeritageEffectDescriptor>(),
                Notes: notes
            );
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(realmUpgradesFile));

        if (doc.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var record in doc.RootElement.EnumerateArray())
            {
                var id = ReadIhInt(record, "ID");
                var name = ReadIhString(record, "Name");
                var group = ReadIhString(record, "Group");
                var description = ReadIhString(record, "Description");
                var param = ReadIhString(record, "Param");
                var add = ReadIhString(record, "A");
                var mult = ReadIhString(record, "M");
                var power = ReadIhString(record, "P");
                var reset = ReadIhString(record, "Reset");
                var switchValue = ReadIhString(record, "Switch");
                var req = ReadIhString(record, "Req");
                var maxLevel = ReadIhString(record, "MaxLvl");
                var cost = ReadIhDecimal(record, "Cost");
                var costD = ReadIhDecimal(record, "CostD");

                var isImprint = group.Equals("Imprint", StringComparison.OrdinalIgnoreCase);
                var isHeritage =
                    ContainsIhIgnoreCase(name, "Heritage") ||
                    ContainsIhIgnoreCase(group, "Heritage") ||
                    ContainsIhIgnoreCase(description, "Heritage");

                if (!isImprint && !isHeritage)
                {
                    continue;
                }

                var isOwned = ownedLevels.TryGetValue(id, out var ownedLevel);
                var level = isOwned ? ownedLevel : 0;
                var spend = GetIhSpendForLevel(cost, costD, level);
                var nextCost = GetIhNextCost(cost, costD, level);

                var entry = new ImprintHeritageCalculationUpgrade(
                    Id: id,
                    Name: name,
                    Group: group,
                    Param: param,
                    Add: add,
                    Mult: mult,
                    Power: power,
                    Reset: reset,
                    Switch: switchValue,
                    Req: req,
                    Level: level,
                    MaxLevel: maxLevel,
                    Cost: cost,
                    CostD: costD,
                    Spend: spend,
                    NextCost: nextCost,
                    IsOwned: isOwned,
                    EffectDescriptors: BuildIhEffectDescriptors(
                        id,
                        name,
                        group,
                        param,
                        add,
                        mult,
                        power,
                        level,
                        isOwned
                    )
                );

                if (isImprint)
                {
                    imprints.Add(entry);
                }

                if (isHeritage)
                {
                    heritage.Add(entry);
                }
            }
        }

        imprints = imprints.OrderBy(x => x.Id).ToList();
        heritage = heritage.OrderBy(x => x.Id).ToList();

        var imprintSpent = imprints.Where(x => x.IsOwned).Sum(x => x.Spend);
        var heritageSpent = heritage.Where(x => x.IsOwned).Sum(x => x.Spend);

        var imprintEffectDescriptors = imprints
            .SelectMany(x => x.EffectDescriptors)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Operation, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var heritageEffectDescriptors = heritage
            .SelectMany(x => x.EffectDescriptors)
            .OrderBy(x => x.Id)
            .ThenBy(x => x.Operation, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ImprintHeritageCalculationContext(
            TotalMemoriesPath: "$.Memories.TotalMemories",
            OwnedMemoryUpgradeLevelsPath: "$.Memories.Upgrades",
            RealmUpgradesFile: realmUpgradesFile,
            TotalMemories: totalMemories,
            PoolRule: new ImprintHeritageCalculationPoolRule(
                ImprintPoolStartsAt: totalMemories,
                HeritagePoolStartsAt: totalMemories,
                PoolsAreIndependent: true
            ),
            Summary: new ImprintHeritageCalculationSummary(
                ImprintTotalRecords: imprints.Count,
                ImprintOwnedRecords: imprints.Count(x => x.IsOwned),
                ImprintSpent: imprintSpent,
                ImprintRemaining: totalMemories - imprintSpent,
                HeritageTotalRecords: heritage.Count,
                HeritageOwnedRecords: heritage.Count(x => x.IsOwned),
                HeritageSpent: heritageSpent,
                HeritageRemaining: totalMemories - heritageSpent,
                ImprintEffectDescriptorCount: imprintEffectDescriptors.Count,
                ImprintOwnedEffectDescriptorCount: imprintEffectDescriptors.Count(x => x.IsOwned),
                HeritageEffectDescriptorCount: heritageEffectDescriptors.Count,
                HeritageOwnedEffectDescriptorCount: heritageEffectDescriptors.Count(x => x.IsOwned)
            ),
            Imprints: imprints,
            Heritage: heritage,
            ImprintEffectDescriptors: imprintEffectDescriptors,
            HeritageEffectDescriptors: heritageEffectDescriptors,
            Notes: notes
        );
    }

    private static IReadOnlyList<ImprintHeritageEffectDescriptor> BuildIhEffectDescriptors(
        int id,
        string name,
        string group,
        string target,
        string add,
        string mult,
        string power,
        int level,
        bool isOwned)
    {
        var result = new List<ImprintHeritageEffectDescriptor>();

        AddIhDescriptorIfPresent(result, id, name, group, target, "Add", add, level, isOwned);
        AddIhDescriptorIfPresent(result, id, name, group, target, "Mult", mult, level, isOwned);
        AddIhDescriptorIfPresent(result, id, name, group, target, "Power", power, level, isOwned);

        if (result.Count == 0)
        {
            result.Add(
                new ImprintHeritageEffectDescriptor(
                    Id: id,
                    Name: name,
                    SourceGroup: group,
                    Target: target,
                    Operation: "None",
                    RawValue: "",
                    Level: level,
                    IsOwned: isOwned,
                    FormulaStatus: "NoRawEffectField"
                )
            );
        }

        return result;
    }

    private static void AddIhDescriptorIfPresent(
        List<ImprintHeritageEffectDescriptor> result,
        int id,
        string name,
        string group,
        string target,
        string operation,
        string rawValue,
        int level,
        bool isOwned)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        result.Add(
            new ImprintHeritageEffectDescriptor(
                Id: id,
                Name: name,
                SourceGroup: group,
                Target: target,
                Operation: operation,
                RawValue: rawValue,
                Level: level,
                IsOwned: isOwned,
                FormulaStatus: "RawMappedNotApplied"
            )
        );
    }
    private static string FindIhRealmUpgradesFile(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "raw_files", "Assets", "Resources", "jsonfiles", "RealmUpgrades.bytes"),
            Path.Combine(workspacePath, "Assets", "Resources", "jsonfiles", "RealmUpgrades.bytes")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private static decimal ReadIhTotalMemories(JsonElement root)
    {
        if (!root.TryGetProperty("Memories", out var memories) ||
            memories.ValueKind != JsonValueKind.Object)
        {
            return 0m;
        }

        if (!memories.TryGetProperty("TotalMemories", out var totalMemories) ||
            totalMemories.ValueKind != JsonValueKind.Object)
        {
            return 0m;
        }

        var mantissa = ReadIhDecimal(totalMemories, "Mantissa");
        var exponent = ReadIhInt(totalMemories, "Exponent");

        return mantissa * PowIh10(exponent);
    }

    private static Dictionary<int, int> ReadIhOwnedMemoryUpgradeLevels(JsonElement root)
    {
        var result = new Dictionary<int, int>();

        if (!root.TryGetProperty("Memories", out var memories) ||
            memories.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        if (!memories.TryGetProperty("Upgrades", out var upgrades) ||
            upgrades.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in upgrades.EnumerateObject())
        {
            if (!int.TryParse(property.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
            {
                continue;
            }

            result[id] = ReadIhIntValue(property.Value);
        }

        return result;
    }

    private static decimal GetIhSpendForLevel(decimal cost, decimal costD, int level)
    {
        if (level <= 0)
        {
            return 0m;
        }

        var n = (decimal)level;
        return (n * ((2m * cost) + ((n - 1m) * costD))) / 2m;
    }

    private static decimal GetIhNextCost(decimal cost, decimal costD, int currentLevel)
    {
        if (currentLevel < 0)
        {
            currentLevel = 0;
        }

        return cost + ((decimal)currentLevel * costD);
    }

    private static bool ContainsIhIgnoreCase(string text, string value)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ReadIhString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ToString();
    }

    private static int ReadIhInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        return ReadIhIntValue(value, defaultValue);
    }

    private static int ReadIhIntValue(JsonElement value, int defaultValue = 0)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static decimal ReadIhDecimal(JsonElement element, string propertyName, decimal defaultValue = 0m)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return defaultValue;
        }

        return ReadIhDecimalValue(value, defaultValue);
    }

    private static decimal ReadIhDecimalValue(JsonElement value, decimal defaultValue = 0m)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }

    private static decimal PowIh10(int exponent)
    {
        if (exponent == 0)
        {
            return 1m;
        }

        if (exponent < 0)
        {
            var divisor = 1m;

            for (var i = 0; i < Math.Abs(exponent); i++)
            {
                divisor *= 10m;
            }

            return 1m / divisor;
        }

        var result = 1m;

        for (var i = 0; i < exponent; i++)
        {
            result *= 10m;
        }

        return result;
    }
    private sealed record SaveCalculationContextExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        CharacterCalculationContext Character,
        ProgressCalculationContext Progress,
    ImprintHeritageCalculationContext ImprintHeritage,
        IReadOnlyList<ResourceCalculationEntry> Resources,
        IReadOnlyList<BuildingCalculationEntry> Buildings,
        CatalystCalculationContext Catalysts,
        SpellCalculationContext Spells,
        CraftCalculationContext Craft,
        TrialCalculationContext Trial,
        UpgradeTargetReference UpgradeTargets,
        RealmMemoryAppliedEffectsReference RealmMemoryAppliedEffects,
        IReadOnlyList<string> Notes
    );

    private sealed record ImprintHeritageCalculationContext(
        string TotalMemoriesPath,
        string OwnedMemoryUpgradeLevelsPath,
        string RealmUpgradesFile,
        decimal TotalMemories,
        ImprintHeritageCalculationPoolRule PoolRule,
        ImprintHeritageCalculationSummary Summary,
        IReadOnlyList<ImprintHeritageCalculationUpgrade> Imprints,
        IReadOnlyList<ImprintHeritageCalculationUpgrade> Heritage,
        IReadOnlyList<ImprintHeritageEffectDescriptor> ImprintEffectDescriptors,
        IReadOnlyList<ImprintHeritageEffectDescriptor> HeritageEffectDescriptors,
        IReadOnlyList<string> Notes
    );

    private sealed record ImprintHeritageCalculationPoolRule(
        decimal ImprintPoolStartsAt,
        decimal HeritagePoolStartsAt,
        bool PoolsAreIndependent
    );

    private sealed record ImprintHeritageCalculationSummary(
        int ImprintTotalRecords,
        int ImprintOwnedRecords,
        decimal ImprintSpent,
        decimal ImprintRemaining,
        int HeritageTotalRecords,
        int HeritageOwnedRecords,
        decimal HeritageSpent,
        decimal HeritageRemaining,
        int ImprintEffectDescriptorCount,
        int ImprintOwnedEffectDescriptorCount,
        int HeritageEffectDescriptorCount,
        int HeritageOwnedEffectDescriptorCount
    );

    private sealed record ImprintHeritageCalculationUpgrade(
        int Id,
        string Name,
        string Group,
        string Param,
        string Add,
        string Mult,
        string Power,
        string Reset,
        string Switch,
        string Req,
        int Level,
        string MaxLevel,
        decimal Cost,
        decimal CostD,
        decimal Spend,
        decimal NextCost,
        bool IsOwned,
        IReadOnlyList<ImprintHeritageEffectDescriptor> EffectDescriptors
    );
    private sealed record ImprintHeritageEffectDescriptor(
        int Id,
        string Name,
        string SourceGroup,
        string Target,
        string Operation,
        string RawValue,
        int Level,
        bool IsOwned,
        string FormulaStatus
    );
    private sealed record CharacterCalculationContext(
        int HeroId,
        string ClassName,
        int PetId,
        string PetKey,
        int HeroMaxLevelAllTime,
        int MaxPetLevel,
        int PetMaxLevelAllTime,
        string CharExp,
        string CharExpMult,
        int HeroPlayedTime,
        string HeroSkipedPlayedTime,
        bool CurrentCharacterLevelMapped,
        string CurrentCharacterLevelNote
    );

    private sealed record ProgressCalculationContext(
        int Ascends,
        int AscendsRealm,
        int TotalManaLog,
        string SaveTimeUtc
    );

    private sealed record ResourceCalculationEntry(
        string Name,
        string Scientific,
        double Mantissa,
        int Exponent,
        string ValueKind
    );

    private sealed record BuildingCalculationEntry(
        int Index,
        int Tier,
        string Name,
        int Level
    );

    private sealed record CatalystCalculationContext(
        CatalystTotals Totals,
        IReadOnlyList<CatalystAssignmentCalculationEntry> Assignments
    );

    private sealed record CatalystTotals(
        string TotalCatalysts,
        string TotalGreenCatalysts,
        string FreeGreenCatalysts,
        string TotalBlueCatalysts,
        string FreeBlueCatalysts,
        string TotalRedCatalysts,
        string FreeRedCatalysts,
        bool CatalystTradeIsAll
    );

    private sealed record CatalystAssignmentCalculationEntry(
        int Tier,
        string BuildingName,
        string GreenCatalysts,
        string BlueCatalysts,
        string RedCatalysts,
        string GreenFormulaInput,
        string BlueFormulaInput,
        string RedFormulaInput
    );

    private sealed record SpellCalculationContext(
        IReadOnlyList<SpellbarCalculationEntry> Spellbar,
        IReadOnlyList<OtherSpellShardCalculationEntry> OtherSpellShards,
        string AccumCasts
    );

    private sealed record SpellbarCalculationEntry(
        int Position,
        int SpellId,
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior,
        double ShardProgress,
        int AutocastMode,
        double LifetimeUses,
        double LifetimeAverage,
        double ThisRunUses,
        double ThisRunAverage
    );

    private sealed record OtherSpellShardCalculationEntry(
        int SpellId,
        string Key,
        string Name,
        double ShardProgress
    );

    private readonly record struct SpellUseStats(
        double Value,
        double Average
    );

    private sealed record CraftCalculationContext(
        int ItemCount,
        IReadOnlyList<CraftItemCalculationEntry> Items
    );

    private sealed record CraftItemCalculationEntry(
        string ItemId,
        int Tier,
        int Enchant,
        bool Favorite,
        double Progress
    );

    private sealed record TrialCalculationContext(
        int Completed,
        int TotalCompleted,
        int Tries,
        int Keys,
        double Timer,
        bool TrialOfSkillActive,
        double PatienceProgress,
        bool PatienceAuto
    );

    private sealed record RealmMemoryAppliedEffectsReference(
        string File,
        int OwnedTargetCount,
        string BaseAllBuildingsProfitOwnedMultiplier,
        string BaseAllBuildingsProfitOwnedBonusPercent,
        string RealmIncomeOwnedMultiplier,
        string RealmIncomeOwnedBonusPercent,
        string FormulaStatus,
        string Notes
    );
    private sealed record UpgradeTargetReference(
        string File,
        int TargetCount,
        int UpgradeCount,
        IReadOnlyList<UpgradeTargetReferenceEntry> TopTargets,
        string Notes
    );

    private sealed record UpgradeTargetReferenceEntry(
        string Target,
        int PurchasedCount,
        double AdditiveSum,
        double MultiplierProduct,
        int FormulaCount
    );

    private sealed record SpellCatalogEntry(
        string Key,
        string Name,
        string SpellType,
        string TypeBehavior
    );
}

// EOF - SaveCalculationContextCommand.cs

