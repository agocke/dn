using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.BuildTasks.UnitTests;

namespace Dn;

public static class BuildCommand
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

    public static Result Run(string[] args)
    {
        // TODO: Implement full MSBuild property and Item parsing
        var projectPath = args[0];
        var parsedProject = MiniBuildParser.TryParse(projectPath);
        if (parsedProject is null)
        {
            return Result.Failure.Instance;
        }

        // If there are no Compile items, assume we want to glob all *.cs files
        var projectFolder = Path.GetFullPath(Path.GetDirectoryName(projectPath)!);
        var csFiles = Directory.EnumerateFiles(projectFolder, "*.cs", SearchOption.AllDirectories);
        Console.WriteLine(string.Join(Environment.NewLine, csFiles));

        var cscTask = new Microsoft.CodeAnalysis.BuildTasks.Csc();
        cscTask.Sources = csFiles.Select(p => new TaskItem(p)).ToArray();
        cscTask.UseSharedCompilation = true;
        cscTask.BuildEngine = new MockEngine(Console.Out);
        var exec = cscTask.Execute();

        Console.WriteLine(cscTask.Utf8Output);
        return Result.Success.Instance;
    }
}
