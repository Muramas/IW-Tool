using System.Text.Json;
using System.Text.RegularExpressions;
using IdleWizard.BuildTool.Core.Workspace;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class PetOptionsCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine(@"Usage: --pet-options .\iw_workspace_vNext [pet_options.json]");
            return;
        }

        var workspacePath = args[1];
        var outputPath = args.Length >= 3
            ? args[2]
            : ".\\pet_options.json";

        var pets = LoadPetsFromSource(workspacePath);

        var export = new PetOptionsExport(
            DateTime.UtcNow.ToString("O"),
            workspacePath,
            WorkspacePaths.ResolveExportRoot(workspacePath),
            pets,
            new[]
            {
                "Pets are source-scanned from PetNames.cs and implementation files.",
                "Familiars are a separate system and are not included here.",
                "Some display names may fall back to enum keys if source display names are not found."
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

        Console.WriteLine("Pet options");
        Console.WriteLine("-----------");
        Console.WriteLine($"Workspace:   {workspacePath}");
        Console.WriteLine($"Export root: {WorkspacePaths.ResolveExportRoot(workspacePath)}");
        Console.WriteLine($"Pets:        {pets.Count}");
        Console.WriteLine($"Output:      {Path.GetFullPath(outputPath)}");

        Console.WriteLine("");
        foreach (var pet in pets)
        {
            Console.WriteLine($"{pet.Name} ({pet.Key})");
        }
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
            var text = File.ReadAllText(file);
            var keyMatch = keyPattern.Match(text);

            if (!keyMatch.Success)
            {
                continue;
            }

            var windowStart = Math.Max(0, keyMatch.Index - 800);
            var windowLength = Math.Min(text.Length - windowStart, 1800);
            var window = text.Substring(windowStart, windowLength);

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

    private sealed record PetOptionsExport(
        string GeneratedAtUtc,
        string Workspace,
        string ExportRoot,
        IReadOnlyList<PetOption> Pets,
        IReadOnlyList<string> Warnings
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
