// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serde.Json;
using Microsoft.Build.Logging.StructuredLogger;
using Serde;

//
// Script
//

var testProjectsPath = Path.GetFullPath(Path.Combine(GetThisFilePath(), "..", "..", "test_projects"));

Console.WriteLine(testProjectsPath);

var testProjectPaths = Directory.EnumerateFiles(testProjectsPath, "*.csproj", SearchOption.AllDirectories);

Dictionary<string, TestBaseline> baselines = new();

foreach (var projPath in testProjectPaths)
{
    var binlogPath = Path.Combine(projPath, "..", "msbuild.binlog");
    await DotnetBuild(projPath, binlogPath);

    string? cscArgs = null;
    var buildRoot = BinaryLog.ReadBuild(binlogPath);
    buildRoot.VisitAllChildren<CscTask>(c =>
    {
        Debug.Assert(cscArgs is null);
        cscArgs = c.CommandLineArguments;
    });
    Debug.Assert(cscArgs is not null);

    baselines[projPath] = new TestBaseline(cscArgs);
}

var serializedBaselines = JsonSerializer.Serialize(new DictWrap.SerializeImpl<string, StringWrap, TestBaseline, IdWrap<TestBaseline>>(baselines));
File.WriteAllText(Path.Combine(GetThisFilePath(), "..", "..", "test_baselines.json"), serializedBaselines);

return 0;

//
// Helpers
//

static async Task<int> DotnetBuild(string projPath, string binlogPath)
{
    ProcessStartInfo startInfo = new()
    {
        FileName = "dotnet",
        Arguments = $"build {projPath} -bl:\"{binlogPath}\"",
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

static string GetThisFilePath([CallerFilePath] string path = "") => path;

[GenerateSerialize]
partial record TestBaseline
(
    string CscArgs
);

record FileHash (string RelativePath, string Hash);
