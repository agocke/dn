using System.Collections.Immutable;
using Serde;
using Serde.Json;

namespace Dn;

/// <summary>
/// Generates the $(project).runtimeconfig.json and optionally $(project).runtimeconfig.dev.json files
/// for a project.
/// </summary>
[GenerateSerialize]
internal partial record RuntimeConfigJson
{
    public required RuntimeOptions RuntimeOptions { get; init; }

    [SerdeMemberOptions(
        SerializeProxy = typeof(ImmutableDictionaryProxy.Ser<
            string,
            bool,
            StringProxy,
            BoolProxy
        >)
    )]
    public required ImmutableDictionary<string, bool> ConfigProperties { get; init; }

    public static RuntimeConfigJson Create(
        string tfm,
        string frameworkVersion
    ) =>
        new RuntimeConfigJson
        {
            RuntimeOptions = new RuntimeOptions
            {
                Tfm = tfm,
                Framework = new Framework
                {
                    Name = "Microsoft.NETCore.App",
                    Version = frameworkVersion,
                },
            },
            ConfigProperties = ImmutableDictionary<string, bool>.Empty.Add(
                "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization",
                true
            ),
        };

    public void WriteToFile(string outPath)
    {
        File.WriteAllText(outPath, JsonSerializer.Serialize(this));
    }
}

[GenerateSerialize]
internal partial record RuntimeOptions
{
    public required string Tfm { get; init; }
    public required Framework Framework { get; init; }
    public string? RollForward { get; init; } = null;
}

[GenerateSerialize]
internal partial record Framework
{
    public string? Name { get; init; }
    public string? Version { get; init; }
}