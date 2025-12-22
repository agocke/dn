using Serde;
using Serde.CmdLine;
using StaticCs;

namespace Dn;

[GenerateDeserialize]
public sealed partial record DnArgs
{
    [CommandGroup("command")]
    public required SubCommand SubCommand { get; init; }
}

[GenerateDeserialize]
[Closed]
public abstract partial record SubCommand
{
    private SubCommand() { }

    [Command("build", Description = "Build a .NET project.")]
    public sealed partial record BuildArgs : SubCommand
    {
        [CommandParameter(0, "project-path")]
        public string? ProjectPath { get; init; }

        [CommandOption(
            "--artifacts-path",
            Description = "The artifacts path. All output from the project, including build, publish, and pack output, will go in subfolders under the specified path."
        )]
        public string? ArtifactsPath { get; init; }
    }

    [Command("restore", Description = "Restore project dependencies.")]
    public sealed partial record RestoreArgs : SubCommand
    {
        [CommandParameter(0, "project-path")]
        public string? ProjectPath { get; init; }
    }
}
