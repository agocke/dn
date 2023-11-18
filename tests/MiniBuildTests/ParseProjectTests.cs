using Xunit;
using static MiniBuild.ProjectSubNode;

namespace MiniBuild.Tests;

public class ParseProjectTests
{
    [Fact]
    public void BasicProjectWithPackageRef()
    {
        var src = """
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="xunit" Version="2.4.2" />
    </ItemGroup>
</Project>
""";
        var parsed = ProjectParser.Parse(src);
        var expected = new ParsedProject(
            Sdk: "Microsoft.NET.Sdk",
            Nodes: [
                new PropertyGroup([ new("TargetFramework", "net8.0")]),
                new ItemGroup([
                    new ParsedItem("PackageReference", "xunit")
                ])
            ]
        );
        var pNodes = parsed.Nodes;
        var eNodes = expected.Nodes;
        Assert.Equal(expected, parsed);
    }
}