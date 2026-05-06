using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveUpgradeEffectMapCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --save-upgrade-effect-map .\save_export.txt .\iw_workspace_vNext [zz_save_upgrade_effect_map.json]");
            return;
        }

        var savePath = Path.GetFullPath(args[1]);
        var workspacePath = args[2];
        var outputPath = args.Length >= 4
            ? args[3]
            : ".\\zz_save_upgrade_effect_map.json";

        if (!File.Exists(savePath))
        {
            Console.WriteLine($"Save file not found: {savePath}");
            return;
        }

        var catalogPath = FindUpgradeCatalogFile(workspacePath);

        if (catalogPath is null)
        {
            Console.WriteLine("Could not find Upgrades.bytes.txt / Upgrades catalog.");
            Console.WriteLine("Expected something like:");
            Console.WriteLine(@"  .\iw_workspace_vNext\raw_files\Assets\Resources\jsonfiles\Upgrades.bytes.txt");
            return;
        }

        var saveJson = DecodeSaveString(File.ReadAllText(savePath));

        using var saveDoc = JsonDocument.Parse(saveJson);
        var root = saveDoc.RootElement;

        var purchasedIds = ReadIntArray(root, "Upgrades");
        var purchasedSet = purchasedIds.ToHashSet();

        var catalog = LoadUpgradeCatalog(catalogPath);

        var purchased = new List<UpgradeEffectEntry>();
        var unmatchedIds = new List<int>();

        foreach (var id in purchasedIds.OrderBy(x => x))
        {
            if (!catalog.TryGetValue(id, out var upgrade))
            {
                unmatchedIds.Add(id);

                purchased.Add(
                    new UpgradeEffectEntry(
                        Id: id,
                        Name: "",
                        Cost: "",
                        Description: "",
                        Sprite: "",
                        Class: "",
                        Target: "",
                        Effect: "",
                        Addendum: "",
                        Multiplier: "",
                        Secondary: "",
                        ConditionAccess: "",
                        ConditionParameter: "",
                        ConditionArgument: "",
                        Spell: "",
                        Building: "",
                        Purchased: true,
                        CatalogMatched: false,
                        EffectKind: "MissingCatalog",
                        FormulaSummary: "Purchased upgrade ID was present in save but was not found in upgrade catalog.",
                        MappingStatus: "NeedsCatalogMapping",
                        SourceNotes: "SaveData.Upgrades[] stores purchased upgrade IDs. Catalog record was not found."
                    )
                );

                continue;
            }

            purchased.Add(ToEffectEntry(upgrade, purchased: true));
        }

        var allCatalog = catalog.Values
            .OrderBy(x => x.Id)
            .Select(x => ToEffectEntry(x, purchasedSet.Contains(x.Id)))
            .ToList();

        var purchasedByTarget = purchased
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Target) ? "(no target)" : x.Target)
            .Select(g => new UpgradeTargetSummaryEntry(
                Target: g.Key,
                PurchasedCount: g.Count(),
                AdditiveCount: g.Count(x => x.EffectKind.Contains("Add", StringComparison.OrdinalIgnoreCase)),
                MultiplicativeCount: g.Count(x => x.EffectKind.Contains("Multiply", StringComparison.OrdinalIgnoreCase)),
                FormulaCount: g.Count(x => !string.IsNullOrWhiteSpace(x.Effect)),
                ExampleIds: g.Select(x => x.Id).Take(12).ToList(),
                ExampleNames: g.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Take(8).ToList()
            ))
            .OrderByDescending(x => x.PurchasedCount)
            .ThenBy(x => x.Target)
            .ToList();

        var purchasedByCondition = purchased
            .GroupBy(x => string.IsNullOrWhiteSpace(x.ConditionParameter) ? "(no condition)" : x.ConditionParameter)
            .Select(g => new UpgradeConditionSummaryEntry(
                ConditionParameter: g.Key,
                PurchasedCount: g.Count(),
                ExampleArguments: g.Select(x => x.ConditionArgument)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .Take(12)
                    .ToList(),
                ExampleIds: g.Select(x => x.Id).Take(12).ToList()
            ))
            .OrderByDescending(x => x.PurchasedCount)
            .ThenBy(x => x.ConditionParameter)
            .ToList();

        var export = new SaveUpgradeEffectMapExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SaveFile: savePath,
            Workspace: workspacePath,
            ExportRoot: WorkspacePaths.ResolveExportRoot(workspacePath),
            UpgradeCatalogFile: catalogPath,
            Summary: new UpgradeEffectSummary(
                PurchasedUpgradeCount: purchasedIds.Count,
                CatalogUpgradeCount: catalog.Count,
                CatalogMatchedPurchasedCount: purchased.Count(x => x.CatalogMatched),
                CatalogUnmatchedPurchasedCount: unmatchedIds.Count,
                UniquePurchasedTargets: purchasedByTarget.Count,
                PurchasedWithFormulaEffectCount: purchased.Count(x => !string.IsNullOrWhiteSpace(x.Effect)),
                PurchasedWithSecondaryInputCount: purchased.Count(x => !string.IsNullOrWhiteSpace(x.Secondary)),
                PurchasedWithConditionCount: purchased.Count(x => !string.IsNullOrWhiteSpace(x.ConditionParameter))
            ),
            PurchasedByTarget: purchasedByTarget,
            PurchasedByCondition: purchasedByCondition,
            PurchasedUpgrades: purchased,
            UnmatchedPurchasedUpgradeIds: unmatchedIds,
            Notes: new[]
            {
                "SaveData.Upgrades[] is the purchased/active upgrade ID list.",
                "BoughtUpgrades is a count/statistic; Upgrades[] contains the identities.",
                "Catalog fields come from Upgrades.bytes.txt / GlobalData.Upgrades data.",
                "V is exposed as Target.",
                "W is exposed as Secondary.",
                "CondtionAccess and CAArgumet preserve the game's misspelled source fields but are exported as ConditionAccess and ConditionArgument.",
                "EffectKind is a best-effort classification based on Addendum, Multiplier, Effect, Target, Secondary, Spell, and Building.",
                "Special upgrade hooks in source may still require separate source-hook mapping."
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

        Console.WriteLine("Save upgrade effect map");
        Console.WriteLine("-----------------------");
        Console.WriteLine($"Save:       {savePath}");
        Console.WriteLine($"Workspace:  {workspacePath}");
        Console.WriteLine($"Catalog:    {catalogPath}");
        Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Purchased:  {purchasedIds.Count}");
        Console.WriteLine($"Catalog:    {catalog.Count}");
        Console.WriteLine($"Matched:    {purchased.Count(x => x.CatalogMatched)}");
        Console.WriteLine($"Unmatched:  {unmatchedIds.Count}");
        Console.WriteLine($"Targets:    {purchasedByTarget.Count}");
        Console.WriteLine("");
        Console.WriteLine("Top purchased targets:");

        foreach (var target in purchasedByTarget.Take(20))
        {
            Console.WriteLine($"  {target.Target}: {target.PurchasedCount}");
        }
    }

    private static UpgradeEffectEntry ToEffectEntry(
        UpgradeCatalogRecord upgrade,
        bool purchased)
    {
        var effectKind = ClassifyEffectKind(upgrade);

        return new UpgradeEffectEntry(
            Id: upgrade.Id,
            Name: upgrade.Name,
            Cost: upgrade.Cost,
            Description: upgrade.Description,
            Sprite: upgrade.Sprite,
            Class: upgrade.Class,
            Target: upgrade.V,
            Effect: upgrade.Effect,
            Addendum: upgrade.Addendum,
            Multiplier: upgrade.Multiplier,
            Secondary: upgrade.W,
            ConditionAccess: upgrade.CondtionAccess,
            ConditionParameter: upgrade.CAParameter,
            ConditionArgument: upgrade.CAArgumet,
            Spell: upgrade.Spell,
            Building: upgrade.Building,
            Purchased: purchased,
            CatalogMatched: true,
            EffectKind: effectKind,
            FormulaSummary: BuildFormulaSummary(upgrade, effectKind),
            MappingStatus: "DataMapped",
            SourceNotes: "Mapped from Upgrades.bytes.txt catalog. Verify special source hooks separately where applicable."
        );
    }

    private static string ClassifyEffectKind(UpgradeCatalogRecord upgrade)
    {
        var hasTarget = !string.IsNullOrWhiteSpace(upgrade.V);
        var hasEffect = !string.IsNullOrWhiteSpace(upgrade.Effect);
        var hasSecondary = !string.IsNullOrWhiteSpace(upgrade.W);
        var hasSpell = !string.IsNullOrWhiteSpace(upgrade.Spell);
        var hasBuilding = !string.IsNullOrWhiteSpace(upgrade.Building);

        var add = ParseDoubleOrNull(upgrade.Addendum);
        var mult = ParseDoubleOrNull(upgrade.Multiplier);

        var hasAdd = add.HasValue && Math.Abs(add.Value) > 0.0000000001;
        var hasMult = mult.HasValue && Math.Abs(mult.Value - 1.0) > 0.0000000001;

        if (hasEffect && hasTarget && hasSecondary)
        {
            return "FormulaWithSecondary";
        }

        if (hasEffect && hasTarget)
        {
            return "Formula";
        }

        if (hasTarget && hasAdd && hasMult)
        {
            return "AddAndMultiply";
        }

        if (hasTarget && hasAdd)
        {
            return "Add";
        }

        if (hasTarget && hasMult)
        {
            return "Multiply";
        }

        if (hasTarget)
        {
            return "TargetFlagOrUnlock";
        }

        if (hasSpell)
        {
            return "SpellFlagOrSpecial";
        }

        if (hasBuilding)
        {
            return "BuildingFlagOrSpecial";
        }

        return "UnknownOrSpecial";
    }

    private static string BuildFormulaSummary(
        UpgradeCatalogRecord upgrade,
        string effectKind)
    {
        var parts = new List<string>();

        parts.Add(effectKind);

        if (!string.IsNullOrWhiteSpace(upgrade.V))
        {
            parts.Add("Target=" + upgrade.V);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Effect))
        {
            parts.Add("Effect=" + upgrade.Effect);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Addendum))
        {
            parts.Add("Addendum=" + upgrade.Addendum);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Multiplier))
        {
            parts.Add("Multiplier=" + upgrade.Multiplier);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.W))
        {
            parts.Add("Secondary=" + upgrade.W);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.CondtionAccess)
            || !string.IsNullOrWhiteSpace(upgrade.CAParameter)
            || !string.IsNullOrWhiteSpace(upgrade.CAArgumet))
        {
            parts.Add(
                "Condition=" +
                upgrade.CondtionAccess +
                "(" +
                upgrade.CAParameter +
                ", " +
                upgrade.CAArgumet +
                ")"
            );
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Spell))
        {
            parts.Add("Spell=" + upgrade.Spell);
        }

        if (!string.IsNullOrWhiteSpace(upgrade.Building))
        {
            parts.Add("Building=" + upgrade.Building);
        }

        return string.Join("; ", parts);
    }

    private static Dictionary<int, UpgradeCatalogRecord> LoadUpgradeCatalog(string catalogPath)
    {
        var result = new Dictionary<int, UpgradeCatalogRecord>();

        using var doc = JsonDocument.Parse(File.ReadAllText(catalogPath));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var idText = GetString(item, "ID");

            if (!int.TryParse(idText, out var id))
            {
                continue;
            }

            result[id] = new UpgradeCatalogRecord(
                Id: id,
                Name: GetString(item, "Name"),
                Cost: GetString(item, "Cost"),
                Description: GetString(item, "Description"),
                Sprite: GetString(item, "Sprite"),
                Class: GetString(item, "Class"),
                Effect: GetString(item, "Effect"),
                V: GetString(item, "V"),
                Addendum: GetString(item, "Addendum"),
                Multiplier: GetString(item, "Multiplier"),
                W: GetString(item, "W"),
                CondtionAccess: GetString(item, "CondtionAccess"),
                CAParameter: GetString(item, "CAParameter"),
                CAArgumet: GetString(item, "CAArgumet"),
                Spell: GetString(item, "Spell"),
                Building: GetString(item, "Building")
            );
        }

        return result;
    }

    private static string? FindUpgradeCatalogFile(string workspacePath)
    {
        var candidates = new[]
        {
            Path.Combine(workspacePath, "raw_files", "Assets", "Resources", "jsonfiles", "Upgrades.bytes.txt"),
            Path.Combine(workspacePath, "Assets", "Resources", "jsonfiles", "Upgrades.bytes.txt"),
            Path.Combine(WorkspacePaths.ResolveExportRoot(workspacePath), "Assets", "Resources", "jsonfiles", "Upgrades.bytes.txt"),
            Path.Combine(WorkspacePaths.ResolveExportRoot(workspacePath), "raw_files", "Assets", "Resources", "jsonfiles", "Upgrades.bytes.txt")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        var roots = new[]
        {
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath)
        };

        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var file = Directory
                .GetFiles(root, "Upgrades.bytes.txt", SearchOption.AllDirectories)
                .FirstOrDefault();

            if (file is not null)
            {
                return Path.GetFullPath(file);
            }
        }

        return null;
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
            if (item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out var number))
            {
                result.Add(number);
            }
            else if (item.ValueKind == JsonValueKind.String && int.TryParse(item.GetString(), out number))
            {
                result.Add(number);
            }
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

    private static string GetString(JsonElement element, string propertyName)
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

    private static double? ParseDoubleOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return double.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private sealed record SaveUpgradeEffectMapExport(
        string GeneratedAtUtc,
        string SaveFile,
        string Workspace,
        string ExportRoot,
        string UpgradeCatalogFile,
        UpgradeEffectSummary Summary,
        IReadOnlyList<UpgradeTargetSummaryEntry> PurchasedByTarget,
        IReadOnlyList<UpgradeConditionSummaryEntry> PurchasedByCondition,
        IReadOnlyList<UpgradeEffectEntry> PurchasedUpgrades,
        IReadOnlyList<int> UnmatchedPurchasedUpgradeIds,
        IReadOnlyList<string> Notes
    );

    private sealed record UpgradeEffectSummary(
        int PurchasedUpgradeCount,
        int CatalogUpgradeCount,
        int CatalogMatchedPurchasedCount,
        int CatalogUnmatchedPurchasedCount,
        int UniquePurchasedTargets,
        int PurchasedWithFormulaEffectCount,
        int PurchasedWithSecondaryInputCount,
        int PurchasedWithConditionCount
    );

    private sealed record UpgradeTargetSummaryEntry(
        string Target,
        int PurchasedCount,
        int AdditiveCount,
        int MultiplicativeCount,
        int FormulaCount,
        IReadOnlyList<int> ExampleIds,
        IReadOnlyList<string> ExampleNames
    );

    private sealed record UpgradeConditionSummaryEntry(
        string ConditionParameter,
        int PurchasedCount,
        IReadOnlyList<string> ExampleArguments,
        IReadOnlyList<int> ExampleIds
    );

    private sealed record UpgradeEffectEntry(
        int Id,
        string Name,
        string Cost,
        string Description,
        string Sprite,
        string Class,
        string Target,
        string Effect,
        string Addendum,
        string Multiplier,
        string Secondary,
        string ConditionAccess,
        string ConditionParameter,
        string ConditionArgument,
        string Spell,
        string Building,
        bool Purchased,
        bool CatalogMatched,
        string EffectKind,
        string FormulaSummary,
        string MappingStatus,
        string SourceNotes
    );

    private sealed record UpgradeCatalogRecord(
        int Id,
        string Name,
        string Cost,
        string Description,
        string Sprite,
        string Class,
        string Effect,
        string V,
        string Addendum,
        string Multiplier,
        string W,
        string CondtionAccess,
        string CAParameter,
        string CAArgumet,
        string Spell,
        string Building
    );
}
