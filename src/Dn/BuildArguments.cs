
using System.ComponentModel;
using Spectre.Console.Cli;

namespace Dn;

public sealed class BuildArguments : CommandSettings
{
    [Description("The path to the project file to build.")]
    [CommandArgument(0, "[PROJECT]")]
    public string? ProjectPath { get; init; }

    [CommandOption("--artifacts-path")]
    public string? ArtifactsPath { get; init; }
}