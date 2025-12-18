// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Build.Logging.StructuredLogger;

//
// Script
//

var testProjectsPath = Path.GetFullPath(Path.Combine(GetThisFilePath(), "..", "..", "test_projects"));

Console.WriteLine(testProjectsPath);

var testProjectPaths = Directory.EnumerateFiles(testProjectsPath, "*.csproj", SearchOption.AllDirectories);

foreach (var projPath in testProjectPaths)
{
    var projDir = Path.GetDirectoryName(projPath)!;
    var projName = Path.GetFileNameWithoutExtension(projPath);

    // Run restore first
    Console.WriteLine($"Restoring {projName}...");
    await DotnetRestore(projPath);

    // Capture project.assets.json if it exists (projects with PackageReferences)
    var assetsPath = Path.Combine(projDir, "obj", "project.assets.json");
    if (File.Exists(assetsPath))
    {
        // Normalize and write to baseline file
        var normalizedAssets = NormalizeProjectAssetsJson(File.ReadAllText(assetsPath));
        var baselineAssetsPath = Path.Combine(projDir, "project.assets.json.baseline");
        File.WriteAllText(baselineAssetsPath, normalizedAssets);
        Console.WriteLine($"  Wrote {baselineAssetsPath}");
    }

    // Run build and capture csc args
    Console.WriteLine($"Building {projName}...");
    var binlogPath = Path.Combine(projDir, "msbuild.binlog");
    await DotnetBuild(projPath, binlogPath);

    string? cscArgs = null;
    var buildRoot = BinaryLog.ReadBuild(binlogPath);
    buildRoot.VisitAllChildren<CscTask>(c =>
    {
        Debug.Assert(cscArgs is null);
        cscArgs = c.CommandLineArguments;
    });
    Debug.Assert(cscArgs is not null);

    // Write csc args to baseline file
    var baselineCscPath = Path.Combine(projDir, "csc.baseline");
    File.WriteAllText(baselineCscPath, cscArgs);
    Console.WriteLine($"  Wrote {baselineCscPath}");
}

return 0;

//
// Helpers
//

static async Task<int> DotnetRestore(string projPath)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "dotnet",
        Arguments = $"restore \"{projPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    var proc = Process.Start(startInfo)!;
    await proc.WaitForExitAsync();
    if (proc.ExitCode != 0)
    {
        Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
        Console.WriteLine(await proc.StandardError.ReadToEndAsync());
    }
    return proc.ExitCode;
}

static async Task<int> DotnetBuild(string projPath, string binlogPath)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "dotnet",
        Arguments = $"build \"{projPath}\" --no-incremental -bl:\"{binlogPath}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    var proc = Process.Start(startInfo)!;
    await proc.WaitForExitAsync();
    if (proc.ExitCode != 0)
    {
        Console.WriteLine(await proc.StandardOutput.ReadToEndAsync());
        Console.WriteLine(await proc.StandardError.ReadToEndAsync());
    }
    return proc.ExitCode;
}

/// <summary>
/// Normalize project.assets.json by removing machine-specific paths.
/// This makes the baselines portable across different machines.
/// </summary>
static string NormalizeProjectAssetsJson(string json)
{
    var doc = JsonNode.Parse(json);
    if (doc is null) return json;

    // Remove project section which contains absolute paths
    if (doc["project"] is JsonObject project)
    {
        if (project["restore"] is JsonObject restore)
        {
            restore.Remove("projectPath");
            restore.Remove("packagesPath");
            restore.Remove("outputPath");
            restore.Remove("projectJsonPath");
        }
    }

    // The targets and libraries sections are what we care about for correctness
    // They contain the resolved package versions and dependencies

    return doc.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
}

static string GetThisFilePath([CallerFilePath] string path = "") => path;
