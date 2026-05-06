using System.Text.Json;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class SaveUpgradeTargetAggregateCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --save-upgrade-target-aggregate .\zz_save_upgrade_effect_map.json [zz_save_upgrade_target_aggregate.json]");
            return;
        }

        var inputPath = Path.GetFullPath(args[1]);
        var outputPath = args.Length >= 3
            ? args[2]
            : ".\\zz_save_upgrade_target_aggregate.json";

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Upgrade effect map not found: {inputPath}");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(inputPath));
        var root = doc.RootElement;

        if (!root.TryGetProperty("PurchasedUpgrades", out var purchased)
            || purchased.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Input does not contain PurchasedUpgrades array.");
            return;
        }

        var entries = new List<UpgradeEffectEntry>();

        foreach (var item in purchased.EnumerateArray())
        {
            entries.Add(
                new UpgradeEffectEntry(
                    Id: GetInt(item, "Id"),
                    Name: GetString(item, "Name"),
                    Target: GetString(item, "Target"),
                    EffectKind: GetString(item, "EffectKind"),
                    Effect: GetString(item, "Effect"),
                    Addendum: GetString(item, "Addendum"),
                    Multiplier: GetString(item, "Multiplier"),
                    Secondary: GetString(item, "Secondary"),
                    ConditionParameter: GetString(item, "ConditionParameter"),
                    ConditionArgument: GetString(item, "ConditionArgument"),
                    Spell: GetString(item, "Spell"),
                    Building: GetString(item, "Building"),
                    FormulaSummary: GetString(item, "FormulaSummary")
                )
            );
        }

        var targetGroups = entries
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Target) ? "(no target)" : x.Target)
            .Select(BuildTargetAggregate)
            .OrderByDescending(x => x.PurchasedCount)
            .ThenBy(x => x.Target)
            .ToList();

        var export = new UpgradeTargetAggregateExport(
            GeneratedAtUtc: DateTime.UtcNow.ToString("O"),
            SourceFile: inputPath,
            TargetCount: targetGroups.Count,
            UpgradeCount: entries.Count,
            Targets: targetGroups,
            Notes: new[]
            {
                "This aggregate is derived from save-upgrade-effect-map.",
                "AdditiveSum is the sum of numeric Addendum values for simple additive upgrades.",
                "MultiplierProduct is the product of numeric Multiplier values for simple multiplicative upgrades.",
                "Formula upgrades are preserved separately because effects like PowA require source/formula-specific evaluation.",
                "Conditioned upgrades may already be purchased, but their condition metadata is preserved for explainability.",
                "Special source hooks still require separate source-hook mapping."
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

        Console.WriteLine("Save upgrade target aggregate");
        Console.WriteLine("-----------------------------");
        Console.WriteLine($"Input:    {inputPath}");
        Console.WriteLine($"Output:   {Path.GetFullPath(outputPath)}");
        Console.WriteLine($"Upgrades: {entries.Count}");
        Console.WriteLine($"Targets:  {targetGroups.Count}");
        Console.WriteLine("");
        Console.WriteLine("Top targets:");

        foreach (var target in targetGroups.Take(30))
        {
            Console.WriteLine(
                $"  {target.Target}: count={target.PurchasedCount}, add={target.AdditiveSum}, mult={target.MultiplierProduct}, formulas={target.FormulaCount}"
            );
        }
    }

    private static UpgradeTargetAggregate BuildTargetAggregate(IGrouping<string, UpgradeEffectEntry> group)
    {
        var simpleAdds = new List<UpgradeEffectEntry>();
        var simpleMultipliers = new List<UpgradeEffectEntry>();
        var formulas = new List<FormulaUpgradeEntry>();
        var conditioned = new List<ConditionedUpgradeEntry>();
        var unknown = new List<UpgradeReferenceEntry>();

        var additiveSum = 0.0;
        var multiplierProduct = 1.0;
        var hasMultiplier = false;

        foreach (var entry in group)
        {
            var add = ParseDoubleOrNull(entry.Addendum);
            var mult = ParseDoubleOrNull(entry.Multiplier);

            var isFormula =
                !string.IsNullOrWhiteSpace(entry.Effect)
                || entry.EffectKind.Contains("Formula", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(entry.Secondary);

            if (isFormula)
            {
                formulas.Add(
                    new FormulaUpgradeEntry(
                        entry.Id,
                        entry.Name,
                        entry.Effect,
                        entry.Addendum,
                        entry.Multiplier,
                        entry.Secondary,
                        entry.FormulaSummary
                    )
                );
            }
            else
            {
                if (entry.EffectKind.Contains("Add", StringComparison.OrdinalIgnoreCase)
                    && add.HasValue)
                {
                    additiveSum += add.Value;
                    simpleAdds.Add(entry);
                }

                if (entry.EffectKind.Contains("Multiply", StringComparison.OrdinalIgnoreCase)
                    && mult.HasValue)
                {
                    multiplierProduct *= mult.Value;
                    hasMultiplier = true;
                    simpleMultipliers.Add(entry);
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.ConditionParameter))
            {
                conditioned.Add(
                    new ConditionedUpgradeEntry(
                        entry.Id,
                        entry.Name,
                        entry.ConditionParameter,
                        entry.ConditionArgument
                    )
                );
            }

            if (entry.EffectKind.Contains("Unknown", StringComparison.OrdinalIgnoreCase)
                || entry.EffectKind.Contains("Special", StringComparison.OrdinalIgnoreCase))
            {
                unknown.Add(new UpgradeReferenceEntry(entry.Id, entry.Name));
            }
        }

        return new UpgradeTargetAggregate(
            Target: group.Key,
            PurchasedCount: group.Count(),
            AdditiveSum: additiveSum,
            MultiplierProduct: hasMultiplier ? multiplierProduct : 1.0,
            SimpleAddCount: simpleAdds.Count,
            SimpleMultiplierCount: simpleMultipliers.Count,
            FormulaCount: formulas.Count,
            ConditionedCount: conditioned.Count,
            UnknownOrSpecialCount: unknown.Count,
            ExampleIds: group.Select(x => x.Id).Take(20).ToList(),
            ExampleNames: group.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Take(12).ToList(),
            FormulaUpgrades: formulas,
            ConditionedUpgrades: conditioned.Take(50).ToList(),
            UnknownOrSpecialUpgrades: unknown
        );
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

    private static int GetInt(JsonElement element, string propertyName)
    {
        var text = GetString(element, propertyName);
        return int.TryParse(text, out var value) ? value : 0;
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

    private sealed record UpgradeTargetAggregateExport(
        string GeneratedAtUtc,
        string SourceFile,
        int TargetCount,
        int UpgradeCount,
        IReadOnlyList<UpgradeTargetAggregate> Targets,
        IReadOnlyList<string> Notes
    );

    private sealed record UpgradeTargetAggregate(
        string Target,
        int PurchasedCount,
        double AdditiveSum,
        double MultiplierProduct,
        int SimpleAddCount,
        int SimpleMultiplierCount,
        int FormulaCount,
        int ConditionedCount,
        int UnknownOrSpecialCount,
        IReadOnlyList<int> ExampleIds,
        IReadOnlyList<string> ExampleNames,
        IReadOnlyList<FormulaUpgradeEntry> FormulaUpgrades,
        IReadOnlyList<ConditionedUpgradeEntry> ConditionedUpgrades,
        IReadOnlyList<UpgradeReferenceEntry> UnknownOrSpecialUpgrades
    );

    private sealed record UpgradeEffectEntry(
        int Id,
        string Name,
        string Target,
        string EffectKind,
        string Effect,
        string Addendum,
        string Multiplier,
        string Secondary,
        string ConditionParameter,
        string ConditionArgument,
        string Spell,
        string Building,
        string FormulaSummary
    );

    private sealed record FormulaUpgradeEntry(
        int Id,
        string Name,
        string Effect,
        string Addendum,
        string Multiplier,
        string Secondary,
        string FormulaSummary
    );

    private sealed record ConditionedUpgradeEntry(
        int Id,
        string Name,
        string ConditionParameter,
        string ConditionArgument
    );

    private sealed record UpgradeReferenceEntry(
        int Id,
        string Name
    );
}
