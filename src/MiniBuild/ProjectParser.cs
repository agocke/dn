
using System.Collections.Immutable;
using System.Diagnostics;
using System.Xml;
using StaticCs.Collections;

namespace MiniBuild;

public sealed record ParsedProject(
    string Sdk,
    EqArray<ProjectSubNode> Nodes);

public abstract record ProjectSubNode
{
    private ProjectSubNode() { }
    public sealed record PropertyGroup(EqArray<ParsedProperty> Properties) : ProjectSubNode;
    public sealed record ItemGroup(EqArray<ParsedItem> Items) : ProjectSubNode;
}

public sealed record ParsedProperty(string Name, string Value);

public sealed record ParsedItem(string Name, string Include)
{
    public string? Exclude { get; init; } = null;
    public string? Condition { get; init; } = null;
}

/// <summary>
/// An XML parser for MSBuild project files. Will never support Tasks or Targets.
/// </summary>
public static class ProjectParser
{
    public static ParsedProject Parse(string project)
    {
        var doc = new XmlDocument();
        doc.LoadXml(project);
        return Parse(doc);
    }

    private static ParsedProject Parse(XmlDocument doc)
    {
        var root = doc.DocumentElement;
        Debug.Assert(root is not null);
        var nodes = ImmutableArray.CreateBuilder<ProjectSubNode>();
        foreach (var child in root.ChildNodes)
        {
            if (child is not XmlNode node)
            {
                continue;
            }
            if (node.Name == "PropertyGroup")
            {
                var parsedProperties = ImmutableArray.CreateBuilder<ParsedProperty>();
                foreach (var property in node.ChildNodes)
                {
                    if (property is not XmlElement propertyElement)
                    {
                        continue;
                    }
                    var name = propertyElement.Name;
                    var value = propertyElement.InnerText;
                    parsedProperties.Add(new ParsedProperty(name, value));
                }
                nodes.Add(new ProjectSubNode.PropertyGroup(parsedProperties.ToImmutable().ToEq()));
            }
            else if (node.Name == "ItemGroup")
            {
                var parsedItems = ImmutableArray.CreateBuilder<ParsedItem>();
                foreach (var item in node.ChildNodes)
                {
                    if (item is not XmlElement itemElement)
                    {
                        continue;
                    }
                    var name = itemElement.Name;
                    var includeNode = itemElement.GetAttributeNode("Include");
                    var excludeNode = itemElement.GetAttributeNode("Exclude");
                    var conditionNode = itemElement.GetAttributeNode("Condition");
                    parsedItems.Add(new ParsedItem(name, includeNode?.Value ?? "")
                    {
                        Exclude = excludeNode?.Value,
                        Condition = conditionNode?.Value,
                    });
                }
                nodes.Add(new ProjectSubNode.ItemGroup(parsedItems.ToImmutable().ToEq()));
            }
        }
        var sdkName = root.GetAttribute("Sdk");
        return new ParsedProject(
            Sdk: sdkName,
            Nodes: nodes.ToImmutable().ToEq());
    }

    public static ParsedProject? TryParse(string projectPath)
    {
        var doc = new XmlDocument();
        doc.Load(projectPath);
        return Parse(doc);
    }
}
