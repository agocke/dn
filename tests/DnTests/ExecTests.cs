
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Dn.Test;

public sealed class ExecTests : IDisposable
{
    private readonly TempDirectory _tempDir = TempDirectory.CreateSubDirectory();
    private readonly ITestOutputHelper _outputHelper;

    private readonly DnEnv _env;

    public ExecTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
        _env = new DnEnv(_tempDir.Path, new TestWriter(_outputHelper));
    }

    void IDisposable.Dispose()
    {
        _tempDir.Dispose();
    }

    [Fact]
    public void HelloWorld()
    {
        _outputHelper.WriteLine($"TempDir: {_tempDir.Path}");
        _tempDir.CopyFile(ResolveRelativePath("../test_baselines/test_projects/HelloWorldNet8/Program.cs"));
        _tempDir.CopyFile(ResolveRelativePath("../test_baselines/test_projects/HelloWorldNet8/HelloWorldNet8.csproj"));
        int code = BuildCommand.Execute(_env, new BuildArguments());
        Assert.Equal(0, code);
        string outDll = Path.Combine(_tempDir.Path, "obj/Debug/net8.0/HelloWorldNet8.dll");
        Assert.True(File.Exists(outDll));
        (int exitCode, string stdout, string stderr) = RunDotnet(outDll);
        _outputHelper.WriteLine(stdout);
        _outputHelper.WriteLine(stderr);
        Assert.Equal(0, exitCode);
    }

    private (int ExitCode, string Out, string Err) RunDotnet(string args)
    {
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _tempDir.Path
        };
        var p = Process.Start(psi)!;
        p.WaitForExit();
        return (p.ExitCode, p.StandardOutput.ReadToEnd(), p.StandardError.ReadToEnd());
    }

    public static string ResolveRelativePath(string relativePath, [CallerFilePath] string thisPath = "")
    {
        return Path.Combine(Path.GetDirectoryName(thisPath)!, relativePath);
    }
}
