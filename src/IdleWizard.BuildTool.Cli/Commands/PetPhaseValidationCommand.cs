using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class PetPhaseValidationCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --validate-phase-pets .\scenario.json [pet_validation.json]");
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

        var workspaceRaw = GetRequiredString(scenario, "workspace");
        var workspacePath = ResolvePathRelativeToFile(scenarioPath, workspaceRaw);

        var pets = LoadPetsFromSource(workspacePath);
        var petIndex = BuildPetIndex(pets);

        var maxPetLevel = 0;

        if (scenario.TryGetProperty("globalContext", out var global)
            && global.ValueKind == JsonValueKind.Object)
        {
            int.TryParse(Get(global, "maxPetLevel"), out maxPetLevel);
        }

        var phases = new List<PhasePetValidationExport>();
        var warnings = 0;

        if (!scenario.TryGetProperty("phases", out var phaseArray)
            || phaseArray.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine("Scenario does not contain phases[].");
            return;
        }

        Console.WriteLine("Validate phase pets");
        Console.WriteLine("-------------------");
        Console.WriteLine($"Scenario:      {scenarioPath}");
        Console.WriteLine($"Workspace:     {workspacePath}");
        Console.WriteLine($"Pets:          {pets.Count}");
        Console.WriteLine($"Max pet level: {maxPetLevel}");
        Console.WriteLine("");

        foreach (var phase in phaseArray.EnumerateArray())
        {
            var phaseName = Get(phase, "name");
            var configuredPet = Get(phase, "pet");
            var rawLevel = Get(phase, "petLevel");

            int.TryParse(rawLevel, out var petLevel);

            var found = petIndex.TryGetValue(configuredPet, out var pet);
            var levelWithinMax = maxPetLevel <= 0 || petLevel <= maxPetLevel;

            var warning = "";

            if (string.IsNullOrWhiteSpace(configuredPet))
            {
                warning = "No pet configured.";
            }
            else if (!found)
            {
                warning = "Pet was not found in source-scanned pet list.";
            }
            else if (!levelWithinMax)
            {
                warning = "Configured pet level exceeds global maxPetLevel.";
            }

            if (!string.IsNullOrWhiteSpace(warning))
            {
                warnings++;
            }

            Console.WriteLine($"Phase: {phaseName}");
            Console.WriteLine($"  Configured pet: {configuredPet}");
            Console.WriteLine($"  Pet level:      {petLevel}");
            Console.WriteLine($"  Found:          {found}");

            if (found && pet is not null)
            {
                Console.WriteLine($"  Key:            {pet.Key}");
                Console.WriteLine($"  Name:           {pet.Name}");
                Console.WriteLine($"  Source file:    {pet.SourceFile}");
            }

            Console.WriteLine($"  Level within max: {levelWithinMax}");

            if (!string.IsNullOrWhiteSpace(warning))
            {
                Console.WriteLine($"  Warning: {warning}");
            }

            Console.WriteLine("");

            phases.Add(
                new PhasePetValidationExport(
                    phaseName,
                    configuredPet,
                    rawLevel,
                    petLevel,
                    found,
                    found && pet is not null ? pet.Key : "",
                    found && pet is not null ? pet.Name : "",
                    found && pet is not null ? pet.SourceFile : "",
                    found && pet is not null && pet.SourceBacked,
                    maxPetLevel,
                    levelWithinMax,
                    warning
                )
            );
        }

        var outputPath = "";

        if (args.Length >= 3)
        {
            outputPath = args[2];
        }
        else if (scenario.TryGetProperty("petValidationOutputJson", out var outputElement)
            && outputElement.ValueKind == JsonValueKind.String)
        {
            outputPath = outputElement.GetString() ?? "";
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = ResolvePathRelativeToFile(scenarioPath, outputPath);

            var export = new PetValidationExport(
                DateTime.UtcNow.ToString("O"),
                scenarioPath,
                workspacePath,
                pets.Count,
                maxPetLevel,
                phases,
                warnings,
                new[]
                {
                    "Pets are source-scanned from PetNames.cs and pet implementation files.",
                    "Familiars are a separate system and are not included here.",
                    "Pet formulas/effects are not implemented yet."
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

            Console.WriteLine($"Wrote pet validation JSON: {outputPath}");
            Console.WriteLine("");
        }

        Console.WriteLine("Validation summary:");
        Console.WriteLine($"  Warnings: {warnings}");
    }

    private static List<PetOption> LoadPetsFromSource(string workspacePath)
    {
        var sourceRoot = WorkspacePaths.ResolveAssemblyCSharpRoot(workspacePath);

        if (!Directory.Exists(sourceRoot))
        {
            return new List<PetOption>();
        }

        var petKeys = LoadPetKeys(workspacePath);
        var result = new List<PetOption>();

        foreach (var key in petKeys)
        {
            var found = FindPetImplementation(sourceRoot, key);

            result.Add(
                new PetOption(
                    key,
                    string.IsNullOrWhiteSpace(found.DisplayName) ? key : found.DisplayName,
                    found.SourceFile,
                    found.Found
                )
            );
        }

        return result
            .OrderBy(x => x.Name)
            .ToList();
    }

    private static Dictionary<string, PetOption> BuildPetIndex(IReadOnlyList<PetOption> pets)
    {
        var result = new Dictionary<string, PetOption>(StringComparer.OrdinalIgnoreCase);

        foreach (var pet in pets)
        {
            if (!string.IsNullOrWhiteSpace(pet.Key))
            {
                result[pet.Key] = pet;
            }

            if (!string.IsNullOrWhiteSpace(pet.Name))
            {
                result[pet.Name] = pet;
            }
        }

        return result;
    }

    private static List<string> LoadPetKeys(string workspacePath)
    {
        var file = WorkspacePaths.FindSourceFile(workspacePath, "PetNames.cs");

        if (file is null)
        {
            return new List<string>();
        }

        var text = File.ReadAllText(file);

        var enumMatch = Regex.Match(
            text,
            @"enum\s+PetNames\s*\{(?<body>[\s\S]*?)\}",
            RegexOptions.IgnoreCase
        );

        if (!enumMatch.Success)
        {
            return new List<string>();
        }

        var body = enumMatch.Groups["body"].Value;
        var keys = new List<string>();

        foreach (var rawPart in body.Split(','))
        {
            var part = rawPart.Trim();

            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var key = part.Split('=')[0].Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (key.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            keys.Add(key);
        }

        return keys
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }

    private static PetImplementation FindPetImplementation(
        string sourceRoot,
        string petKey)
    {
        var keyPattern = new Regex(
            @"NameKey\s*=\s*PetNames\." + Regex.Escape(petKey) + @"\b",
            RegexOptions.IgnoreCase
        );

        var namePattern = new Regex(
            @"(?:base\.)?Name\s*=\s*""(?<name>[^""]+)""",
            RegexOptions.IgnoreCase
        );

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            var sourceText = File.ReadAllText(file);
            var keyMatch = keyPattern.Match(sourceText);

            if (!keyMatch.Success)
            {
                continue;
            }

            var windowStart = Math.Max(0, keyMatch.Index - 800);
            var windowLength = Math.Min(sourceText.Length - windowStart, 1800);
            var window = sourceText.Substring(windowStart, windowLength);

            var nameMatch = namePattern.Match(window);
            var displayName = nameMatch.Success
                ? nameMatch.Groups["name"].Value
                : petKey;

            return new PetImplementation(
                true,
                displayName,
                Path.GetRelativePath(sourceRoot, file).Replace("\\", "/")
            );
        }

        return new PetImplementation(
            false,
            petKey,
            ""
        );
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

    private static string ResolvePathRelativeToFile(string baseFile, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var dir = Path.GetDirectoryName(baseFile) ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(dir, path));
    }

    private sealed record PetValidationExport(
        string GeneratedAtUtc,
        string Scenario,
        string Workspace,
        int PetCount,
        int MaxPetLevel,
        IReadOnlyList<PhasePetValidationExport> Phases,
        int WarningCount,
        IReadOnlyList<string> Warnings
    );

    private sealed record PhasePetValidationExport(
        string PhaseName,
        string ConfiguredPet,
        string RawPetLevel,
        int PetLevel,
        bool Found,
        string Key,
        string Name,
        string SourceFile,
        bool SourceBacked,
        int MaxPetLevel,
        bool LevelWithinMax,
        string Warning
    );

    private sealed record PetOption(
        string Key,
        string Name,
        string SourceFile,
        bool SourceBacked
    );

    private sealed record PetImplementation(
        bool Found,
        string DisplayName,
        string SourceFile
    );
}
