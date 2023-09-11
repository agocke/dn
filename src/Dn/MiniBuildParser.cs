
using System.Collections.Immutable;

namespace Dn;

/// <summary>
/// An XML parser for MSBuild project files. Will never support Tasks or Targets.
/// </summary>
internal static class MiniBuildParser
{
    public static ParsedProject? TryParse(string projectPath)
    {
        var doc = new System.Xml.XmlDocument();
        doc.Load(projectPath);
        var root = doc.DocumentElement;
        if (root is null)
        {
            return null;
        }
        var parsedItems = ImmutableArray.CreateBuilder<ParsedItem>();
        var itemGroups = root.SelectNodes("//ItemGroup");
        if (itemGroups is not null)
        {
            foreach (var itemGroup in itemGroups)
            {
                if (itemGroup is not System.Xml.XmlElement itemGroupElement)
                {
                    continue;
                }
                var items = itemGroupElement.SelectNodes("*");
                if (items is not null)
                {
                    foreach (var item in items)
                    {
                        if (item is not System.Xml.XmlElement itemElement)
                        {
                            continue;
                        }
                        var name = itemElement.Name;
                        var include = itemElement.GetAttribute("Include");
                        var exclude = itemElement.GetAttribute("Exclude");
                        var condition = itemElement.GetAttribute("Condition");
                        parsedItems.Add(new ParsedItem(name, include)
                        {
                            Exclude = exclude,
                            Condition = condition,
                        });
                    }
                }
            }
        }
        return new ParsedProject(parsedItems.ToImmutable());
    }
}

internal sealed record ParsedProject(ImmutableArray<ParsedItem> Items);

internal sealed record ParsedItem(string Name, string Include)
{
    public string? Exclude { get; init; } = null;
    public string? Condition { get; init; } = null;
}