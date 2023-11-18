using Xunit;
using static MiniBuild.ProjectSubNode;

namespace MiniBuild.Tests;

public class ResolvedProjectTests
{
    [Fact]
    public void BasicProjectWithPackageRef()
    {
        var src = """
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <TargetFramework>net9.0</TargetFramework>
    </PropertyGroup>
</Project>
""";
        var context = new ProjectContext(ProjectParser.Parse(src));
        var resolved = context.Resolve();
        Assert.Equal("net9.0", resolved.ResolvedProperties["TargetFramework"]);
    }
}