using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveProgressionContextCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-progression-context .\save_export.txt .\progression_context.json");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var outputPath = Path.GetFullPath(args[2]);

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var export = new ProgressionContextExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Character: BuildCharacterSummary(root),
            Realm: BuildRealmSummary(root),
            Paragon: BuildParagonSummary(root),
            Memories: BuildMemorySummary(root),
            Attributes: BuildAttributeSummary(root),
            Pantheon: BuildPantheonSummary(root),
            Pets: BuildPetSummary(root),
            Familiars: BuildFamiliarSummary(root),
            TrialsAndAchievements: BuildTrialsAndAchievementsSummary(root),
            MappingCoverage: BuildCoverageSummary(root),
            Notes: new[]
            {
                "Phase 2 progression context map. This command intentionally summarizes foundational systems before final VM/burst formulas are implemented.",
                "This command does not yet calculate VM or burst output. It maps save-state inputs needed by later calculation layers.",
                "Effect formulas are not invented here. Fields are surfaced so follow-up mappers can bind source-confirmed effects later."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        File.WriteAllText(outputPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Save progression context");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Save: {savePath}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine($"Class/Hero ID: {export.Character.HeroId}");
        Console.WriteLine($"Realm/Legacy: {export.Realm.Active}");
        Console.WriteLine($"Paragon: {export.Paragon.Level}");
        Console.WriteLine($"Memories: {export.Memories.MemoryUpgradeCount} upgrades");
        Console.WriteLine($"Attributes: total {export.Attributes.AttTotal}, free {export.Attributes.AttFree}");
        Console.WriteLine($"Pantheon: {export.Pantheon.ChooseCount} chosen gods");
        Console.WriteLine($"PetStartingLevel: {export.Pets.PetStartingLevel} ({export.Pets.PetStartingLevelSource})");
        Console.WriteLine($"Familiars: {export.Familiars.FamiliarCount} known, {export.Familiars.ActiveSlotCount} active slots");
        Console.WriteLine($"Coverage: {export.MappingCoverage.Mapped.Count} mapped/summarized, {export.MappingCoverage.NeedsEffectMapping.Count} needs effect mapping");
    }

    private static CharacterProgressionSummary BuildCharacterSummary(JsonElement root)
    {
        return new CharacterProgressionSummary(
            HeroId: GetInt(root, "Hero"),
            PetId: GetInt(root, "Pet"),
            Ascends: GetInt(root, "Ascends"),
            AscendsRealm: GetInt(root, "AscendsRealm"),
            HeroMaxLevelAllTime: GetInt(root, "HeroMaxLevelAllTime"),
            HeroPlayedTime: GetInt(root, "HeroPlayedTime"),
            HeroSkipedPlayedTime: ReadBigNumberScientific(root, "HeroSkipedPlayedTime"),
            SaveVersion: GetInt(root, "SaveVersion"),
            SaveTime: GetString(root, "SaveTime", "")
        );
    }

    private static RealmSummary BuildRealmSummary(JsonElement root)
    {
        if (!root.TryGetProperty("Realm", out var realm) || realm.ValueKind != JsonValueKind.Object)
        {
            return new RealmSummary("", 0, 0, 0, false, "Realm object missing");
        }

        var historyCount = realm.TryGetProperty("History", out var history) && history.ValueKind == JsonValueKind.Array
            ? history.GetArrayLength()
            : 0;

        var completedCount = realm.TryGetProperty("Completed", out var completed) && completed.ValueKind == JsonValueKind.Object
            ? completed.EnumerateObject().Count()
            : 0;

        var paramnesicCount = realm.TryGetProperty("Paramnesics", out var paramnesics) && paramnesics.ValueKind == JsonValueKind.Object
            ? paramnesics.EnumerateObject().Count()
            : 0;

        return new RealmSummary(
            Active: GetString(realm, "Active", ""),
            CompletedCount: completedCount,
            ParamnesicCount: paramnesicCount,
            HistoryCount: historyCount,
            HasRealmData: realm.TryGetProperty("RealmData", out var realmData) && realmData.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(realmData.GetString()),
            Notes: "Realm/legacy state is summarized only. Effect formulas still need source mapping."
        );
    }

    private static ParagonSummary BuildParagonSummary(JsonElement root)
    {
        return new ParagonSummary(
            Level: GetInt(root, "Paragon"),
            Notes: "Paragon level is surfaced. Unlock/effect mapping still needs source-confirmed binding."
        );
    }

    private static MemorySummary BuildMemorySummary(JsonElement root)
    {
        if (!root.TryGetProperty("Memories", out var memories) || memories.ValueKind != JsonValueKind.Object)
        {
            return MemorySummary.Empty("Memories object missing");
        }

        var upgrades = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (memories.TryGetProperty("Upgrades", out var upgradeElement) && upgradeElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in upgradeElement.EnumerateObject())
            {
                upgrades[property.Name] = property.Value.ValueKind == JsonValueKind.Number ? property.Value.GetInt32() : 0;
            }
        }

        var carryOver = memories.TryGetProperty("CarryOver", out var carry) && carry.ValueKind == JsonValueKind.Object
            ? BuildCarryOverSummary(carry)
            : CarryOverSummary.Empty();

        var memorySetCount = memories.TryGetProperty("MemorySets", out var sets) && sets.ValueKind == JsonValueKind.Array
            ? sets.GetArrayLength()
            : 0;

        return new MemorySummary(
            Realms: GetInt(memories, "Realms"),
            Memories: ReadBigNumberScientific(memories, "Memories"),
            SwitchMemories: ReadBigNumberScientific(memories, "SwitchMemories"),
            TotalMemories: ReadBigNumberScientific(memories, "TotalMemories"),
            MemoryUpgradeCount: upgrades.Count,
            Upgrades: upgrades,
            MemorySetCount: memorySetCount,
            CarryOver: carryOver,
            Notes: "Memories are summarized. Upgrade IDs and carryover fields still need effect mapping."
        );
    }

    private static CarryOverSummary BuildCarryOverSummary(JsonElement carry)
    {
        return new CarryOverSummary(
            Breakthroughs: GetInt(carry, "breakthroughs"),
            Trophies: GetInt(carry, "trophies"),
            PersistCount: CountObjectProperties(carry, "persist"),
            CatasCount: CountObjectProperties(carry, "catas"),
            EchoExpCount: CountObjectProperties(carry, "echoExp"),
            Attributes: GetInt(carry, "attributes")
        );
    }

    private static AttributeSummary BuildAttributeSummary(JsonElement root)
    {
        return new AttributeSummary(
            AttTotal: GetInt(root, "AttTotal"),
            AttSearched: GetInt(root, "AttSearched"),
            AttFree: GetInt(root, "AttFree"),
            AttResets: GetInt(root, "AttResets"),
            AttProgress: GetDouble(root, "AttProgress", 0.0),
            Int: GetInt(root, "Int"),
            Ins: GetInt(root, "Ins"),
            Scr: GetInt(root, "Scr"),
            Wis: GetInt(root, "Wis"),
            Dom: GetInt(root, "Dom"),
            Pat: GetInt(root, "Pat"),
            Mas: GetInt(root, "Mas"),
            Emp: GetInt(root, "Emp"),
            Ver: GetInt(root, "Ver"),
            Notes: "Attributes are surfaced as raw save values. Attribute-to-effect mapping is next."
        );
    }

    private static PantheonSummary BuildPantheonSummary(JsonElement root)
    {
        if (!root.TryGetProperty("Pantheon", out var pantheon) || pantheon.ValueKind != JsonValueKind.Object)
        {
            return PantheonSummary.Empty("Pantheon object missing");
        }

        var chooses = ReadIntArray(pantheon, "chooses");

        return new PantheonSummary(
            ChooseCount: chooses.Count,
            Chooses: chooses,
            ExpGodCount: CountObjectProperties(pantheon, "exp"),
            MaxLevelGodCount: CountObjectProperties(pantheon, "maxLvls"),
            MaxLevelRealmGodCount: CountObjectProperties(pantheon, "maxLvlsRealm"),
            MajorMaxLevel: GetInt(pantheon, "majorMaxLvl"),
            MinorMaxLevel: GetInt(pantheon, "minorMaxLvl"),
            PrayingExp: ReadBigNumberScientific(pantheon, "prayingExp"),
            Temple: pantheon.TryGetProperty("temple", out var temple) && temple.ValueKind == JsonValueKind.Object ? BuildTempleSummary(temple) : TempleSummary.Empty(),
            Notes: "Pantheon save state is summarized. God IDs and temple effects still need effect mapping."
        );
    }

    private static TempleSummary BuildTempleSummary(JsonElement temple)
    {
        return new TempleSummary(
            Buff: GetInt(temple, "buff"),
            Xp: GetInt(temple, "xp"),
            Power: GetInt(temple, "power"),
            BuffTotal: GetInt(temple, "buffTotal"),
            XpTotal: GetInt(temple, "xpTotal"),
            PowerTotal: GetInt(temple, "powerTotal"),
            BuffsCount: CountObjectProperties(temple, "buffs"),
            XpsCount: CountObjectProperties(temple, "xps"),
            PowersCount: CountObjectProperties(temple, "powers"),
            PowersSecCount: CountObjectProperties(temple, "powersSec")
        );
    }

    private static PetSummary BuildPetSummary(JsonElement root)
    {
        var petExp = ReadBigNumberScientific(root, "PetExp");
        var petMaxLevel = GetInt(root, "PetMaxLevel");
        var inferredStartingLevel = InferPetStartingLevelFromPetExpAndMaxLevel(petExp, petMaxLevel, growBase: 1.2);
        var startingLevelSource = inferredStartingLevel >= 0
            ? "inferred_from_PetExp_and_PetMaxLevel_using_Pet.RecalculateLevel_inverse.ValidatedAgainstPetMaxLevel"
            : "unresolved";

        return new PetSummary(
            PetId: GetInt(root, "Pet"),
            PetExp: petExp,
            PetMaxLevel: petMaxLevel,
            PetMaxLevelAllTime: GetInt(root, "PetMaxLevelAllTime"),
            PetPlayedTime: GetInt(root, "PetPlayedTime"),
            PetSkipedPlayedTime: ReadBigNumberScientific(root, "PetSkipedPlayedTime"),
            PetStartingLevel: inferredStartingLevel,
            PetStartingLevelSource: startingLevelSource,
            Notes: "Pet state is surfaced separately from familiars. PetStartingLevel is inferred from PetExp and PetMaxLevel until the runtime Pet.StartingLvl effect stack is mapped."
        );
    }

    private static FamiliarSummary BuildFamiliarSummary(JsonElement root)
    {
        if (!root.TryGetProperty("Familiars", out var familiars) || familiars.ValueKind != JsonValueKind.Object)
        {
            return FamiliarSummary.Empty("Familiars object missing");
        }

        var familiarCount = familiars.TryGetProperty("familiars", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.GetArrayLength()
            : 0;

        var activeSlots = ReadIntArray(familiars, "activeSlots");

        return new FamiliarSummary(
            FamiliarCount: familiarCount,
            ActiveSlotCount: activeSlots.Count(x => x >= 0),
            ActiveSlots: activeSlots,
            Dust: ReadBigNumberScientific(familiars, "dust"),
            FoodAmountCount: familiars.TryGetProperty("foodAmounts", out var food) && food.ValueKind == JsonValueKind.Array ? food.GetArrayLength() : 0,
            Notes: "Familiars are separate from pets. Familiar passive/rank/feeding effects still need source mapping."
        );
    }

    private static TrialsAndAchievementsSummary BuildTrialsAndAchievementsSummary(JsonElement root)
    {
        return new TrialsAndAchievementsSummary(
            CompletedChallengeCount: root.TryGetProperty("CompletedChIDs", out var completedChallenges) && completedChallenges.ValueKind == JsonValueKind.Array ? completedChallenges.GetArrayLength() : 0,
            AchievementCount: root.TryGetProperty("AchievementsSave", out var achievements) && achievements.ValueKind == JsonValueKind.Array ? achievements.GetArrayLength() : 0,
            RealmAchievementCount: root.TryGetProperty("RAchieves", out var realmAchievements) && realmAchievements.ValueKind == JsonValueKind.Array ? realmAchievements.GetArrayLength() : 0,
            TrialCompleted: root.TryGetProperty("Trial", out var trial) && trial.ValueKind == JsonValueKind.Object ? GetInt(trial, "completed") : 0,
            TrialTotalCompleted: root.TryGetProperty("Trial", out var trial2) && trial2.ValueKind == JsonValueKind.Object ? GetInt(trial2, "totalCompleted") : 0,
            TriumphFailCount: CountNestedArray(root, new[] { "Triumphs", "TriumphFail" }),
            Notes: "Challenges have partial XP baseline handling elsewhere. Achievement/trial/triumph effects still need mapping."
        );
    }

    private static MappingCoverageSummary BuildCoverageSummary(JsonElement root)
    {
        var mapped = new List<string>
        {
            "Save import / decode",
            "Character summary",
            "Realm raw state summary",
            "Paragon raw level summary",
            "Memories raw state summary",
            "Attributes raw state summary",
            "Pantheon raw state summary",
            "Pet raw state summary",
            "PetStartingLevel inferred from PetExp and PetMaxLevel",
            "Familiars raw state summary",
            "Trials / achievements raw state summary"
        };

        var needs = new List<string>
        {
            "Realm/legacy effect formulas",
            "Paragon unlock/effect formulas",
            "Memory upgrade effect formulas",
            "Memory carryover/persist effect formulas",
            "Attribute effect formulas",
            "Pantheon/god effect formulas",
            "Temple scripture/tenet/offering effects",
            "Pet effect formulas",
            "Runtime Pet.StartingLvl effect stack",
            "Familiar rank/passive/feeding effects",
            "Achievement/trial/triumph effect formulas",
            "Spell effects per phase",
            "Item enchant scaling",
            "Ascension / enhanced third-tier class effects",
            "Void Mana final calculation",
            "Burst final calculation"
        };

        var unknown = new List<string>();

        foreach (var candidate in new[] { "Gilding", "Card", "Shop", "Catalysts", "Ascention", "Craft", "Interior", "EventSave", "Quests" })
        {
            if (root.TryGetProperty(candidate, out _))
            {
                unknown.Add(candidate);
            }
        }

        return new MappingCoverageSummary(mapped, needs, unknown);
    }

    private static string DecodeSaveString(string input)
    {
        var clean = new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        var bytes = Convert.FromBase64String(clean);

        using var inputStream = new MemoryStream(bytes);
        using var gzip = new GZipStream(inputStream, CompressionMode.Decompress);
        using var output = new MemoryStream();

        gzip.CopyTo(output);

        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static int CountObjectProperties(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value.EnumerateObject().Count()
            : 0;
    }

    private static int CountNestedArray(JsonElement root, IReadOnlyList<string> path)
    {
        var element = root;

        foreach (var part in path)
        {
            if (!element.TryGetProperty(part, out element))
            {
                return 0;
            }
        }

        return element.ValueKind == JsonValueKind.Array ? element.GetArrayLength() : 0;
    }

    private static List<int> ReadIntArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return new List<int>();
        }

        return value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).ToList();
    }

    private static string ReadBigNumberScientific(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            return value.GetDouble().ToString("G17", CultureInfo.InvariantCulture);
        }

        if (value.ValueKind == JsonValueKind.Object
            && value.TryGetProperty("Mantissa", out var mantissa)
            && value.TryGetProperty("Exponent", out var exponent))
        {
            return $"{mantissa.GetDouble().ToString("G17", CultureInfo.InvariantCulture)}e{exponent.GetInt32().ToString(CultureInfo.InvariantCulture)}";
        }

        return value.ToString();
    }

    private static int InferPetStartingLevelFromPetExpAndMaxLevel(string petExpText, int petMaxLevel, double growBase)
    {
        if (petMaxLevel <= 0)
        {
            return -1;
        }

        var petExp = ParseScientificDouble(petExpText);

        if (petExp <= 0.0 || growBase <= 1.0)
        {
            return -1;
        }

        for (var startingLevel = 0; startingLevel <= 5000; startingLevel++)
        {
            var derivedLevel = DerivePetLevelFromExp(petExp, growBase, startingLevel);

            if (derivedLevel == petMaxLevel)
            {
                return startingLevel;
            }

            if (startingLevel > petMaxLevel && derivedLevel > petMaxLevel + 1)
            {
                break;
            }
        }

        return -1;
    }

    private static int DerivePetLevelFromExp(double petExp, double growBase, int startingLevel)
    {
        const double baseXp = 100.0;

        if (petExp < 0.0 || growBase <= 1.0)
        {
            return 0;
        }

        var startingExp = baseXp * (1.0 - Math.Pow(growBase, startingLevel)) / (1.0 - growBase);
        var argument = 1.0 - ((startingExp + petExp) * (1.0 - growBase) / baseXp);

        if (argument <= 0.0 || double.IsNaN(argument) || double.IsInfinity(argument))
        {
            return 0;
        }

        var levelBase = Math.Floor(Math.Log(argument) / Math.Log(growBase));

        if (levelBase < 0.0)
        {
            levelBase = 0.0;
        }

        return (int)levelBase + 1;
    }

    private static double ParseScientificDouble(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0.0;
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0.0;
    }

    private static string GetString(JsonElement root, string propertyName, string defaultValue)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? defaultValue
            : defaultValue;
    }

    private static int GetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : defaultValue;
    }

    private static double GetDouble(JsonElement root, string propertyName, double defaultValue)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : defaultValue;
    }

    private sealed record ProgressionContextExport(
        string GeneratedAtUtc,
        string SaveFile,
        CharacterProgressionSummary Character,
        RealmSummary Realm,
        ParagonSummary Paragon,
        MemorySummary Memories,
        AttributeSummary Attributes,
        PantheonSummary Pantheon,
        PetSummary Pets,
        FamiliarSummary Familiars,
        TrialsAndAchievementsSummary TrialsAndAchievements,
        MappingCoverageSummary MappingCoverage,
        IReadOnlyList<string> Notes);

    private sealed record CharacterProgressionSummary(
        int HeroId,
        int PetId,
        int Ascends,
        int AscendsRealm,
        int HeroMaxLevelAllTime,
        int HeroPlayedTime,
        string HeroSkipedPlayedTime,
        int SaveVersion,
        string SaveTime);

    private sealed record RealmSummary(
        string Active,
        int CompletedCount,
        int ParamnesicCount,
        int HistoryCount,
        bool HasRealmData,
        string Notes);

    private sealed record ParagonSummary(
        int Level,
        string Notes);

    private sealed record MemorySummary(
        int Realms,
        string Memories,
        string SwitchMemories,
        string TotalMemories,
        int MemoryUpgradeCount,
        IReadOnlyDictionary<string, int> Upgrades,
        int MemorySetCount,
        CarryOverSummary CarryOver,
        string Notes)
    {
        public static MemorySummary Empty(string notes) => new(
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            new Dictionary<string, int>(),
            0,
            CarryOverSummary.Empty(),
            notes);
    }

    private sealed record CarryOverSummary(
        int Breakthroughs,
        int Trophies,
        int PersistCount,
        int CatasCount,
        int EchoExpCount,
        int Attributes)
    {
        public static CarryOverSummary Empty() => new(0, 0, 0, 0, 0, 0);
    }

    private sealed record AttributeSummary(
        int AttTotal,
        int AttSearched,
        int AttFree,
        int AttResets,
        double AttProgress,
        int Int,
        int Ins,
        int Scr,
        int Wis,
        int Dom,
        int Pat,
        int Mas,
        int Emp,
        int Ver,
        string Notes);

    private sealed record PantheonSummary(
        int ChooseCount,
        IReadOnlyList<int> Chooses,
        int ExpGodCount,
        int MaxLevelGodCount,
        int MaxLevelRealmGodCount,
        int MajorMaxLevel,
        int MinorMaxLevel,
        string PrayingExp,
        TempleSummary Temple,
        string Notes)
    {
        public static PantheonSummary Empty(string notes) => new(
            0,
            Array.Empty<int>(),
            0,
            0,
            0,
            0,
            0,
            string.Empty,
            TempleSummary.Empty(),
            notes);
    }

    private sealed record TempleSummary(
        int Buff,
        int Xp,
        int Power,
        int BuffTotal,
        int XpTotal,
        int PowerTotal,
        int BuffsCount,
        int XpsCount,
        int PowersCount,
        int PowersSecCount)
    {
        public static TempleSummary Empty() => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed record PetSummary(
        int PetId,
        string PetExp,
        int PetMaxLevel,
        int PetMaxLevelAllTime,
        int PetPlayedTime,
        string PetSkipedPlayedTime,
        int PetStartingLevel,
        string PetStartingLevelSource,
        string Notes);

    private sealed record FamiliarSummary(
        int FamiliarCount,
        int ActiveSlotCount,
        IReadOnlyList<int> ActiveSlots,
        string Dust,
        int FoodAmountCount,
        string Notes)
    {
        public static FamiliarSummary Empty(string notes) => new(
            0,
            0,
            Array.Empty<int>(),
            string.Empty,
            0,
            notes);
    }

    private sealed record TrialsAndAchievementsSummary(
        int CompletedChallengeCount,
        int AchievementCount,
        int RealmAchievementCount,
        int TrialCompleted,
        int TrialTotalCompleted,
        int TriumphFailCount,
        string Notes);

    private sealed record MappingCoverageSummary(
        IReadOnlyList<string> Mapped,
        IReadOnlyList<string> NeedsEffectMapping,
        IReadOnlyList<string> UnknownOrDeferredSaveObjects);
}

// EOF - SaveProgressionContextCommand.cs