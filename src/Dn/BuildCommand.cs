using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;
using Spectre.Console.Cli;

namespace Dn;

public sealed class BuildCommand : Command<BuildArguments>
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
        var app = new CommandApp<BuildCommand>();
        return app.Run(args);
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] BuildArguments settings)
    {
        // TODO: Implement full MSBuild property and Item parsing
        var projectPath = settings.ProjectPath;
        var parsedProject = MiniBuildParser.TryParse(projectPath!);
        if (parsedProject is null)
        {
            return 1;
        }

        // If there are no Compile items, assume we want to glob all *.cs files
        var projectFolder = Path.GetFullPath(Path.GetDirectoryName(projectPath)!);
        var csFiles = Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories);
        Console.WriteLine(string.Join(Environment.NewLine, csFiles));

        // Add all ref assemblies from the Microsoft.NETCore.App.Ref package
        var refPackDir = Path.Combine(AppContext.BaseDirectory, "microsoft.netcore.app.ref", "ref", "net8.0");
        var refAssemblies = Directory.EnumerateFiles(refPackDir, "*.dll", SearchOption.TopDirectoryOnly);

        var cscTask = new Microsoft.CodeAnalysis.BuildTasks.Csc();
        cscTask.Sources = csFiles.Select(p => new TaskItem(p)).ToArray();
        cscTask.UseSharedCompilation = true;
        cscTask.BuildEngine = new MockEngine(Console.Out);
        if (settings.ArtifactsPath is {} artifactsPath)
        {
            cscTask.OutputAssembly = new TaskItem(Path.Combine(artifactsPath, "out.dll"));
        }
        cscTask.References = refAssemblies.Select(p => new TaskItem(p)).ToArray();
        var exec = cscTask.Execute();

        Console.WriteLine(cscTask.Utf8Output);
        return 0;
    }
}
