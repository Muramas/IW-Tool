using System.Text.Json;
using IdleWizard.BuildTool.Cli.Commands;
using IdleWizard.BuildTool.Core.Data;
using IdleWizard.BuildTool.Core.Effects;
using IdleWizard.BuildTool.Core.Evaluation;
using IdleWizard.BuildTool.Core.Numbers;
using IdleWizard.BuildTool.Core.Variables;

namespace IdleWizard.BuildTool.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            RunDemo();
            PrintHelp();
            return 0;
        }

        switch (args[0])
        {
            case "--score-slot":
                ScoreSlotCommand.Run(args);
                return 0;

            case "--score-slot-weighted":
                ScoreSlotWeightedCommand.Run(args);
                return 0;

            case "--score-slot-weighted-file":
                ScoreSlotWeightedFileCommand.Run(args);
                return 0;

            case "--enchant-value":
                EnchantValueCommand.Run(args);
                return 0;

            case "--item-enchant-value":
                ItemEnchantValueCommand.Run(args);
                return 0;

            case "--enchant-plan-scenario":
                EnchantPlanScenarioCommand.Run(args);
                return 0;

            case "--spell-catalog":
                ClassSpellPhaseCommand.RunSpellCatalog(args);
                return 0;

            case "--phase-class-summary":
                ClassSpellPhaseCommand.RunPhaseClassSummary(args);
                return 0;

            case "--class-spell-map":
                ClassSpellMapCommand.RunClassSpellMap(args);
                return 0;

            case "--validate-phase-spells":
                ClassSpellMapCommand.RunValidatePhaseSpells(args);
                return 0;

            case "--spell-variants":
                SpellVariantsCommand.Run(args);
                return 0;

            case "--scenario-options":
                ScenarioOptionsCommand.Run(args);
                return 0;

            case "--pet-options":
                PetOptionsCommand.Run(args);
                return 0;

            case "--validate-phase-pets":
                PetPhaseValidationCommand.Run(args);
                return 0;

            case "--inspect-save-string":
                InspectSaveStringCommand.Run(args);
                return 0;

            case "--save-summary":
                SaveSummaryCommand.Run(args);
                return 0;

            case "--save-extract-keys":
                SaveExtractKeysCommand.Run(args);
                return 0;

            case "--save-item-presets":
                SaveItemPresetsCommand.Run(args);
                return 0;

            case "--apply-preset-to-scenario":
                ApplyPresetToScenarioCommand.Run(args);
                return 0;

            case "--apply-phase-presets-to-scenario":
                ApplyPhasePresetsToScenarioCommand.Run(args);
                return 0;

            case "--generate-scenario-from-save":
                GenerateScenarioFromSaveCommand.Run(args);
                return 0;

            case "--save-import-options":
                SaveImportOptionsCommand.Run(args);
                return 0;

            case "--save-variable-map":
                SaveVariableMapCommand.Run(args);
                return 0;

            case "--save-field-inventory":
                SaveFieldInventoryCommand.Run(args);
                return 0;

            case "--save-field-shape":
                SaveFieldShapeCommand.Run(args);
                return 0;

            case "--save-spell-map":
                SaveSpellMapCommand.Run(args);
                return 0;

            case "--save-progress-map":
                SaveProgressMapCommand.Run(args);
                return 0;

            case "--save-catalyst-map":
                SaveCatalystMapCommand.Run(args);
                return 0;

            case "--save-calculation-context":
                SaveCalculationContextCommand.Run(args);
                return 0;

            case "--save-unlock-map":
                SaveUnlockMapCommand.Run(args);
                return 0;

            case "--save-upgrade-effect-map":
                SaveUpgradeEffectMapCommand.Run(args);
                return 0;

            case "--save-upgrade-target-aggregate":
                SaveUpgradeTargetAggregateCommand.Run(args);
                return 0;

            case "--save-upgrade-formula-dependencies":
                SaveUpgradeFormulaDependenciesCommand.Run(args);
                return 0;

            case "--save-achievement-map":
                SaveAchievementMapCommand.Run(args);
                return 0;

            case "--save-hero-xp-map":
                SaveHeroXpMapCommand.Run(args);
                return 0;

            case "--apply-save-import-options-to-scenario":
                ApplySaveImportOptionsToScenarioCommand.Run(args);
                return 0;

            case "--class-source-diagnostics":
                ClassSourceDiagnosticsCommand.Run(args);
                return 0;

            case "--hero-impl-diagnostics":
                HeroImplementationDiagnosticsCommand.Run(args);
                return 0;

            case "--score-items":
                ScoreItemsCommand.Run(args);
                return 0;

            case "--compare-items":
                CompareItemsCommand.Run(args);
                return 0;

            case "--audit":
                RunAudit(args);
                return 0;

            case "--data-summary":
                RunDataSummary(args);
                return 0;

            case "--records":
                RunRecords(args);
                return 0;

            case "--find-record":
                RunFindRecord(args);
                return 0;

            case "--effects":
                RunEffects(args);
                return 0;

            case "--convert-effects":
                RunConvertEffects(args);
                return 0;

            case "--evaluate-item":
                RunEvaluateItem(args);
                return 0;

            case "--evaluate-item-values":
                RunEvaluateItemValues(args);
                return 0;

            case "--help":
            case "-h":
            case "/?":
                PrintHelp();
                return 0;

            default:
                Console.WriteLine($"Unknown command: {args[0]}");
                Console.WriteLine("");
                PrintHelp();
                return 1;
        }
    }

    private static void RunAudit(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --audit .\iw_workspace_vNext\verified_source_index.json");
            return;
        }

        var auditPath = args[1];

        if (!File.Exists(auditPath))
        {
            Console.WriteLine($"Audit file not found: {auditPath}");
            Console.WriteLine(@"Expected something like: .\iw_workspace_vNext\verified_source_index.json");
            return;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(auditPath));
        var root = doc.RootElement;

        Console.WriteLine($"Audit file: {auditPath}");

        if (root.TryGetProperty("file_count", out var fileCount))
        {
            Console.WriteLine($"Files indexed: {fileCount}");
        }

        Console.WriteLine("");
        Console.WriteLine("Important symbols:");

        foreach (var name in new[]
        {
            "BigNumber",
            "Variable",
            "VariableComplex",
            "VariableBignumber",
            "VariableFloat",
            "VariableInt",
            "VariableLong",
            "ProfitVariable",
            "EffectFactory",
            "EffectNames",
            "SimpleEffect",
            "CombineEffect",
            "ActionEffect",
            "IEffect",
            "GameContext",
            "GameManager",
            "ConditionFactory",
            "SaveData",
            "SpellBook",
            "Item",
            "Spell",
            "Upgrade",
            "Affix",
            "Familiar",
            "Pet",
            "Hero",
            "Statistic",
        })
        {
            TryPrintSymbolLocation(root, name);
        }
    }

    private static bool TryPrintSymbolLocation(JsonElement root, string symbolName)
    {
        var found = false;

        foreach (var sectionName in new[] { "classes", "structs", "interfaces" })
        {
            if (
                root.TryGetProperty(sectionName, out var section)
                && section.ValueKind == JsonValueKind.Object
                && section.TryGetProperty(symbolName, out var locations)
            )
            {
                Console.WriteLine($"{symbolName} ({sectionName}): {locations}");
                found = true;
            }
        }

        if (found)
        {
            return true;
        }

        if (
            root.TryGetProperty("files", out var files)
            && files.ValueKind == JsonValueKind.Array
        )
        {
            var expectedFileName = symbolName + ".cs";

            foreach (var file in files.EnumerateArray())
            {
                if (
                    file.TryGetProperty("path", out var pathElement)
                    && pathElement.ValueKind == JsonValueKind.String
                )
                {
                    var path = pathElement.GetString() ?? "";

                    if (
                        path.EndsWith(
                            expectedFileName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        Console.WriteLine($"{symbolName} (file): {path}");
                        return true;
                    }
                }
            }
        }

        Console.WriteLine($"{symbolName}: NOT FOUND");
        return false;
    }

    private static void RunDataSummary(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --data-summary .\iw_workspace_vNext");
            return;
        }

        var workspacePath = args[1];

        var loader = new RawGameDataLoader();
        var summaries = loader.SummarizeWorkspace(workspacePath);

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Data files found: {summaries.Count}");
        Console.WriteLine("");

        var importantNames = new[]
        {
            "Items",
            "Spells",
            "Upgrades",
            "Affixes",
            "SetBonuses",
            "SetMap",
            "Weapons",
            "Familiars",
            "Spellcraft",
            "SpellMastery",
            "RealmUpgrades",
            "Tempering",
            "Enchantment",
            "PermanentParagons",
            "Mastery",
            "En.bytes",
        };

        foreach (var summary in summaries)
        {
            var isImportant = importantNames.Any(
                name => summary.File.Contains(name, StringComparison.OrdinalIgnoreCase)
            );

            if (!isImportant)
            {
                continue;
            }

            Console.WriteLine(summary.File);
            Console.WriteLine($"  ParsedJson: {summary.ParsedJson}");
            Console.WriteLine($"  RootType: {summary.RootType}");
            Console.WriteLine($"  RecordPath: {summary.RecordPath}");
            Console.WriteLine($"  RecordCount: {summary.RecordCount}");
            Console.WriteLine($"  Fields: {string.Join(", ", summary.Fields.Take(60))}");

            if (summary.Fields.Count > 60)
            {
                Console.WriteLine($"  FieldsMore: {summary.Fields.Count - 60}");
            }

            if (summary.Error is not null)
            {
                Console.WriteLine($"  Error: {summary.Error}");
            }

            Console.WriteLine("");
        }
    }

    private static void RunRecords(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --records .\iw_workspace_vNext Items 3");
            return;
        }

        var workspacePath = args[1];
        var hint = args[2];
        var count = 5;

        if (args.Length >= 4 && int.TryParse(args[3], out var parsedCount))
        {
            count = parsedCount;
        }

        var repository = new GameDataRepository();

        var matchingFiles = repository.ListMatchingFiles(workspacePath, hint);

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Hint: {hint}");
        Console.WriteLine("Matching files:");

        foreach (var file in matchingFiles)
        {
            Console.WriteLine($"  {file}");
        }

        Console.WriteLine("");

        var records = repository.LoadRecords(workspacePath, hint);

        Console.WriteLine($"Records loaded: {records.Count}");

        foreach (var record in records.Take(count))
        {
            Console.WriteLine("");
            Console.WriteLine($"[{record.Index}] {record.SourceFile}");

            foreach (var pair in record.Fields)
            {
                Console.WriteLine($"  {pair.Key}: {pair.Value}");
            }
        }
    }

    private static void RunFindRecord(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine(@"Usage: --find-record .\iw_workspace_vNext Items Name SomeName");
            return;
        }

        var workspacePath = args[1];
        var hint = args[2];
        var field = args[3];
        var value = args[4];

        var repository = new GameDataRepository();
        var records = repository.LoadRecords(workspacePath, hint);

        var matches = records
            .Where(
                record =>
                    record.Fields.TryGetValue(field, out var actual)
                    && actual.Contains(value, StringComparison.OrdinalIgnoreCase)
            )
            .Take(20)
            .ToList();

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Hint: {hint}");
        Console.WriteLine($"Find: {field} contains {value}");
        Console.WriteLine($"Matches shown: {matches.Count}");

        foreach (var record in matches)
        {
            Console.WriteLine("");
            Console.WriteLine($"[{record.Index}] {record.SourceFile}");

            foreach (var pair in record.Fields)
            {
                Console.WriteLine($"  {pair.Key}: {pair.Value}");
            }
        }
    }

    private static void RunEffects(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --effects .\iw_workspace_vNext Items 10");
            return;
        }

        var workspacePath = args[1];
        var hint = args[2];
        var count = 20;

        if (args.Length >= 4 && int.TryParse(args[3], out var parsedCount))
        {
            count = parsedCount;
        }

        var extractor = new RawEffectExtractor();
        var rawEffects = extractor.Extract(workspacePath, hint);

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Hint: {hint}");
        Console.WriteLine($"Raw effects extracted: {rawEffects.Count}");

        foreach (var effect in rawEffects.Take(count))
        {
            Console.WriteLine("");
            Console.WriteLine($"{effect.SourceKind} {effect.SourceId} {effect.SourceName}");
            Console.WriteLine($"  SourceFile: {effect.SourceFile}");
            Console.WriteLine($"  SourcePath: {effect.SourcePath}");
            Console.WriteLine($"  Target: {effect.Target}");
            Console.WriteLine($"  Effect: {effect.Effect}");
            Console.WriteLine($"  Addendum: {effect.Addendum}");
            Console.WriteLine($"  Multiplier: {effect.Multiplier}");
            Console.WriteLine($"  Diminish: {effect.Diminish}");
            Console.WriteLine($"  Notes: {effect.Notes}");
        }
    }

    private static void RunConvertEffects(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine(@"Usage: --convert-effects .\iw_workspace_vNext Items 10");
            return;
        }

        var workspacePath = args[1];
        var hint = args[2];
        var count = 20;

        if (args.Length >= 4 && int.TryParse(args[3], out var parsedCount))
        {
            count = parsedCount;
        }

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();

        var rawEffects = extractor.Extract(workspacePath, hint);
        var conversions = rawEffects
            .Select(effect => converter.Convert(effect))
            .ToList();

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Hint: {hint}");
        Console.WriteLine($"Raw effects: {rawEffects.Count}");
        Console.WriteLine($"Convertible: {conversions.Count(x => x.Status == RawEffectConversionStatus.Convertible)}");
        Console.WriteLine($"UnsupportedEffectType: {conversions.Count(x => x.Status == RawEffectConversionStatus.UnsupportedEffectType)}");
        Console.WriteLine($"MissingTarget: {conversions.Count(x => x.Status == RawEffectConversionStatus.MissingTarget)}");
        Console.WriteLine($"InvalidNumber: {conversions.Count(x => x.Status == RawEffectConversionStatus.InvalidNumber)}");
        Console.WriteLine("");

        Console.WriteLine("First converted/unresolved records:");

        foreach (var conversion in conversions.Take(count))
        {
            var effect = conversion.Descriptor;

            Console.WriteLine("");
            Console.WriteLine($"{conversion.Status}: {effect.SourceKind} {effect.SourceId} {effect.SourceName}");
            Console.WriteLine($"  SourceFile: {effect.SourceFile}");
            Console.WriteLine($"  SourcePath: {effect.SourcePath}");
            Console.WriteLine($"  Target: {conversion.Target}");
            Console.WriteLine($"  Effect: {effect.Effect}");
            Console.WriteLine($"  Addendum: {conversion.Addendum}");
            Console.WriteLine($"  Multiplier: {conversion.Multiplier}");
            Console.WriteLine($"  Diminish: {effect.Diminish}");
            Console.WriteLine($"  Message: {conversion.Message}");
            Console.WriteLine($"  Notes: {effect.Notes}");
        }

        Console.WriteLine("");
        Console.WriteLine("Top target resources:");

        foreach (
            var group in conversions
                .Where(x => !string.IsNullOrWhiteSpace(x.Target))
                .GroupBy(x => x.Target)
                .OrderByDescending(x => x.Count())
                .ThenBy(x => x.Key)
                .Take(25)
        )
        {
            Console.WriteLine($"  {group.Key}: {group.Count()}");
        }
    }

    private static void RunEvaluateItem(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --evaluate-item .\iw_workspace_vNext 1 4");
            return;
        }

        var workspacePath = args[1];
        var itemId = args[2];
        var tier = args[3];

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();

        var itemRawEffects = extractor
            .Extract(workspacePath, "Items")
            .Where(
                effect =>
                    effect.SourceId.Equals(itemId, StringComparison.OrdinalIgnoreCase)
                    && effect.Notes.Equals($"Tier={tier}", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Item ID: {itemId}");
        Console.WriteLine($"Tier: {tier}");
        Console.WriteLine($"Raw item tier effects: {itemRawEffects.Count}");
        Console.WriteLine("");

        if (itemRawEffects.Count == 0)
        {
            Console.WriteLine("No item effects found for that item/tier.");
            Console.WriteLine("Try:");
            Console.WriteLine(@"  --records .\iw_workspace_vNext Items 10");
            return;
        }

        var context = new CalculatorContext();
        var convertedEffects = new List<ICalcEffect>();

        foreach (var rawEffect in itemRawEffects)
        {
            var conversion = converter.Convert(rawEffect);

            Console.WriteLine($"{conversion.Status}: {rawEffect.SourceName}");
            Console.WriteLine($"  SourcePath: {rawEffect.SourcePath}");
            Console.WriteLine($"  Target: {rawEffect.Target}");
            Console.WriteLine($"  Effect: {rawEffect.Effect}");
            Console.WriteLine($"  Addendum: {rawEffect.Addendum}");
            Console.WriteLine($"  Multiplier: {rawEffect.Multiplier}");
            Console.WriteLine($"  Diminish: {rawEffect.Diminish}");
            Console.WriteLine($"  Message: {conversion.Message}");
            Console.WriteLine("");

            if (conversion.Status != RawEffectConversionStatus.Convertible)
            {
                continue;
            }

            if (!context.Resources.TryGet(rawEffect.Target, out _))
            {
                context.Resources.Register(
                    new CalcVariableComplex(
                        rawEffect.Target,
                        CalcBigNumber.One
                    )
                );
            }

            convertedEffects.Add(converter.ToLinearEffect(rawEffect));
        }

        Console.WriteLine("Before applying effects:");

        foreach (var resource in context.Resources.All.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {resource.Key}: {resource.Value.Value}");
        }

        var itemReport = new BuildEvaluator().Evaluate(context, convertedEffects);

        Console.WriteLine("");
        Console.WriteLine("Applied effects:");

        foreach (var effect in itemReport.Effects)
        {
            Console.WriteLine(
                $"  {effect.Status}: {effect.SourceId} -> {effect.TargetKey} " +
                $"[{effect.Operation}] {effect.Value} {effect.Message}"
            );
        }

        Console.WriteLine("");
        Console.WriteLine("After applying effects:");

        foreach (var resource in context.Resources.All.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {resource.Key}: {resource.Value.Value}");
        }

        Console.WriteLine("");
        Console.WriteLine($"Fully verified: {itemReport.IsFullyVerified}");
        Console.WriteLine("");
        Console.WriteLine("Note: this is relative evaluation using base value 1 for each target.");
        Console.WriteLine("Full build evaluation will use user-entered or imported current stat values.");
    }

    private static void RunEvaluateItemValues(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage: --evaluate-item-values .\iw_workspace_vNext 1 4 ""Base.SoulPower=10"" ""Char.Intelligence=150""");
            return;
        }

        var workspacePath = args[1];
        var itemId = args[2];
        var tier = args[3];

        var userValues = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
        );

        string? efficiency = null;
        string? gilding = null;

        foreach (var rawArg in args.Skip(4))
        {
            if (rawArg.StartsWith("--efficiency=", StringComparison.OrdinalIgnoreCase))
            {
                efficiency = rawArg.Split("=", 2)[1];
                continue;
            }

            if (rawArg.StartsWith("--gilding=", StringComparison.OrdinalIgnoreCase))
            {
                gilding = rawArg.Split("=", 2)[1];
                continue;
            }

            var split = rawArg.Split("=", 2);

            if (split.Length != 2)
            {
                Console.WriteLine($"Ignoring invalid stat argument: {rawArg}");
                Console.WriteLine("Expected format: Resource.Key=123 or Resource.Key=1e50");
                continue;
            }

            userValues[split[0]] = split[1];
        }

        var extractor = new RawEffectExtractor();
        var converter = new RawEffectConverter();

        var itemRawEffects = extractor
            .Extract(workspacePath, "Items")
            .Where(
                effect =>
                    effect.SourceId.Equals(itemId, StringComparison.OrdinalIgnoreCase)
                    && effect.Notes.Equals($"Tier={tier}", StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        Console.WriteLine($"Workspace: {workspacePath}");
        Console.WriteLine($"Item ID: {itemId}");
        Console.WriteLine($"Tier: {tier}");
        Console.WriteLine($"Raw item tier effects: {itemRawEffects.Count}");
        Console.WriteLine($"User-provided stat values: {userValues.Count}");
        Console.WriteLine($"Efficiency: {efficiency ?? "not provided"}");
        Console.WriteLine($"Gilding: {gilding ?? "not provided"}");
        Console.WriteLine("");

        if (itemRawEffects.Count == 0)
        {
            Console.WriteLine("No item effects found for that item/tier.");
            return;
        }

        var context = new CalculatorContext();
        var convertedEffects = new List<ICalcEffect>();

        foreach (var rawEffect in itemRawEffects)
        {
            var conversion = converter.Convert(rawEffect);

            Console.WriteLine($"{conversion.Status}: {rawEffect.SourceName}");
            Console.WriteLine($"  SourcePath: {rawEffect.SourcePath}");
            Console.WriteLine($"  Target: {rawEffect.Target}");
            Console.WriteLine($"  Effect: {rawEffect.Effect}");
            Console.WriteLine($"  Addendum: {rawEffect.Addendum}");
            Console.WriteLine($"  Multiplier: {rawEffect.Multiplier}");
            Console.WriteLine($"  Diminish: {rawEffect.Diminish}");
            Console.WriteLine($"  Message: {conversion.Message}");

            if (userValues.TryGetValue(rawEffect.Target, out var providedValue))
            {
                Console.WriteLine($"  UserValue: {providedValue}");
            }
            else
            {
                Console.WriteLine("  UserValue: not provided, using 1");
            }

            Console.WriteLine("");

            if (conversion.Status != RawEffectConversionStatus.Convertible)
            {
                continue;
            }

            if (!context.Resources.TryGet(rawEffect.Target, out _))
            {
                var initialValue = CalcBigNumber.One;

                if (userValues.TryGetValue(rawEffect.Target, out var provided))
                {
                    try
                    {
                        initialValue = CalcBigNumber.Parse(provided);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"Could not parse provided value for {rawEffect.Target}: {provided}"
                        );
                        Console.WriteLine($"Parse error: {ex.Message}");
                        Console.WriteLine("Using 1 instead.");
                        initialValue = CalcBigNumber.One;
                    }
                }

                context.Resources.Register(
                    new CalcVariableComplex(
                        rawEffect.Target,
                        initialValue
                    )
                );
            }

            convertedEffects.Add(
                converter.ToLinearEffect(
                    rawEffect,
                    efficiency,
                    gilding
                )
            );
        }

        var beforeValues = context.Resources.All
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Value,
                StringComparer.OrdinalIgnoreCase
            );

        Console.WriteLine("Before applying effects:");

        foreach (var resource in context.Resources.All.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  {resource.Key}: {resource.Value.Value}");
        }

        var itemReport = new BuildEvaluator().Evaluate(
            context,
            convertedEffects
        );

        Console.WriteLine("");
        Console.WriteLine("Applied effects:");

        foreach (var appliedEffect in itemReport.Effects)
        {
            Console.WriteLine(
                $"  {appliedEffect.Status}: {appliedEffect.SourceId} -> {appliedEffect.TargetKey} " +
                $"[{appliedEffect.Operation}] {appliedEffect.Value} {appliedEffect.Message}"
            );
        }

        Console.WriteLine("");
        Console.WriteLine("After applying effects:");

        foreach (var resource in context.Resources.All.OrderBy(x => x.Key))
        {
            var before = beforeValues[resource.Key];
            var after = resource.Value.Value;

            Console.WriteLine($"  {resource.Key}: {before} -> {after}");
        }

        Console.WriteLine("");
        Console.WriteLine($"Fully verified: {itemReport.IsFullyVerified}");
        Console.WriteLine("");
        Console.WriteLine("Note: Item pow-diminish is applied when --efficiency and/or --gilding are supplied.");
        Console.WriteLine("If neither is supplied, raw addendum/multiplier are applied directly.");
    }

    private static void RunDemo()
    {
        var ctx = new CalculatorContext();

        ctx.Resources.Register(
            new CalcVariableComplex(
                "Demo.ManaProfit",
                CalcBigNumber.Parse("1e100")
            )
        );

        var effects = new ICalcEffect[]
        {
            new LinearEffect(
                "demo:item:verified-linear",
                "Demo.ManaProfit",
                CalcBigNumber.Zero,
                CalcBigNumber.Parse("2")
            ),

            new LinearEffect(
                "demo:item:missing-target",
                "Missing.Target",
                CalcBigNumber.Zero,
                CalcBigNumber.Parse("2")
            ),
        };

        var report = new BuildEvaluator().Evaluate(ctx, effects);

        Console.WriteLine($"Fully verified: {report.IsFullyVerified}");

        foreach (var effect in report.Effects)
        {
            Console.WriteLine(
                $"{effect.Status}: {effect.SourceId} -> {effect.TargetKey} " +
                $"[{effect.Operation}] {effect.Value} {effect.Message}"
            );
        }

        Console.WriteLine(
            $"Demo.ManaProfit = {ctx.Resources.All["Demo.ManaProfit"].Value}"
        );

        Console.WriteLine("");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Commands:");
        Console.WriteLine(@"  --audit .\iw_workspace_vNext\verified_source_index.json");
        Console.WriteLine(@"  --data-summary .\iw_workspace_vNext");
        Console.WriteLine(@"  --records .\iw_workspace_vNext Items 3");
        Console.WriteLine(@"  --records .\iw_workspace_vNext Spells 3");
        Console.WriteLine(@"  --records .\iw_workspace_vNext Upgrades 3");
        Console.WriteLine(@"  --find-record .\iw_workspace_vNext Items Name SomeName");
        Console.WriteLine(@"  --effects .\iw_workspace_vNext Items 10");
        Console.WriteLine(@"  --convert-effects .\iw_workspace_vNext Items 10");
        Console.WriteLine(@"  --evaluate-item .\iw_workspace_vNext 1 4");
        Console.WriteLine(@"  --evaluate-item-values .\iw_workspace_vNext 1 4 ""Base.SoulPower=10"" ""Char.Intelligence=150""");
        Console.WriteLine(@"  --compare-items .\iw_workspace_vNext 1:4 0:5 ""Base.SoulPower=10"" ""Char.Intelligence=150"" ""Experiment.Efficiency=20"" ""Hero.AbilityPower=100"" ""--efficiency=2"" ""--gilding=1.5""");
        Console.WriteLine(@"  --score-items .\iw_workspace_vNext Hero.AbilityPower 1:4 0:5 ""Hero.AbilityPower=100"" ""Base.SoulPower=10"" ""Char.Intelligence=150"" ""Experiment.Efficiency=20"" ""--efficiency=2"" ""--gilding=1.5""");
        Console.WriteLine(@"  --score-slot .\iw_workspace_vNext Head Hero.AbilityPower ""Hero.AbilityPower=100"" ""--efficiency=2"" ""--gilding=1.5"" ""--equipped=0:5""");
        Console.WriteLine(@"  --score-slot-weighted .\iw_workspace_vNext Head ""Hero.AbilityPower@1.0"" ""Experiment.Efficiency@0.2"" ""Hero.ExpBoost@0.1"" ""Hero.AbilityPower=100"" ""Experiment.Efficiency=20"" ""Hero.ExpBoost=1"" ""--efficiency=2"" ""--gilding=1.5"" ""--equipped=0:5""");
        Console.WriteLine(@"  --score-slot-weighted-file .\scenario_head_hero.json");
        Console.WriteLine(@"  --enchant-value 0.15 5 5");
        Console.WriteLine(@"  --item-enchant-value .\iw_workspace_vNext 0 5 5");
        Console.WriteLine(@"  --enchant-plan-scenario .\scenario_head_hero.json");
        Console.WriteLine(@"  --spell-catalog .\iw_workspace_vNext Temporal");
        Console.WriteLine(@"  --phase-class-summary .\scenario_head_hero.json");
        Console.WriteLine(@"  --class-spell-map .\iw_workspace_vNext Temporalist");
        Console.WriteLine(@"  --validate-phase-spells .\scenario_head_hero.json");
        Console.WriteLine(@"  --spell-variants .\iw_workspace_vNext Temporalist");
        Console.WriteLine(@"  --scenario-options .\iw_workspace_vNext .\scenario_options.json");
        Console.WriteLine(@"  --pet-options .\iw_workspace_vNext .\pet_options.json");
        Console.WriteLine(@"  --validate-phase-pets .\scenario_head_hero.json .\pet_validation_head_hero.json");
        Console.WriteLine(@"  --inspect-save-string .\save_export.txt .\save_inspect.json");
        Console.WriteLine(@"  --class-source-diagnostics .\iw_workspace_vNext Temporalist");
        Console.WriteLine(@"  --hero-impl-diagnostics .\iw_workspace_vNext Temporalist");
    }
}


































