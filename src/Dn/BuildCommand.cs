using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using Internal.CommandLine;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Microsoft.CodeAnalysis.BuildTasks;

namespace Dn;

public sealed class BuildCommand
{
    public abstract record Result
    {
        private Result() { }

        public record Success : Result
        {
            private Success() { }
            public static readonly Success Instance = new();
        }
        public record Failure : Result
        {
            private Failure() { }
            public static readonly Failure Instance = new();
        }
    }

    public static int Run(string[] args)
    {
        BuildArguments? buildArgs = null;

        var argSyntax = ArgumentSyntax.Parse(args, syntax =>
        {
            string? commandName = null;

            var build = syntax.DefineCommand("build", ref commandName, "Install a new SDK");
            if (build.IsActive)
            {
                string? projectPath = null;
                string? artifactsPath = null;

                syntax.DefineOption("artifacts-path", ref artifactsPath, "Path to artifacts output");
                syntax.DefineParameter("project-path", ref projectPath!, "Path to project");

                buildArgs = new BuildArguments
                {
                    ArtifactsPath = artifactsPath,
                    ProjectPath = projectPath
                };
            }
        });

        if (buildArgs is null)
        {
            throw new InvalidOperationException("Expected command or exception");
        }

        var env = new DnEnv(Environment.CurrentDirectory, Console.Out);

        return Execute(env, buildArgs);
    }

    internal class CscWrap : Csc
    {
        protected override string PathToManagedTool => Path.Combine(AppContext.BaseDirectory, "bincore/csc.dll");
    }

    public static int Execute(DnEnv env, BuildArguments settings)
    {
        // TODO: Implement full MSBuild property and Item parsing
        var projectPath = settings.ProjectPath;
        if (projectPath is null)
        {
            projectPath = Directory.EnumerateFiles(env.WorkingDirectory, "*.csproj", SearchOption.TopDirectoryOnly).Single();
        }

        var parsedProject = MiniBuildParser.TryParse(projectPath);
        if (parsedProject is null)
        {
            return 1;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var objDir = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "Debug", "net8.0");
        Directory.CreateDirectory(objDir);

        var cscTask = BuildCscArgs(projectPath, objDir, projectName, settings.ArtifactsPath, env.Out);
        _ = cscTask.Execute();

        const string tfm = "net8.0";
        const string version = "8.0.0-preview.7.23375.6";
        var runtimeConfigPath = Path.Combine(objDir, $"{projectName}.runtimeconfig.json");
        GenerateRuntimeConfigurationFiles.Run(tfm, version, runtimeConfigPath);

        env.Out.WriteLine(cscTask.Utf8Output);
        return 0;
    }

    private static CscWrap BuildCscArgs(string projectPath, string objDir, string projectName, string? artifactsPath, TextWriter output)
    {
        // If there are no Compile items, assume we want to glob all *.cs files
        var projectFolder = Path.GetFullPath(Path.GetDirectoryName(projectPath)!);
        var csFiles = Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories);
        output.WriteLine(string.Join(Environment.NewLine, csFiles));

        // Add all ref assemblies from the Microsoft.NETCore.App.Ref package
        var refPackDir = Path.Combine(AppContext.BaseDirectory, "microsoft.netcore.app.ref", "ref", "net8.0");
        var refAssemblies = Directory.EnumerateFiles(refPackDir, "*.dll", SearchOption.TopDirectoryOnly);

        var cscTask = new CscWrap();
        if (artifactsPath is not null)
        {
            output.WriteLine(artifactsPath);
            cscTask.OutputAssembly = new TaskItem(Path.Combine(artifactsPath, "out.dll"));
        }
        else
        {
            cscTask.OutputAssembly = new TaskItem(Path.Combine(objDir, $"{projectName}.dll"));
        }
        cscTask.Sources = csFiles.Select(p => new TaskItem(p)).ToArray();
        cscTask.UseSharedCompilation = true;
        cscTask.BuildEngine = new MockEngine(output);
        cscTask.References = refAssemblies.Select(p => new TaskItem(p)).ToArray();

        return cscTask;
    }
}
