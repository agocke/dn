using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Dn.Test;

public sealed class RestoreTests : IDisposable
{
    private readonly TempDirectory _tempDir = TempDirectory.CreateSubDirectory();
    private readonly string _savedWorkingDirectory = Environment.CurrentDirectory;
    private readonly ITestOutputHelper _outputHelper;
    private readonly DnEnv _env;

    // Path to test baselines directory
    private static string TestBaselinesPath => ResolveRelativePath("../test_baselines");
    private static string TestProjectsPath => Path.Combine(TestBaselinesPath, "test_projects");

    public RestoreTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _env = new DnEnv(_tempDir.Path, new TestWriter(_outputHelper));
        Environment.CurrentDirectory = _tempDir.Path;
    }

    void IDisposable.Dispose()
    {
        Environment.CurrentDirectory = _savedWorkingDirectory;
        _tempDir.Dispose();
    }

    /// <summary>
    /// Get the path to a test project directory
    /// </summary>
    private static string GetTestProjectPath(string projectName) =>
        Path.Combine(TestProjectsPath, projectName);

    /// <summary>
    /// Read the project.assets.json baseline for a test project (if it exists)
    /// </summary>
    private static string? ReadProjectAssetsBaseline(string projectName)
    {
        var baselinePath = Path.Combine(GetTestProjectPath(projectName), "project.assets.json.baseline");
        return File.Exists(baselinePath) ? File.ReadAllText(baselinePath) : null;
    }

    [Fact]
    public async Task RestoreSimplePackage()
    {
        // Create a simple project with a package reference
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;

        var programContent = """
            using Newtonsoft.Json;
            var json = JsonConvert.SerializeObject(new { Name = "Test" });
            System.Console.WriteLine(json);
            """;

        File.WriteAllText(Path.Combine(_tempDir.Path, "TestProject.csproj"), projectContent);
        File.WriteAllText(Path.Combine(_tempDir.Path, "Program.cs"), programContent);

        // Run restore
        var restoreArgs = new RestoreCommand.RestoreArguments
        {
            ProjectPath = Path.Combine(_tempDir.Path, "TestProject.csproj"),
            Force = false,
            NoCache = false
        };

        int exitCode = await RestoreCommand.ExecuteAsync(_env, restoreArgs);

        Assert.Equal(0, exitCode);

        // Verify assets file was created
        var assetsPath = Path.Combine(_tempDir.Path, "obj", "project.assets.json");
        Assert.True(File.Exists(assetsPath), "project.assets.json should be created");

        // Read and validate the assets file
        var assetsJson = File.ReadAllText(assetsPath);
        var assets = JsonNode.Parse(assetsJson);
        Assert.NotNull(assets);

        // Verify it has the expected structure
        Assert.NotNull(assets["version"]);
        Assert.Equal(3, assets["version"]!.GetValue<int>());

        // Verify targets section contains net8.0
        var targets = assets["targets"];
        Assert.NotNull(targets);
        Assert.NotNull(targets["net8.0"]);

        // Verify Newtonsoft.Json is in the targets
        var net8Target = targets["net8.0"]!.AsObject();
        var newtonsoftEntry = net8Target.FirstOrDefault(kvp => kvp.Key.StartsWith("Newtonsoft.Json/"));
        Assert.NotNull(newtonsoftEntry.Value);

        // Verify libraries section
        var libraries = assets["libraries"];
        Assert.NotNull(libraries);
        var newtonsoftLib = libraries!.AsObject().FirstOrDefault(kvp => kvp.Key.StartsWith("Newtonsoft.Json/"));
        Assert.NotNull(newtonsoftLib.Value);
        Assert.Equal("package", newtonsoftLib.Value!["type"]!.GetValue<string>());
    }

    [Fact]
    public async Task RestoreWithTransitiveDependency()
    {
        // Create a project with a package that has transitive dependencies
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
              </ItemGroup>
            </Project>
            """;

        var programContent = """
            System.Console.WriteLine("Hello");
            """;

        File.WriteAllText(Path.Combine(_tempDir.Path, "TestProject.csproj"), projectContent);
        File.WriteAllText(Path.Combine(_tempDir.Path, "Program.cs"), programContent);

        // Run restore
        var restoreArgs = new RestoreCommand.RestoreArguments
        {
            ProjectPath = Path.Combine(_tempDir.Path, "TestProject.csproj"),
            Force = false,
            NoCache = false
        };

        int exitCode = await RestoreCommand.ExecuteAsync(_env, restoreArgs);

        Assert.Equal(0, exitCode);

        // Verify assets file
        var assetsPath = Path.Combine(_tempDir.Path, "obj", "project.assets.json");
        Assert.True(File.Exists(assetsPath));

        var assetsJson = File.ReadAllText(assetsPath);
        var assets = JsonNode.Parse(assetsJson);

        // Check for transitive dependencies (Logging has several)
        var libraries = assets!["libraries"]!.AsObject();

        // Microsoft.Extensions.Logging depends on Microsoft.Extensions.DependencyInjection.Abstractions
        // and Microsoft.Extensions.Logging.Abstractions
        var hasLogging = libraries.Any(kvp => kvp.Key.StartsWith("Microsoft.Extensions.Logging/"));
        var hasLoggingAbstractions = libraries.Any(kvp => kvp.Key.StartsWith("Microsoft.Extensions.Logging.Abstractions/"));
        var hasDIAbstractions = libraries.Any(kvp => kvp.Key.StartsWith("Microsoft.Extensions.DependencyInjection.Abstractions/"));

        Assert.True(hasLogging, "Should have Microsoft.Extensions.Logging");
        Assert.True(hasLoggingAbstractions, "Should have Microsoft.Extensions.Logging.Abstractions (transitive)");
        Assert.True(hasDIAbstractions, "Should have Microsoft.Extensions.DependencyInjection.Abstractions (transitive)");
    }

    [Fact]
    public async Task RestoreMatchesBaseline()
    {
        // This test compares against baselines generated by MakeBaselines tool
        var projectDir = GetTestProjectPath("WithPackages");

        // Copy project files to temp dir (skip baseline and build artifacts)
        foreach (var file in Directory.GetFiles(projectDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            if (!fileName.EndsWith(".binlog") && !fileName.EndsWith(".baseline"))
            {
                File.Copy(file, Path.Combine(_tempDir.Path, fileName));
            }
        }

        // Run restore
        var projectFile = Directory.GetFiles(_tempDir.Path, "*.csproj").First();
        var restoreArgs = new RestoreCommand.RestoreArguments
        {
            ProjectPath = projectFile,
            Force = false,
            NoCache = false
        };

        int exitCode = await RestoreCommand.ExecuteAsync(_env, restoreArgs);
        Assert.Equal(0, exitCode);

        // Read baseline from separate file
        var baselineContent = ReadProjectAssetsBaseline("WithPackages");
        Assert.NotNull(baselineContent);

        // Compare with baseline - only compare the stable parts (targets, libraries, projectFileDependencyGroups)
        var actualAssetsPath = Path.Combine(_tempDir.Path, "obj", "project.assets.json");
        var baselineJson = JsonNode.Parse(baselineContent)!;
        var actualJson = JsonNode.Parse(File.ReadAllText(actualAssetsPath))!;

        // Compare version
        Assert.Equal(baselineJson["version"]!.GetValue<int>(), actualJson["version"]!.GetValue<int>());

        // Compare targets (the resolved package graph)
        AssertJsonEqual(baselineJson["targets"], actualJson["targets"], "targets");

        // Compare libraries (the downloaded packages)
        AssertJsonEqual(baselineJson["libraries"], actualJson["libraries"], "libraries");

        // Compare projectFileDependencyGroups (the direct dependencies)
        AssertJsonEqual(baselineJson["projectFileDependencyGroups"], actualJson["projectFileDependencyGroups"], "projectFileDependencyGroups");
    }

    private void AssertJsonEqual(JsonNode? expected, JsonNode? actual, string path)
    {
        if (expected is null && actual is null) return;
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        var expectedStr = expected.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        var actualStr = actual.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        if (expectedStr != actualStr)
        {
            _outputHelper.WriteLine($"Mismatch at {path}:");
            _outputHelper.WriteLine($"Expected:\n{expectedStr}");
            _outputHelper.WriteLine($"Actual:\n{actualStr}");
        }
        Assert.Equal(expectedStr, actualStr);
    }

    [Fact]
    public async Task RestoreNoPackageReferences()
    {
        // Project with no package references should still succeed
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """;

        var programContent = """
            System.Console.WriteLine("Hello");
            """;

        File.WriteAllText(Path.Combine(_tempDir.Path, "TestProject.csproj"), projectContent);
        File.WriteAllText(Path.Combine(_tempDir.Path, "Program.cs"), programContent);

        var restoreArgs = new RestoreCommand.RestoreArguments
        {
            ProjectPath = Path.Combine(_tempDir.Path, "TestProject.csproj"),
            Force = false,
            NoCache = false
        };

        int exitCode = await RestoreCommand.ExecuteAsync(_env, restoreArgs);

        Assert.Equal(0, exitCode);

        // Assets file should still be created
        var assetsPath = Path.Combine(_tempDir.Path, "obj", "project.assets.json");
        Assert.True(File.Exists(assetsPath));

        var assets = JsonNode.Parse(File.ReadAllText(assetsPath));
        var libraries = assets!["libraries"]!.AsObject();

        // Should have no package libraries
        Assert.Empty(libraries);
    }

    [Fact]
    public async Task RestoreGeneratesNuGetFiles()
    {
        var projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(_tempDir.Path, "TestProject.csproj"), projectContent);
        File.WriteAllText(Path.Combine(_tempDir.Path, "Program.cs"), "System.Console.WriteLine(\"Hello\");");

        var restoreArgs = new RestoreCommand.RestoreArguments
        {
            ProjectPath = Path.Combine(_tempDir.Path, "TestProject.csproj"),
            Force = false,
            NoCache = false
        };

        int exitCode = await RestoreCommand.ExecuteAsync(_env, restoreArgs);
        Assert.Equal(0, exitCode);

        // Check all expected files are generated
        var objDir = Path.Combine(_tempDir.Path, "obj");
        Assert.True(File.Exists(Path.Combine(objDir, "project.assets.json")), "project.assets.json should exist");
        Assert.True(File.Exists(Path.Combine(objDir, "project.nuget.cache")), "project.nuget.cache should exist");
        Assert.True(File.Exists(Path.Combine(objDir, "TestProject.csproj.nuget.g.props")), ".nuget.g.props should exist");
        Assert.True(File.Exists(Path.Combine(objDir, "TestProject.csproj.nuget.g.targets")), ".nuget.g.targets should exist");
        Assert.True(File.Exists(Path.Combine(objDir, "TestProject.csproj.nuget.dgspec.json")), ".nuget.dgspec.json should exist");
    }

    private static string ResolveRelativePath(string relativePath, [CallerFilePath] string thisPath = "")
    {
        return Path.Combine(Path.GetDirectoryName(thisPath)!, relativePath);
    }
}
