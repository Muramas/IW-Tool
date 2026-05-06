using System.Text.Json;

namespace IdleWizard.BuildTool.Core.Workspace;

public static class WorkspacePaths
{
    public static string ResolveExportRoot(string workspacePath)
    {
        var configPath = Path.Combine(workspacePath, "workspace_config.json");

        if (File.Exists(configPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
            var root = doc.RootElement;

            if (root.TryGetProperty("exportRoot", out var exportRootElement))
            {
                var raw = exportRootElement.GetString();

                if (!string.IsNullOrWhiteSpace(raw))
                {
                    return ResolvePathRelativeToWorkspace(workspacePath, raw);
                }
            }
        }

        var rawExport = Path.Combine(workspacePath, "raw_export");

        if (Directory.Exists(rawExport))
        {
            return rawExport;
        }

        var rawFiles = Path.Combine(workspacePath, "raw_files");

        if (Directory.Exists(rawFiles))
        {
            return rawFiles;
        }

        return workspacePath;
    }

    public static string ResolveScriptsRoot(string workspacePath)
    {
        var exportRoot = ResolveExportRoot(workspacePath);

        var candidates = new[]
        {
            Path.Combine(exportRoot, "Scripts"),
            Path.Combine(exportRoot, "raw_files", "Scripts"),
            Path.Combine(workspacePath, "raw_files", "Scripts")
        };

        return candidates.FirstOrDefault(Directory.Exists)
            ?? Path.Combine(exportRoot, "Scripts");
    }

    public static string ResolveAssetsRoot(string workspacePath)
    {
        var exportRoot = ResolveExportRoot(workspacePath);

        var candidates = new[]
        {
            Path.Combine(exportRoot, "Assets"),
            Path.Combine(exportRoot, "raw_files", "Assets"),
            Path.Combine(workspacePath, "raw_files", "Assets")
        };

        return candidates.FirstOrDefault(Directory.Exists)
            ?? Path.Combine(exportRoot, "Assets");
    }

    public static string ResolveAssemblyCSharpRoot(string workspacePath)
    {
        return Path.Combine(
            ResolveScriptsRoot(workspacePath),
            "Assembly-CSharp"
        );
    }

    public static string? FindSourceFile(string workspacePath, string fileName)
    {
        var scriptsRoot = ResolveScriptsRoot(workspacePath);

        if (!Directory.Exists(scriptsRoot))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(scriptsRoot, fileName, SearchOption.AllDirectories)
            .OrderBy(x => x)
            .FirstOrDefault();
    }

    public static string? FindDataFile(string workspacePath, string logicalName)
    {
        var exportRoot = ResolveExportRoot(workspacePath);

        var candidates = new[]
        {
            logicalName + ".bytes",
            logicalName + ".bytes.txt",
            logicalName + ".json"
        };

        return Directory
            .EnumerateFiles(exportRoot, "*", SearchOption.AllDirectories)
            .Where(path => candidates.Any(
                candidate => Path.GetFileName(path).Equals(
                    candidate,
                    StringComparison.OrdinalIgnoreCase
                )
            ))
            .OrderBy(path => path)
            .FirstOrDefault();
    }

    private static string ResolvePathRelativeToWorkspace(
        string workspacePath,
        string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var workspaceFull = Path.GetFullPath(workspacePath);
        return Path.GetFullPath(Path.Combine(workspaceFull, path));
    }
}
