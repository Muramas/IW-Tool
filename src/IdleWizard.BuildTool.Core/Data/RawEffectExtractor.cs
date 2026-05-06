using System.Text.Json;

namespace IdleWizard.BuildTool.Core.Data;

public sealed class RawEffectExtractor
{
    public IReadOnlyList<RawEffectDescriptor> Extract(
        string workspacePath,
        string fileNameHint)
    {
        var rawRoot = Path.Combine(workspacePath, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            throw new DirectoryNotFoundException(
                $"Missing raw_files directory: {rawRoot}"
            );
        }

        var file = FindBestFile(rawRoot, fileNameHint);

        if (file is null)
        {
            return Array.Empty<RawEffectDescriptor>();
        }

        var relative = Path
            .GetRelativePath(rawRoot, file)
            .Replace("\\", "/");

        using var doc = JsonDocument.Parse(File.ReadAllText(file));

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<RawEffectDescriptor>();
        }

        var lowerName = Path.GetFileName(file).ToLowerInvariant();

        if (lowerName == "items.bytes.txt")
        {
            return ExtractItemEffects(relative, doc.RootElement);
        }

        if (lowerName == "upgrades.bytes.txt")
        {
            return ExtractUpgradeEffects(relative, doc.RootElement);
        }

        if (lowerName == "affixes.bytes.txt")
        {
            return ExtractAffixEffects(relative, doc.RootElement);
        }

        if (lowerName == "spellcraft.bytes.txt")
        {
            return ExtractSimpleTargetAMWEffects(
                relative,
                "Spellcraft",
                doc.RootElement
            );
        }

        if (lowerName == "mastery.bytes.txt")
        {
            return ExtractSimpleTargetAMWEffects(
                relative,
                "Mastery",
                doc.RootElement
            );
        }

        if (lowerName == "setbonuses.bytes.txt")
        {
            return ExtractSetBonusEffects(relative, doc.RootElement);
        }

        if (lowerName == "realmupgrades.bytes.txt")
        {
            return ExtractRealmUpgradeEffects(relative, doc.RootElement);
        }

        return Array.Empty<RawEffectDescriptor>();
    }

    private static string? FindBestFile(
        string rawRoot,
        string fileNameHint)
    {
        var candidates = Directory
            .EnumerateFiles(rawRoot, "*", SearchOption.AllDirectories)
            .Where(
                path =>
                    (
                        (path.EndsWith(".bytes", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".bytes.txt", StringComparison.OrdinalIgnoreCase))
                        || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                    )
                    && Path
                        .GetFileName(path)
                        .Contains(fileNameHint, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(path => path)
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        var exactBytesName = fileNameHint + ".bytes.txt";
        var exactRawBytesName = fileNameHint + ".bytes";
        var exactJsonName = fileNameHint + ".json";

        var exact = candidates.FirstOrDefault(
            path =>
                Path.GetFileName(path).Equals(
                    exactBytesName,
                    StringComparison.OrdinalIgnoreCase
                )
                || Path.GetFileName(path).Equals(
                    exactRawBytesName,
                    StringComparison.OrdinalIgnoreCase
                )
                || Path.GetFileName(path).Equals(
                    exactJsonName,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        return exact ?? candidates[0];
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractItemEffects(
        string sourceFile,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var itemIndex = 0;

        foreach (var item in root.EnumerateArray())
        {
            var itemId = Get(item, "ID");
            var itemName = Get(item, "Name");

            if (!item.TryGetProperty("Tiers", out var tiers))
            {
                itemIndex++;
                continue;
            }

            var tierIndex = 0;

            foreach (var tier in tiers.EnumerateArray())
            {
                var tierName = Get(tier, "Tier");

                if (!tier.TryGetProperty("Effs", out var effects))
                {
                    tierIndex++;
                    continue;
                }

                var effectIndex = 0;

                foreach (var effect in effects.EnumerateArray())
                {
                    results.Add(
                        new RawEffectDescriptor(
                            sourceFile,
                            "ItemTierEffect",
                            itemId,
                            itemName,
                            $"Items[{itemIndex}].Tiers[{tierIndex}].Effs[{effectIndex}]",
                            Get(effect, "target"),
                            Get(effect, "effect", "0"),
                            Get(effect, "a", "0"),
                            Get(effect, "m", "1"),
                            Get(effect, "diminish", "0"),
                            $"Tier={tierName}"
                        )
                    );

                    effectIndex++;
                }

                tierIndex++;
            }

            itemIndex++;
        }

        return results;
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractUpgradeEffects(
        string sourceFile,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var index = 0;

        foreach (var upgrade in root.EnumerateArray())
        {
            var target = Get(upgrade, "V");

            if (string.IsNullOrWhiteSpace(target))
            {
                index++;
                continue;
            }

            results.Add(
                new RawEffectDescriptor(
                    sourceFile,
                    "UpgradeEffect",
                    Get(upgrade, "ID"),
                    Get(upgrade, "Name"),
                    $"Upgrades[{index}]",
                    target,
                    "0",
                    Get(upgrade, "Addendum", "0"),
                    Get(upgrade, "Multiplier", "1"),
                    "0",
                    "Flat upgrade data: V/Addendum/Multiplier"
                )
            );

            index++;
        }

        return results;
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractAffixEffects(
        string sourceFile,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var index = 0;

        foreach (var affix in root.EnumerateArray())
        {
            results.Add(
                new RawEffectDescriptor(
                    sourceFile,
                    "AffixRollRange",
                    Get(affix, "Id"),
                    Get(affix, "Description"),
                    $"Affixes[{index}]",
                    Get(affix, "Target"),
                    "0",
                    Get(affix, "Min"),
                    Get(affix, "Max"),
                    Get(affix, "Diminish", "0"),
                    "Roll range only: Addendum=Min, Multiplier=Max placeholder fields; user roll required before calculation"
                )
            );

            index++;
        }

        return results;
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractSimpleTargetAMWEffects(
        string sourceFile,
        string sourceKind,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var index = 0;

        foreach (var record in root.EnumerateArray())
        {
            var target = Get(record, "Target");

            if (string.IsNullOrWhiteSpace(target))
            {
                index++;
                continue;
            }

            results.Add(
                new RawEffectDescriptor(
                    sourceFile,
                    sourceKind,
                    Get(record, "Level"),
                    Get(record, "Description"),
                    $"{sourceKind}[{index}]",
                    target,
                    "0",
                    Get(record, "a", "0"),
                    Get(record, "m", "1"),
                    "0",
                    $"w={Get(record, "w", "")}"
                )
            );

            index++;
        }

        return results;
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractSetBonusEffects(
        string sourceFile,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var index = 0;

        foreach (var record in root.EnumerateArray())
        {
            var target = Get(record, "T");

            if (string.IsNullOrWhiteSpace(target))
            {
                index++;
                continue;
            }

            results.Add(
                new RawEffectDescriptor(
                    sourceFile,
                    "SetBonusEffect",
                    Get(record, "Key"),
                    Get(record, "Description"),
                    $"SetBonuses[{index}]",
                    target,
                    "0",
                    Get(record, "A", "0"),
                    Get(record, "M", "1"),
                    "0",
                    $"Amount={Get(record, "Amount", "")}"
                )
            );

            index++;
        }

        return results;
    }

    private static IReadOnlyList<RawEffectDescriptor> ExtractRealmUpgradeEffects(
        string sourceFile,
        JsonElement root)
    {
        var results = new List<RawEffectDescriptor>();
        var index = 0;

        foreach (var record in root.EnumerateArray())
        {
            var target = Get(record, "Param");

            if (string.IsNullOrWhiteSpace(target))
            {
                index++;
                continue;
            }

            results.Add(
                new RawEffectDescriptor(
                    sourceFile,
                    "RealmUpgradeEffect",
                    Get(record, "ID"),
                    Get(record, "Name"),
                    $"RealmUpgrades[{index}]",
                    target,
                    "0",
                    Get(record, "A", "0"),
                    Get(record, "M", "1"),
                    "0",
                    $"P={Get(record, "P", "")}; MaxLvl={Get(record, "MaxLvl", "")}"
                )
            );

            index++;
        }

        return results;
    }

    private static string Get(
        JsonElement element,
        string propertyName,
        string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => fallback,
            _ => value.GetRawText(),
        };
    }
}

