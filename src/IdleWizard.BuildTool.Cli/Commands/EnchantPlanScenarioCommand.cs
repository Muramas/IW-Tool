using System.Globalization;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Enchanting;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class EnchantPlanScenarioCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --enchant-plan-scenario .\scenario_head_hero.json");
            return;
        }

        var scenarioPath = Path.GetFullPath(args[1]);

        if (!File.Exists(scenarioPath))
        {
            Console.WriteLine($"Scenario file not found: {scenarioPath}");
            return;
        }

        using var scenarioDoc = JsonDocument.Parse(File.ReadAllText(scenarioPath));
        var scenario = scenarioDoc.RootElement;

        var workspace = GetRequiredString(scenario, "workspace");
        workspace = ResolvePathRelativeToFile(scenarioPath, workspace);

        var rawRoot = Path.Combine(workspace, "raw_files");

        if (!Directory.Exists(rawRoot))
        {
            Console.WriteLine($"Missing raw_files folder: {rawRoot}");
            return;
        }

        var itemsFile = FindDataFile(rawRoot, "Items");
        var enchantFile = FindDataFile(rawRoot, "Enchantment");

        if (itemsFile is null)
        {
            Console.WriteLine("Could not find Items.bytes / Items.bytes.txt / Items.json.");
            return;
        }

        if (enchantFile is null)
        {
            Console.WriteLine("Could not find Enchantment.bytes / Enchantment.bytes.txt / Enchantment.json.");
            return;
        }

        using var itemsDoc = JsonDocument.Parse(File.ReadAllText(itemsFile));
        using var enchantDoc = JsonDocument.Parse(File.ReadAllText(enchantFile));

        var itemIndex = IndexArrayBy(itemsDoc.RootElement, "ID");
        var enchantIndex = IndexArrayBy(enchantDoc.RootElement, "Key");

        var globalObjectives = ReadDoubleDictionary(scenario, "objectives");
        var phasePlans = new List<EnchantCandidate>();

        if (!scenario.TryGetProperty("phases", out var phases)
            || phases.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Scenario does not contain phases[]. Nothing to plan.");
            return;
        }

        foreach (var phase in phases.EnumerateArray())
        {
            var phaseName = Get(phase, "name");
            var phaseWeight = GetDouble(phase, "phaseWeight", 1.0);

            var phaseObjectives = ReadDoubleDictionary(phase, "objectives");
            var activeObjectives = phaseObjectives.Count > 0
                ? phaseObjectives
                : globalObjectives;

            if (!phase.TryGetProperty("equipment", out var equipment)
                || equipment.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var slot in equipment.EnumerateObject())
            {
                var equip = slot.Value;

                var itemId = Get(equip, "itemId");
                var tier = Get(equip, "tier");
                var baseLevel = GetInt(equip, "enchantLevel", 0);
                var bonusLevel = GetInt(equip, "bonusEnchantLevel", 0);

                if (string.IsNullOrWhiteSpace(itemId))
                {
                    continue;
                }

                if (!itemIndex.TryGetValue(itemId, out var item))
                {
                    continue;
                }

                var itemName = Get(item, "Name");
                var enchantKey = Get(item, "Enchant");
                var baseRateRaw = Get(item, "EBase");

                if (string.IsNullOrWhiteSpace(enchantKey)
                    || string.IsNullOrWhiteSpace(baseRateRaw))
                {
                    continue;
                }

                if (!double.TryParse(baseRateRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var baseRate))
                {
                    continue;
                }

                enchantIndex.TryGetValue(enchantKey, out var enchantRecord);

                var target = enchantRecord.ValueKind == JsonValueKind.Object
                    ? Get(enchantRecord, "T")
                    : "";

                var description = enchantRecord.ValueKind == JsonValueKind.Object
                    ? Get(enchantRecord, "Description")
                    : "";

                var current = EnchantCalculator.Calculate(baseRate, baseLevel, bonusLevel);
                var next = EnchantCalculator.Calculate(baseRate, baseLevel + 1, bonusLevel);

                var gainRatio = next.Multiplier / current.Multiplier;
                var log10Gain = Math.Log10(gainRatio);

                var objectiveWeight = activeObjectives.TryGetValue(target, out var weight)
                    ? weight
                    : 0.0;

                var contribution = log10Gain * objectiveWeight * phaseWeight;

                phasePlans.Add(
                    new EnchantCandidate(
                        phaseName,
                        phaseWeight,
                        slot.Name,
                        itemId,
                        itemName,
                        tier,
                        enchantKey,
                        target,
                        description,
                        baseRate,
                        baseLevel,
                        bonusLevel,
                        current.EffectiveLevel,
                        current.BonusPercent,
                        next.EffectiveLevel,
                        next.BonusPercent,
                        next.BonusPercent - current.BonusPercent,
                        gainRatio,
                        log10Gain,
                        objectiveWeight,
                        contribution
                    )
                );
            }
        }

        var ranked = phasePlans
            .OrderByDescending(x => x.Contribution)
            .ThenByDescending(x => x.Log10Gain)
            .ToList();

        Console.WriteLine("Enchant plan scenario");
        Console.WriteLine("---------------------");
        Console.WriteLine($"Scenario:   {scenarioPath}");
        Console.WriteLine($"Workspace:  {workspace}");
        Console.WriteLine($"Candidates: {ranked.Count}");
        Console.WriteLine("");

        Console.WriteLine("Global objective weights:");
        foreach (var objective in globalObjectives.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {objective.Key}: {objective.Value}");
        }

        Console.WriteLine("");
        Console.WriteLine("Note: if a phase has its own objectives object, phase objectives override global objectives for that phase.");

        Console.WriteLine("");
        Console.WriteLine("Ranked next +1 enchant candidates:");
        Console.WriteLine("");

        var rank = 1;

        foreach (var item in ranked)
        {
            var verdict = item.Contribution > 0
                ? "Scores"
                : "No direct objective score";

            Console.WriteLine($"#{rank}: {item.Phase} / {item.Slot} / {item.ItemName}");
            Console.WriteLine($"  Item:             {item.ItemId}, Tier {item.Tier}");
            Console.WriteLine($"  Enchant key:      {item.EnchantKey}");
            Console.WriteLine($"  Target:           {item.Target}");
            Console.WriteLine($"  Description:      {item.Description}");
            Console.WriteLine($"  Rate:             {item.BaseRate:P2}");
            Console.WriteLine($"  Level:            {item.BaseLevel}+{item.BonusLevel} = {item.EffectiveLevel}");
            Console.WriteLine($"  Current bonus:    {item.CurrentBonusPercent:0.00}%");
            Console.WriteLine($"  Next effective:   {item.NextEffectiveLevel}");
            Console.WriteLine($"  Next bonus:       {item.NextBonusPercent:0.00}%");
            Console.WriteLine($"  Gain from +1:     {item.GainPercent:0.00}%");
            Console.WriteLine($"  Gain ratio:       {item.GainRatio:0.############}");
            Console.WriteLine($"  log10 gain:       {item.Log10Gain:0.############}");
            Console.WriteLine($"  Phase weight:     {item.PhaseWeight}");
            Console.WriteLine($"  Objective weight: {item.ObjectiveWeight}");
            Console.WriteLine($"  Contribution:     {item.Contribution:0.############}");
            Console.WriteLine($"  Verdict:          {verdict}");
            Console.WriteLine("");

            rank++;
        }

        Console.WriteLine("Notes:");
        Console.WriteLine("  This is direct-objective enchant planning only.");
        Console.WriteLine("  Dust cost is not included yet.");
        Console.WriteLine("  Next step is to source-verify enchant dust cost and rank by contribution per dust.");
    }

    private static Dictionary<string, JsonElement> IndexArrayBy(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

        if (root.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var key = Get(item, propertyName);

            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = item;
            }
        }

        return result;
    }

    private static Dictionary<string, double> ReadDoubleDictionary(JsonElement root, string propertyName)
    {
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty(propertyName, out var obj)
            || obj.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in obj.EnumerateObject())
        {
            var raw = GetScalar(property.Value);

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                result[property.Name] = value;
            }
        }

        return result;
    }

    private static string GetRequiredString(JsonElement root, string propertyName)
    {
        var value = Get(root, propertyName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required scenario property: {propertyName}");
        }

        return value;
    }

    private static string ResolvePathRelativeToFile(string baseFile, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var dir = Path.GetDirectoryName(baseFile) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(dir, path));
    }

    private static string? FindDataFile(string rawRoot, string logicalName)
    {
        var candidates = new[]
        {
            logicalName + ".bytes",
            logicalName + ".bytes.txt",
            logicalName + ".json"
        };

        return Directory
            .EnumerateFiles(rawRoot, "*", SearchOption.AllDirectories)
            .Where(path => candidates.Any(
                candidate => Path.GetFileName(path).Equals(candidate, StringComparison.OrdinalIgnoreCase)
            ))
            .OrderBy(path => path)
            .FirstOrDefault();
    }

    private static string Get(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "";
        }

        return GetScalar(value);
    }

    private static string GetScalar(JsonElement value)
    {
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

    private static int GetInt(JsonElement element, string propertyName, int fallback)
    {
        var raw = Get(element, propertyName);

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static double GetDouble(JsonElement element, string propertyName, double fallback)
    {
        var raw = Get(element, propertyName);

        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private sealed record EnchantCandidate(
        string Phase,
        double PhaseWeight,
        string Slot,
        string ItemId,
        string ItemName,
        string Tier,
        string EnchantKey,
        string Target,
        string Description,
        double BaseRate,
        int BaseLevel,
        int BonusLevel,
        int EffectiveLevel,
        double CurrentBonusPercent,
        int NextEffectiveLevel,
        double NextBonusPercent,
        double GainPercent,
        double GainRatio,
        double Log10Gain,
        double ObjectiveWeight,
        double Contribution
    );
}

