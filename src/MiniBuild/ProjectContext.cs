using System.Collections.Frozen;
using StaticCs.Collections;

namespace MiniBuild;

public sealed record ResolvedProject(
    FrozenDictionary<string, string> ResolvedProperties
);

public sealed class ProjectContext
{
    public ParsedProject RootProject { get; init; }
    public EqArray<ParsedProject> ParsedProjects { get; init; }

    public ProjectContext(ParsedProject rootProject, params ParsedProject[] projects)
    {
        RootProject = rootProject;
        ParsedProjects = projects.ToEq();
    }

    public ResolvedProject Resolve()
    {
        var properties = new Dictionary<string, string>();
        foreach (var node in RootProject.Nodes)
        {
            switch (node)
            {
                case ProjectSubNode.PropertyGroup propertyGroup:
                    foreach (var property in propertyGroup.Properties)
                    {
                        properties[property.Name] = property.Value;
                    }
                    break;
            }
        }
        return new ResolvedProject(properties.ToFrozenDictionary());
    }
}