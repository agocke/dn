using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Text;
using Internal.CommandLine;
using MiniBuild;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Microsoft.CodeAnalysis.BuildTasks;
using StaticCs.Collections;

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

        var parsedProject = ProjectParser.TryParse(projectPath);
        if (parsedProject is null)
        {
            return 1;
        }
        var ctx = new ProjectContext(parsedProject);
        var resolvedProject = ctx.Resolve();

        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var objDir = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "Debug", "net8.0");
        Directory.CreateDirectory(objDir);

        var csFiles = GetCompileItemsOrDefault(resolvedProject, Path.GetDirectoryName(projectPath)!);
        var cscTask = BuildCscArgs(csFiles, projectPath, objDir, projectName, settings.ArtifactsPath, env.Out);
        _ = cscTask.Execute();

        var runtimeConfigPath = Path.Combine(objDir, $"{projectName}.runtimeconfig.json");
        GenerateRuntimeConfigurationFiles.Run(VersionInfo.Tfm, VersionInfo.SdkVersion, runtimeConfigPath);

        env.Out.WriteLine(cscTask.Utf8Output);
        return 0;
    }

    private static EqArray<string> GetCompileItemsOrDefault(ResolvedProject resolved, string projectFolder)
    {
        if (resolved.Items.TryGetValue("Compile", out var compileItems) && compileItems.Any())
        {
            return compileItems.Select(i => i.Value).ToEq();
        }
        // If there are no Compile items, assume we want to glob all *.cs files
        var csFiles = Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories);
        return csFiles.ToEq();
    }

    private static CscWrap BuildCscArgs(
        EqArray<string> csFiles,
        string projectPath,
        string objDir,
        string projectName,
        string? artifactsPath,
        TextWriter output)
    {
        // If there are no Compile items, assume we want to glob all *.cs files
        var projectFolder = Path.GetFullPath(Path.GetDirectoryName(projectPath)!);
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
