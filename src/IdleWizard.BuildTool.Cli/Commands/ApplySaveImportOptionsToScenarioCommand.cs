using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace IdleWizard.BuildTool.Cli.Commands;

public static class ApplySaveImportOptionsToScenarioCommand
{
    public static void Run(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine(@"Usage:");
            Console.WriteLine(@"  --apply-save-import-options-to-scenario .\save_import_options.json .\scenario_in.json .\scenario_out.json");
            return;
        }

        var importOptionsPath = Path.GetFullPath(args[1]);
        var scenarioInPath = Path.GetFullPath(args[2]);
        var scenarioOutPath = Path.GetFullPath(args[3]);

        if (!File.Exists(importOptionsPath))
        {
            Console.WriteLine($"Save import options JSON not found: {importOptionsPath}");
            return;
        }

        if (!File.Exists(scenarioInPath))
        {
            Console.WriteLine($"Scenario JSON not found: {scenarioInPath}");
            return;
        }

        var importNode = JsonNode.Parse(File.ReadAllText(importOptionsPath));
        var scenarioNode = JsonNode.Parse(File.ReadAllText(scenarioInPath));

        if (importNode is not JsonObject importObject)
        {
            Console.WriteLine("Save import options root is not an object.");
            return;
        }

        if (scenarioNode is not JsonObject scenarioObject)
        {
            Console.WriteLine("Scenario root is not an object.");
            return;
        }

        var detectedClassName = ReadNestedString(importObject, "DetectedClass", "Name");
        var detectedClassId = ReadNestedInt(importObject, "DetectedClass", "Id");

        var detectedPetName = ReadNestedString(importObject, "DetectedPet", "Name");
        var detectedPetKey = ReadNestedString(importObject, "DetectedPet", "Key");
        var detectedPetId = ReadNestedInt(importObject, "DetectedPet", "Id");

        var petMaxLevel = ReadInt(importObject, "PetMaxLevel");

        ApplyGlobalContext(scenarioObject, detectedClassName, petMaxLevel);

        scenarioObject["saveImport"] = new JsonObject
        {
            ["appliedAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["sourceOptionsFile"] = importOptionsPath,
            ["detectedClass"] = new JsonObject
            {
                ["id"] = detectedClassId,
                ["name"] = detectedClassName
            },
            ["detectedPet"] = new JsonObject
            {
                ["id"] = detectedPetId,
                ["key"] = detectedPetKey,
                ["name"] = detectedPetName
            },
            ["petMaxLevel"] = petMaxLevel,
            ["presets"] = CloneProperty(importObject, "Presets"),
            ["spellbar"] = CloneProperty(importObject, "Spellbar"),
            ["notes"] = new JsonArray
            {
                "Preset names are user-defined. User should choose which preset maps to each phase.",
                "Spellbar is imported as observed selected spells. It is not automatically assigned to phases.",
                "This command does not overwrite phase equipment, phase pets, or phase spells."
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        File.WriteAllText(scenarioOutPath, scenarioObject.ToJsonString(options));

        Console.WriteLine("Apply save import options to scenario");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine($"Import options: {importOptionsPath}");
        Console.WriteLine($"Scenario in:    {scenarioInPath}");
        Console.WriteLine($"Scenario out:   {scenarioOutPath}");
        Console.WriteLine($"Class:          {detectedClassName} ({detectedClassId})");
        Console.WriteLine($"Pet:            {detectedPetName} ({detectedPetKey}, {detectedPetId})");
        Console.WriteLine($"Pet max level:  {petMaxLevel}");
        Console.WriteLine($"Presets:        {CountArray(importObject, "Presets")}");
        Console.WriteLine($"Spellbar:       {CountArray(importObject, "Spellbar")}");
    }

    private static void ApplyGlobalContext(
        JsonObject scenarioObject,
        string className,
        int petMaxLevel)
    {
        if (!scenarioObject.TryGetPropertyValue("globalContext", out var globalNode)
            || globalNode is not JsonObject globalObject)
        {
            globalObject = new JsonObject();
            scenarioObject["globalContext"] = globalObject;
        }

        if (!string.IsNullOrWhiteSpace(className))
        {
            globalObject["class"] = className;
        }

        if (petMaxLevel > 0)
        {
            globalObject["maxPetLevel"] = petMaxLevel.ToString();
        }
    }

    private static JsonNode CloneProperty(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node) || node is null)
        {
            return new JsonArray();
        }

        return node.DeepClone();
    }

    private static int CountArray(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var node)
            || node is not JsonArray array)
        {
            return 0;
        }

        return array.Count;
    }

    private static string ReadNestedString(
        JsonObject obj,
        string objectName,
        string propertyName)
    {
        if (!obj.TryGetPropertyValue(objectName, out var node)
            || node is not JsonObject nested)
        {
            return "";
        }

        if (!nested.TryGetPropertyValue(propertyName, out var value)
            || value is null)
        {
            return "";
        }

        return value.GetValue<string>();
    }

    private static int ReadNestedInt(
        JsonObject obj,
        string objectName,
        string propertyName)
    {
        if (!obj.TryGetPropertyValue(objectName, out var node)
            || node is not JsonObject nested)
        {
            return 0;
        }

        if (!nested.TryGetPropertyValue(propertyName, out var value)
            || value is null)
        {
            return 0;
        }

        if (value.GetValueKind() == JsonValueKind.Number)
        {
            return value.GetValue<int>();
        }

        return int.TryParse(value.GetValue<string>(), out var parsed)
            ? parsed
            : 0;
    }

    private static int ReadInt(JsonObject obj, string propertyName)
    {
        if (!obj.TryGetPropertyValue(propertyName, out var value)
            || value is null)
        {
            return 0;
        }

        if (value.GetValueKind() == JsonValueKind.Number)
        {
            return value.GetValue<int>();
        }

        return int.TryParse(value.GetValue<string>(), out var parsed)
            ? parsed
            : 0;
    }
}

