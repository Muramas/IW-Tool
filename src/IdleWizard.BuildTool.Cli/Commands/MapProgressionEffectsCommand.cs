using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class MapProgressionEffectsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --map-progression-effects .\save_export.txt .\iw_workspace_vNext .\progression_effect_map.json");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = Path.GetFullPath(args[3]);

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var rawFilesPath = WorkspacePaths.ResolveSourceSearchRoot(workspacePath);
        if (!Directory.Exists(rawFilesPath))
        {
            Console.WriteLine($"source snapshot folder not found: {rawFilesPath}");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));
        using var doc = JsonDocument.Parse(saveJson);
        var root = doc.RootElement;

        var context = BuildSaveContext(root);
        var fileIndex = LoadSearchableFiles(rawFilesPath);
        var systems = BuildSystems(context);
        var mappedSystems = new List<ProgressionEffectSystemMap>();

        foreach (var system in systems)
        {
            mappedSystems.Add(MapSystem(system, fileIndex));
        }

        var export = new ProgressionEffectMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            RawFiles: rawFilesPath,
            SaveContext: context,
            Systems: mappedSystems,
            Notes: new[]
            {
                "Phase 2 source-discovery map. This command finds likely source/data files and snippets for progression systems.",
                "This command does not apply formulas yet. It creates a roadmap artifact for source-confirmed effect binding.",
                "Hits are lexical/source candidates. Treat a hit as mapped only after the target/formula is verified in the returned source path/snippet."
            }
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine("Map progression effects");
        Console.WriteLine("-----------------------");
        Console.WriteLine($"Save:      {savePath}");
        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Raw files: {rawFilesPath}");
        Console.WriteLine($"Output:    {outputPath}");
        Console.WriteLine("");

        foreach (var system in mappedSystems)
        {
            Console.WriteLine($"{system.SystemName}");
            Console.WriteLine($"  Save keys:        {system.SaveKeys.Count}");
            Console.WriteLine($"  Search terms:     {system.SearchTerms.Count}");
            Console.WriteLine($"  Source hits:      {system.SourceHits.Count}");
            Console.WriteLine($"  Data hits:        {system.DataHits.Count}");
            Console.WriteLine($"  Needs mapping:    {system.NeedsEffectMapping}");
            Console.WriteLine("");
        }
    }

    private static ProgressionEffectSystemMap MapSystem(ProgressionSystemSearch system, IReadOnlyList<SearchableFile> fileIndex)
    {
        var sourceHits = new List<ProgressionSourceHit>();
        var dataHits = new List<ProgressionSourceHit>();

        foreach (var file in fileIndex)
        {
            var termsFound = system.SearchTerms
                .Where(term => file.Text.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            if (termsFound.Count == 0)
            {
                continue;
            }

            var snippets = BuildSnippets(file, termsFound, maxSnippets: 1);
            var hit = new ProgressionSourceHit(
                RelativePath: file.RelativePath,
                FileKind: ClassifyFileKind(file.RelativePath),
                MatchedTerms: termsFound,
                Snippets: snippets
            );

            if (file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                sourceHits.Add(hit);
            }
            else
            {
                dataHits.Add(hit);
            }
        }

        return new ProgressionEffectSystemMap(
            SystemName: system.SystemName,
            Priority: system.Priority,
            SaveKeys: system.SaveKeys,
            SearchTerms: system.SearchTerms,
            SourceHits: sourceHits.OrderByDescending(x => x.MatchedTerms.Count).ThenBy(x => x.RelativePath).Take(4).ToList(),
            DataHits: dataHits.OrderByDescending(x => x.MatchedTerms.Count).ThenBy(x => x.RelativePath).Take(4).ToList(),
            NeedsEffectMapping: true,
            Notes: system.Notes
        );
    }

    private static List<ProgressionSourceSnippet> BuildSnippets(SearchableFile file, IReadOnlyList<string> terms, int maxSnippets)
    {
        var snippets = new List<ProgressionSourceSnippet>();
        var lines = file.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var term in terms)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var start = Math.Max(0, i - 2);
                var end = Math.Min(lines.Length - 1, i + 3);
                var builder = new StringBuilder();

                for (var j = start; j <= end; j++)
                {
                    builder.Append(j + 1).Append(": ").AppendLine(lines[j]);
                }

                var snippetText = builder.ToString().TrimEnd();

                if (snippetText.Length > 180)
                {
                    snippetText = snippetText[..180] + "...";
                }

                snippets.Add(new ProgressionSourceSnippet(term, i + 1, snippetText));

                if (snippets.Count >= maxSnippets)
                {
                    return snippets;
                }

                break;
            }
        }

        return snippets;
    }

    private static IReadOnlyList<SearchableFile> LoadSearchableFiles(string rawFilesPath)
    {
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".cs",
            ".txt",
            ".json",
            ".bytes",
            ".xml"
        };

        var files = new List<SearchableFile>();

        foreach (var path in Directory.EnumerateFiles(rawFilesPath, "*", SearchOption.AllDirectories))
        {
            var extension = Path.GetExtension(path);
            if (!allowedExtensions.Contains(extension))
            {
                continue;
            }

            try
            {
                var info = new FileInfo(path);
                if (info.Length > 8_000_000)
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(rawFilesPath, path).Replace('\\', '/');

                if (relativePath.StartsWith("Assets/Resources/translation/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isSource = relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
                var isGameData = relativePath.Contains("Assets/Resources/jsonfiles/", StringComparison.OrdinalIgnoreCase);

                if (!isSource && !isGameData)
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                files.Add(new SearchableFile(relativePath, text));
            }
            catch
            {
                // Ignore unreadable extracted files. The export will still show hits from readable files.
            }
        }

        return files;
    }

    private static SaveProgressionSearchContext BuildSaveContext(JsonElement root)
    {
        var memoryUpgradeIds = new List<string>();
        if (root.TryGetProperty("Memories", out var memories)
            && memories.ValueKind == JsonValueKind.Object
            && memories.TryGetProperty("Upgrades", out var upgrades)
            && upgrades.ValueKind == JsonValueKind.Object)
        {
            memoryUpgradeIds = upgrades.EnumerateObject().Select(x => x.Name).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        var pantheonChooses = new List<int>();
        if (root.TryGetProperty("Pantheon", out var pantheon) && pantheon.ValueKind == JsonValueKind.Object)
        {
            pantheonChooses = ReadIntArray(pantheon, "chooses");
        }

        return new SaveProgressionSearchContext(
            HeroId: GetInt(root, "Hero"),
            PetId: GetInt(root, "Pet"),
            RealmActive: GetNestedString(root, new[] { "Realm", "Active" }, ""),
            Paragon: GetInt(root, "Paragon"),
            Ascends: GetInt(root, "Ascends"),
            AscendsRealm: GetInt(root, "AscendsRealm"),
            MemoryUpgradeIds: memoryUpgradeIds,
            PantheonChooses: pantheonChooses,
            FamiliarActiveSlots: root.TryGetProperty("Familiars", out var familiars) && familiars.ValueKind == JsonValueKind.Object ? ReadIntArray(familiars, "activeSlots") : new List<int>(),
            CompletedChallengeCount: root.TryGetProperty("CompletedChIDs", out var completed) && completed.ValueKind == JsonValueKind.Array ? completed.GetArrayLength() : 0,
            AchievementCount: root.TryGetProperty("AchievementsSave", out var achievements) && achievements.ValueKind == JsonValueKind.Array ? achievements.GetArrayLength() : 0,
            RealmAchievementCount: root.TryGetProperty("RAchieves", out var realmAchievements) && realmAchievements.ValueKind == JsonValueKind.Array ? realmAchievements.GetArrayLength() : 0
        );
    }

    private static IReadOnlyList<ProgressionSystemSearch> BuildSystems(SaveProgressionSearchContext context)
    {
        var systems = new List<ProgressionSystemSearch>
        {
            new(
                "Realm / Legacy / Paragon",
                1,
                new[] { "Realm", "Realm.Active", "Realm.Completed", "Realm.Paramnesics", "Paragon", "Ascends", "AscendsRealm" },
                BuildTerms("Realm", "Realmcraft", "Legacy", "Paramnesic", "Paramnesics", "Paragon", "PermanentParagons", "Ascends", "AscendsRealm", context.RealmActive),
                "First target per roadmap. Map active realm/legacy and paragon unlock/effect formulas before final VM/Burst calculations."),
            new(
                "Memories",
                2,
                new[] { "Memories", "Memories.Upgrades", "Memories.CarryOver" },
                BuildTerms("Memories", "Memory", "MemorySets", "CarryOver", "SwitchMemories", "TotalMemories", "Breakthroughs", "Trophies", "persist", "echoExp", "RealmManager", "RealmcraftManager"),
                "Map memory upgrade IDs and carryover/persist formulas."),
            new(
                "Attributes",
                3,
                new[] { "Int", "Ins", "Scr", "Wis", "Dom", "Pat", "Mas", "Emp", "Ver", "AttTotal" },
                BuildTerms("Attributes", "Attribute", "Intelligence", "Insight", "Spellcraft", "Wisdom", "Dominance", "Patience", "Mastery", "Empathy", "Versatility", "AttTotal", "AttFree"),
                "Map attribute names and formulas to effect resources."),
            new(
                "Pantheon / Gods / Temple",
                4,
                new[] { "Pantheon", "Pantheon.chooses", "Pantheon.temple" },
                BuildTerms("Pantheon", "Gods", "MainGod", "MinorGod", "Temple", "Praying", "minorExp", "minorSpells", "minorVoid", "minorIdle", "Scripture", "Tenet", "Offering"),
                "Map active chosen gods, main/minor effects, and temple spend effects."),
            new(
                "Pet Effects",
                5,
                new[] { "Pet", "PetExp", "PetMaxLevel" },
                BuildTerms("Pet", "Pets", "PetExp", "PetMaxLevel", "PetLevel", "CurrentPet", "Pet.AbilityPower", "Pet.BonusExp", context.PetId.ToString()),
                "Map selected pet and pet-level effects. Pets are separate from familiars."),
            new(
                "Familiars",
                6,
                new[] { "Familiars", "Familiars.activeSlots" },
                BuildTerms("Familiar", "Familiars", "Reliquary", "Reliquaries", "FedStatus", "activeSlots", "Familiar", "Rank"),
                "Map familiar active slot/rank/passive/feeding effects. Familiars are not pets."),
            new(
                "Trials / Achievements / Triumphs / Challenges",
                7,
                new[] { "AchievementsSave", "RAchieves", "Trial", "Triumphs", "CompletedChIDs" },
                BuildTerms("Achievement", "Achievements", "RAchieves", "Trial", "Trials", "Triumph", "Triumphs", "Challenge", "Challenges", "CompletedChIDs"),
                "Map leftover permanent modifiers after progression systems."),
            new(
                "Deferred Systems Coverage",
                8,
                new[] { "Gilding", "Card", "Shop", "Catalysts", "Ascention", "Craft", "Interior", "EventSave", "Quests" },
                BuildTerms("Gilding", "Card", "Shop", "Catalysts", "Ascention", "Ascension", "Craft", "Interior", "Event", "Quests", "Mount", "Weapon", "Mythics"),
                "Coverage pass for systems intentionally deferred before spells/enchants/VM/burst."),
        };

        return systems;
    }

    private static IReadOnlyList<string> BuildTerms(params string[] terms)
    {
        return terms.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static IReadOnlyList<string> BuildTerms(IEnumerable<string> terms)
    {
        return terms.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ClassifyFileKind(string relativePath)
    {
        if (relativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return "Source";
        if (relativePath.Contains("jsonfiles", StringComparison.OrdinalIgnoreCase)) return "GameData";
        if (relativePath.Contains("Resources", StringComparison.OrdinalIgnoreCase)) return "Resource";
        return "Data";
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

    private static List<int> ReadIntArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array) return new List<int>();
        return value.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number).Select(x => x.GetInt32()).ToList();
    }

    private static int GetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : defaultValue;
    }

    private static string GetNestedString(JsonElement root, IReadOnlyList<string> path, string defaultValue)
    {
        var element = root;
        foreach (var part in path)
        {
            if (!element.TryGetProperty(part, out element)) return defaultValue;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() ?? defaultValue : element.ToString();
    }

    private sealed record ProgressionEffectMapExport(string GeneratedAtUtc, string SaveFile, string Workspace, string RawFiles, SaveProgressionSearchContext SaveContext, IReadOnlyList<ProgressionEffectSystemMap> Systems, IReadOnlyList<string> Notes);
    private sealed record SaveProgressionSearchContext(int HeroId, int PetId, string RealmActive, int Paragon, int Ascends, int AscendsRealm, IReadOnlyList<string> MemoryUpgradeIds, IReadOnlyList<int> PantheonChooses, IReadOnlyList<int> FamiliarActiveSlots, int CompletedChallengeCount, int AchievementCount, int RealmAchievementCount);
    private sealed record ProgressionSystemSearch(string SystemName, int Priority, IReadOnlyList<string> SaveKeys, IReadOnlyList<string> SearchTerms, string Notes);
    private sealed record ProgressionEffectSystemMap(string SystemName, int Priority, IReadOnlyList<string> SaveKeys, IReadOnlyList<string> SearchTerms, IReadOnlyList<ProgressionSourceHit> SourceHits, IReadOnlyList<ProgressionSourceHit> DataHits, bool NeedsEffectMapping, string Notes);
    private sealed record ProgressionSourceHit(string RelativePath, string FileKind, IReadOnlyList<string> MatchedTerms, IReadOnlyList<ProgressionSourceSnippet> Snippets);
    private sealed record ProgressionSourceSnippet(string Term, int Line, string Text);
    private sealed record SearchableFile(string RelativePath, string Text);
}

// EOF - MapProgressionEffectsCommand.cs




