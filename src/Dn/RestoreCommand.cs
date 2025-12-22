using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiniBuild;
using NuGet.Commands;
using NuGet.Common;
using NuGet.ProjectModel;
using Serde.CmdLine;
using Spectre.Console;

namespace Dn;

public sealed class RestoreCommand
{
    public static async Task<int> ExecuteAsync(DnEnv env, SubCommand.RestoreArgs settings)
    {
        // Find project file
        var projectPath = settings.ProjectPath;
        if (projectPath is null)
        {
            var projects = Directory
                .EnumerateFiles(env.WorkingDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
                .ToList();
            if (projects.Count == 0)
            {
                env.Out.WriteLine("Error: No .csproj file found in current directory");
                return 1;
            }
            if (projects.Count > 1)
            {
                env.Out.WriteLine(
                    "Error: Multiple .csproj files found. Please specify which one to restore."
                );
                return 1;
            }
            projectPath = projects[0];
        }

        if (!File.Exists(projectPath))
        {
            env.Out.WriteLine($"Error: Project file not found: {projectPath}");
            return 1;
        }

        env.Out.WriteLine($"Restoring packages for {Path.GetFileName(projectPath)}...");

        // Parse project
        var parsedProject = ProjectParser.TryParse(projectPath);
        if (parsedProject is null)
        {
            env.Out.WriteLine("Error: Failed to parse project file");
            return 1;
        }

        var ctx = new ProjectContext(parsedProject);
        var resolvedProject = ctx.Resolve();

        // Convert to PackageSpec
        var packageSpec = PackageSpecAdapter.CreatePackageSpec(
            projectPath,
            parsedProject,
            resolvedProject
        );

        // Create DependencyGraphSpec
        var dgSpec = new DependencyGraphSpec();
        dgSpec.AddProject(packageSpec);
        dgSpec.AddRestore(projectPath);

        // Create logger
        var logger = new ConsoleLogger(env.Out);

        // Create providers cache and request provider
        var providersCache = new RestoreCommandProvidersCache();
        var providers = new List<IPreLoadedRestoreRequestProvider>
        {
            new DependencyGraphSpecRequestProvider(providersCache, dgSpec),
        };

        // Setup restore context
        var restoreContext = new RestoreArgs
        {
            CacheContext = new NuGet.Protocol.Core.Types.SourceCacheContext
            {
                NoCache = false,
            },
            DisableParallel = false,
            Log = logger,
            HideWarningsAndErrors = false,
            PreLoadedRequestProviders = providers,
        };

        // Run restore
        try
        {
            var restoreSummaries = await RestoreRunner.RunAsync(
                restoreContext,
                CancellationToken.None
            );

            // Check results
            var allSucceeded = restoreSummaries.All(s => s.Success);

            if (allSucceeded)
            {
                env.Out.WriteLine($"Restore completed successfully.");
                return 0;
            }
            else
            {
                env.Out.WriteLine("Restore failed.");
                foreach (var summary in restoreSummaries.Where(s => !s.Success))
                {
                    env.Out.WriteLine($"  Project: {summary.InputPath}");
                    foreach (var error in summary.Errors)
                    {
                        env.Out.WriteLine($"    Error: {error.Message}");
                    }
                }
                return 1;
            }
        }
        catch (Exception ex)
        {
            env.Out.WriteLine($"Error during restore: {ex.Message}");
            env.Out.WriteLine(ex.StackTrace ?? "");
            return 1;
        }
    }

    /// <summary>
    /// Simple console logger for NuGet operations
    /// </summary>
    private class ConsoleLogger(IAnsiConsole _console) : ILogger
    {
        public void Log(LogLevel level, string data)
        {
            _console.WriteLine($"[{level}] {data}");
        }

        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        public void LogDebug(string data) => Log(LogLevel.Debug, data);

        public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

        public void LogInformation(string data) => Log(LogLevel.Information, data);

        public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

        public void LogWarning(string data) => Log(LogLevel.Warning, data);

        public void LogError(string data) => Log(LogLevel.Error, data);

        public void LogInformationSummary(string data) => Log(LogLevel.Information, data);
    }
}
