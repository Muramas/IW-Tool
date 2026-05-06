using System.Globalization;
using System.Text.Json;
using IdleWizard.BuildTool.Core.Enchanting;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ItemEnchantValueCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 5)
        {
            PrintUsage();
            return;
        }

        var workspacePath = args[1];
        var itemId = args[2];

        if (!int.TryParse(args[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseLevel))
        {
            Console.WriteLine($"Could not parse base enchant level: {args[3]}");
            return;
        }

        if (!int.TryParse(args[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var bonusLevel))
        {
            Console.WriteLine($"Could not parse bonus enchant level: {args[4]}");
            return;
        }

        var rawRoot = Path.Combine(workspacePath, "raw_files");

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

        var item = FindArrayRecordByProperty(itemsDoc.RootElement, "ID", itemId);

        if (item.ValueKind != JsonValueKind.Object)
        {
            Console.WriteLine($"Item not found: {itemId}");
            return;
        }

        var itemName = Get(item, "Name");
        var enchantKey = Get(item, "Enchant");
        var baseRateRaw = Get(item, "EBase");

        if (string.IsNullOrWhiteSpace(enchantKey))
        {
            Console.WriteLine($"Item {itemId} / {itemName} does not have an Enchant key.");
            return;
        }

        if (!double.TryParse(baseRateRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var baseRate))
        {
            Console.WriteLine($"Could not parse item EBase rate: {baseRateRaw}");
            return;
        }

        var enchantRecord = FindArrayRecordByProperty(enchantDoc.RootElement, "Key", enchantKey);

        var enchantTarget = enchantRecord.ValueKind == JsonValueKind.Object
            ? Get(enchantRecord, "T")
            : "";

        var enchantDescription = enchantRecord.ValueKind == JsonValueKind.Object
            ? Get(enchantRecord, "Description")
            : "";

        var current = EnchantCalculator.Calculate(baseRate, baseLevel, bonusLevel);
        var next = EnchantCalculator.Calculate(baseRate, baseLevel + 1, bonusLevel);

        var gainBonus = next.Bonus - current.Bonus;
        var gainPercent = gainBonus * 100.0;

        Console.WriteLine("Item enchant value");
        Console.WriteLine("------------------");
        Console.WriteLine($"Workspace:        {workspacePath}");
        Console.WriteLine($"Item ID:          {itemId}");
        Console.WriteLine($"Item:             {itemName}");
        Console.WriteLine($"Enchant key:      {enchantKey}");
        Console.WriteLine($"Enchant target:   {enchantTarget}");
        Console.WriteLine($"Description:      {enchantDescription}");
        Console.WriteLine("");
        Console.WriteLine($"Base rate:        {baseRate:P2}");
        Console.WriteLine($"Base level:       {baseLevel}");
        Console.WriteLine($"Bonus level:      {bonusLevel}");
        Console.WriteLine($"Effective level:  {current.EffectiveLevel}");
        Console.WriteLine("");
        Console.WriteLine($"Current bonus:    {current.BonusPercent:0.00}%");
        Console.WriteLine($"Current mult:     {current.Multiplier:0.############}");
        Console.WriteLine("");
        Console.WriteLine($"Next base level:  {baseLevel + 1}");
        Console.WriteLine($"Next effective:   {next.EffectiveLevel}");
        Console.WriteLine($"Next bonus:       {next.BonusPercent:0.00}%");
        Console.WriteLine($"Next mult:        {next.Multiplier:0.############}");
        Console.WriteLine("");
        Console.WriteLine($"Gain from +1:     {gainPercent:0.00}%");
        Console.WriteLine("");
        Console.WriteLine("Formula:");
        Console.WriteLine("  bonus = (1 + EBase) ^ (baseLevel + bonusLevel) - 1");
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
                candidate => Path.GetFileName(path).Equals(
                    candidate,
                    StringComparison.OrdinalIgnoreCase
                )
            ))
            .OrderBy(path => path)
            .FirstOrDefault();
    }

    private static JsonElement FindArrayRecordByProperty(
        JsonElement root,
        string propertyName,
        string expectedValue)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return default;
        }

        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var actual = Get(item, propertyName);

            if (actual.Equals(expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return default;
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

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine(@"  --item-enchant-value .\iw_workspace_vNext ITEM_ID BASE_LEVEL BONUS_LEVEL");
        Console.WriteLine("");
        Console.WriteLine("Examples:");
        Console.WriteLine(@"  --item-enchant-value .\iw_workspace_vNext 0 5 5");
        Console.WriteLine(@"  --item-enchant-value .\iw_workspace_vNext 1 7 1");
    }
}
