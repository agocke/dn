# Architecture

## Overview

`dn` is a lightweight re-implementation of the `dotnet` CLI that provides core SDK functionality without the complexity of MSBuild. It aims to be smaller, faster, and simpler than the official .NET SDK while supporting common development workflows for projects that don't require MSBuild's Task and Target system.

## Design Philosophy

The key design principle of `dn` is to **avoid MSBuild entirely**. Instead of using MSBuild's execution engine, `dn` implements:

- Custom XML parsing for `.csproj` files
- Direct invocation of the C# compiler (Roslyn)
- Simplified property and item resolution
- Runtime configuration generation

For projects requiring advanced MSBuild features (Tasks, Targets, custom build logic), the recommendation is to wrap `dn` with external build systems like Make, CMake, Bazel, Buck, or Meson.

## High-Level Architecture

```
┌─────────────┐
│   DnExe     │  Entry point (Program.cs)
└──────┬──────┘
       │
       ▼
┌─────────────┐
│     Dn      │  Command processing & build orchestration
│             │  - BuildCommand
│             │  - DnEnv
│             │  - GenerateRuntimeConfigFiles
└──────┬──────┘
       │
       ▼
┌─────────────┐
│  MiniBuild  │  Project file parsing & resolution
│             │  - ProjectParser (XML parsing)
│             │  - ProjectContext (resolution)
└─────────────┘
```

## Core Components

### 1. DnExe

**Location:** `src/DnExe/`

The entry point for the `dn` executable. Simply delegates to the `BuildCommand` in the `Dn` library.

**Key Files:**
- `Program.cs` - Main entry point

### 2. Dn (Core Library)

**Location:** `src/Dn/`

The main library containing build orchestration logic and command-line interface.

**Key Components:**

- **BuildCommand** (`BuildCommand.cs`)
  - Parses command-line arguments
  - Orchestrates the build process
  - Invokes the C# compiler directly via Roslyn's `Csc` task
  - Generates output assemblies

- **GenerateRuntimeConfigFiles** (`GenerateRuntimeConfigFiles.cs`)
  - Generates `.runtimeconfig.json` files required for .NET execution
  - Configures target framework and runtime version
  - Sets runtime configuration properties

- **DnEnv** (`DnEnv.cs`)
  - Environment configuration for the build process
  - Working directory and output stream management

- **Command-Line Processing** (`CommandLine/`)
  - Argument parsing and help text generation
  - Based on a custom argument parsing library

### 3. MiniBuild

**Location:** `src/MiniBuild/`

A lightweight MSBuild project file parser that extracts properties and items without executing Tasks or Targets.

**Key Components:**

- **ProjectParser** (`ProjectParser.cs`)
  - Custom XML parser for `.csproj` files
  - Extracts PropertyGroups and ItemGroups
  - Does **not** support Tasks or Targets (by design)
  - Returns a `ParsedProject` containing raw property and item definitions

- **ProjectContext** (`ProjectContext.cs`)
  - Resolves properties and items from parsed projects
  - Handles project imports (SDK imports)
  - Produces a `ResolvedProject` with evaluated properties and items

**Data Model:**
```
ParsedProject
├── Sdk (string)
└── Nodes (array of ProjectSubNode)
    ├── PropertyGroup
    │   └── Properties (array of ParsedProperty)
    └── ItemGroup
        └── Items (array of ParsedItem)

ResolvedProject
├── ResolvedProperties (dictionary)
└── Items (dictionary of ResolvedItem arrays)
```

## Build Workflow

The build process follows these steps:

1. **Argument Parsing**
   - Parse command-line arguments (project path, artifacts path, etc.)
   - Set up build environment (`DnEnv`)

2. **Project Discovery**
   - Locate `.csproj` file (explicit path or search current directory)

3. **Project Parsing** (MiniBuild)
   - Parse XML structure of `.csproj`
   - Extract properties and items
   - Create `ParsedProject`

4. **Project Resolution** (MiniBuild)
   - Resolve properties to their final values
   - Resolve items (e.g., Compile items)
   - Create `ResolvedProject`

5. **Compilation**
   - Determine source files (from Compile items or glob `*.cs`)
   - Locate reference assemblies from `microsoft.netcore.app.ref`
   - Build `Csc` task with appropriate arguments
   - Execute Roslyn compiler

6. **Runtime Configuration**
   - Generate `.runtimeconfig.json` file
   - Configure target framework and runtime version

7. **Output**
   - Write compiled assembly to output directory (obj/Debug or artifacts path)
   - Write runtime configuration

## No Restore, No Publish (Yet)

Currently, `dn` implements the **build** workflow. The following features are not yet implemented:

- **Restore**: NuGet package restoration
- **Publish**: Publishing/packaging applications

These may be added in future versions.

## Execution Model

Unlike MSBuild which uses a Task/Target execution graph, `dn` uses a **direct execution model**:

1. Parse project file → Extract data
2. Resolve properties → Simple dictionary lookup
3. Invoke compiler → Direct Roslyn API call

This is much simpler and faster than MSBuild's graph-based execution but cannot handle complex build customization through Tasks and Targets.

## Compiler Integration

`dn` integrates with the Roslyn C# compiler through:

- **CscWrap**: Internal wrapper around `Microsoft.CodeAnalysis.BuildTasks.Csc`
- Direct path to `csc.dll` from bundled binaries
- Reference assemblies from `microsoft.netcore.app.ref` package
- Shared compilation for performance

## Limitations

By design, `dn` does **not** support:

- MSBuild Tasks
- MSBuild Targets
- Custom build logic in `.csproj` files
- Complex condition evaluation
- Full NuGet restore (yet)
- Multi-targeting (yet)

For projects requiring these features, use the official .NET SDK or wrap `dn` in a more powerful build system.

## Testing

The project includes several test suites:

- **EquivalenceTests**: Ensures `dn` produces equivalent outputs to official `dotnet`
- **ExecTests**: Tests execution of built projects
- **MiniBuildTests**: Unit tests for project parsing and resolution
- **test_baselines**: Baseline projects for equivalence testing

## Future Directions

Potential areas for expansion:

1. **Package Restore**: Implement NuGet package resolution and download
2. **Publish Support**: Create self-contained and framework-dependent deployments
3. **Multi-targeting**: Support building for multiple target frameworks
4. **SDK Resolution**: Improve SDK import and resolution logic
5. **Performance**: Further optimize build times through caching and parallelization
