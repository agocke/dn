using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MiniBuild;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace Dn;

/// <summary>
/// Converts MiniBuild's project representation to NuGet's PackageSpec format
/// </summary>
public static class PackageSpecAdapter
{
    /// <summary>
    /// Create a PackageSpec from a parsed project file
    /// </summary>
    public static PackageSpec CreatePackageSpec(
        string projectPath,
        ParsedProject parsedProject,
        ResolvedProject resolvedProject
    )
    {
        var projectName = Path.GetFileNameWithoutExtension(projectPath);
        var projectDir =
            Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException(
                $"Could not determine directory for {projectPath}"
            );

        // Get target framework from properties
        var targetFrameworkString = resolvedProject.ResolvedProperties.TryGetValue(
            "TargetFramework",
            out var tfm
        )
            ? tfm
            : "net8.0"; // Default fallback

        var targetFramework = NuGetFramework.Parse(targetFrameworkString);

        // Extract PackageReference items
        var packageReferences = GetPackageReferences(parsedProject, resolvedProject);

        // Create TargetFrameworkInformation
        var targetFrameworkInfo = new TargetFrameworkInformation
        {
            FrameworkName = targetFramework,
            TargetAlias = targetFrameworkString,
            Dependencies = packageReferences.ToImmutableArray(),
        };

        // Create PackageSpec
        var packageSpec = new PackageSpec(
            new List<TargetFrameworkInformation> { targetFrameworkInfo }
        )
        {
            Name = projectName,
            FilePath = projectPath,
            RestoreMetadata = new ProjectRestoreMetadata
            {
                ProjectUniqueName = projectPath,
                ProjectName = projectName,
                ProjectPath = projectPath,
                ProjectStyle = ProjectStyle.PackageReference,
                OutputPath = Path.Combine(projectDir, "obj"),
                OriginalTargetFrameworks = new List<string> { targetFrameworkString },
                TargetFrameworks = new List<ProjectRestoreMetadataFrameworkInfo>
                {
                    new ProjectRestoreMetadataFrameworkInfo(targetFramework)
                    {
                        TargetAlias = targetFrameworkString,
                    },
                },
                // Use global packages folder
                PackagesPath = GetGlobalPackagesFolder(),
                // Default to nuget.org
                Sources = new List<PackageSource>
                {
                    new PackageSource("https://api.nuget.org/v3/index.json", "nuget.org"),
                },
                ConfigFilePaths = GetNuGetConfigPaths(),
            },
        };

        return packageSpec;
    }

    private static List<LibraryDependency> GetPackageReferences(
        ParsedProject parsedProject,
        ResolvedProject resolvedProject
    )
    {
        var dependencies = new List<LibraryDependency>();

        // Find PackageReference items in the parsed project to get Version metadata
        foreach (var node in parsedProject.Nodes)
        {
            if (node is ProjectSubNode.ItemGroup itemGroup)
            {
                foreach (var item in itemGroup.Items)
                {
                    if (item.Name == "PackageReference")
                    {
                        var packageId = item.Include;

                        // Try to get version from attribute or child element
                        string? versionString = item.Version;

                        // If not in attribute, check metadata (child elements)
                        if (string.IsNullOrEmpty(versionString) && item.Metadata != null)
                        {
                            item.Metadata.TryGetValue("Version", out versionString!);
                        }

                        if (string.IsNullOrEmpty(versionString))
                        {
                            // Skip packages without version - would need central package management
                            continue;
                        }

                        if (VersionRange.TryParse(versionString, out var versionRange))
                        {
                            dependencies.Add(
                                new LibraryDependency
                                {
                                    LibraryRange = new LibraryRange(
                                        packageId,
                                        versionRange,
                                        LibraryDependencyTarget.Package
                                    ),
                                }
                            );
                        }
                    }
                }
            }
        }

        return dependencies;
    }

    private static string GetGlobalPackagesFolder()
    {
        // Default global packages folder location
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".nuget", "packages");
    }

    private static List<string> GetNuGetConfigPaths()
    {
        var paths = new List<string>();

        // Look for NuGet.Config in standard locations
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var userConfig = Path.Combine(userProfile, ".nuget", "NuGet", "NuGet.Config");

        if (File.Exists(userConfig))
        {
            paths.Add(userConfig);
        }

        return paths;
    }
}
