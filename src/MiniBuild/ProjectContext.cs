using System.Collections.Frozen;
using StaticCs.Collections;

namespace MiniBuild;

public sealed record ResolvedProject(
    FrozenDictionary<string, string> ResolvedProperties,
    FrozenDictionary<string, EqArray<ResolvedItem>> Items
);

public readonly record struct ResolvedItem(string Value);

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
        var items = new Dictionary<string, List<ResolvedItem>>();
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
                case ProjectSubNode.ItemGroup itemGroup:
                    foreach (var item in itemGroup.Items)
                    {
                        if (!items.TryGetValue(item.Name, out var list))
                        {
                            list = new();
                        }
                        list.Add(new(item.Include));
                    }
                    break;
            }
        }
        return new ResolvedProject(
            properties.ToFrozenDictionary(),
            items.ToFrozenDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToEq()));
    }
}