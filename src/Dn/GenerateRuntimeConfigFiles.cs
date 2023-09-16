
using System.Collections.Immutable;
using Serde;
using Serde.Json;

namespace Dn;

/// <summary>
/// Generates the $(project).runtimeconfig.json and optionally $(project).runtimeconfig.dev.json files
/// for a project.
/// </summary>
internal sealed partial class GenerateRuntimeConfigurationFiles
{
    [GenerateSerialize]
    private partial record RuntimeConfigJson
    {
        public required RuntimeOptions RuntimeOptions { get; init; }
        public required ImmutableDictionary<string, bool> ConfigProperties { get; init; }
    }

    [GenerateSerialize]
    private partial record RuntimeOptions
    {
        public required string Tfm { get; init; }
        public required Framework Framework { get; init; }
        public string? RollForward { get; init; } = null;
    }

    [GenerateSerialize]
    private partial record Framework
    {
        public string? Name { get; init; }
        public string? Version { get; init; }
    }

    public static void Run(string tfm, string version, string outPath)
    {
        var runtimeConfigJson = new RuntimeConfigJson
        {
            RuntimeOptions = new RuntimeOptions
            {
                Tfm = tfm,
                Framework = new Framework
                {
                    Name = "Microsoft.NETCore.App",
                    Version = version
                }
            },
            ConfigProperties = ImmutableDictionary<string, bool>.Empty
                .Add("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true)
        };
        File.WriteAllText(outPath, JsonSerializer.Serialize(runtimeConfigJson));
    }
}