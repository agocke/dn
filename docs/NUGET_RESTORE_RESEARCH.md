# NuGet Restore Research - Implementation Plan

## Overview

After examining the NuGet.Client repository, I've identified the key entry points and architecture for implementing restore in `dn`.

## Key Findings

### 1. Main Entry Point: RestoreRunner

**Location**: `NuGet.Commands.RestoreRunner`

The `RestoreRunner.RunAsync()` method is the primary entry point that:
- Takes a `RestoreArgs` context
- Creates restore requests from inputs
- Executes restores in parallel (with throttling)
- Commits results and generates assets

**Signature**:
```csharp
public static async Task<IReadOnlyList<RestoreSummary>> RunAsync(
    RestoreArgs restoreContext,
    CancellationToken token)
```

### 2. Core Data Structures

#### DependencyGraphSpec
- **Purpose**: Container for all projects and their dependencies
- **Key Properties**:
  - `IReadOnlyList<string> Restore` - Projects to restore
  - `IReadOnlyList<PackageSpec> Projects` - All project specs
- **Location**: `NuGet.ProjectModel.DependencyGraphSpec`

#### PackageSpec
- **Purpose**: Specification for a single project
- **Key Properties**:
  - `string Name`, `string FilePath`
  - `IList<TargetFrameworkInformation> TargetFrameworks`
  - `ProjectRestoreMetadata RestoreMetadata` - MSBuild-specific metadata
- **Location**: `NuGet.ProjectModel.PackageSpec`

#### RestoreRequest
- **Purpose**: All parameters needed for a single project restore
- **Includes**: Package sources, cache locations, logging, project spec
- **Location**: `NuGet.Commands.RestoreRequest`

### 3. Restore Workflow

```
Input: .csproj file(s)
  ↓
Parse to PackageSpec (with PackageReference items)
  ↓
Create DependencyGraphSpec
  ↓
RestoreRunner.RunAsync()
  ↓
  ├─ For each project:
  │   ├─ Create RestoreRequest
  │   ├─ Execute RestoreCommand
  │   │   ├─ Build dependency graph (transitive resolution)
  │   │   ├─ Download packages to global packages folder
  │   │   ├─ Validate compatibility
  │   │   └─ Generate lock file (assets)
  │   └─ Commit results
  ↓
Output: project.assets.json + restored packages
```

### 4. Key NuGet Libraries Needed

Based on the code examination:

1. **NuGet.Commands** - RestoreRunner, RestoreCommand
2. **NuGet.ProjectModel** - DependencyGraphSpec, PackageSpec, LockFile
3. **NuGet.Protocol** - Package download and repository access
4. **NuGet.DependencyResolver** - Dependency graph resolution
5. **NuGet.Frameworks** - Framework parsing and compatibility
6. **NuGet.Versioning** - Version range parsing
7. **NuGet.Configuration** - Reading NuGet.Config for sources

### 5. Critical Classes to Study

#### RestoreCommand (`NuGet.Commands.RestoreCommand`)
- ~2178 lines - the core restore logic
- Handles dependency resolution, graph walking, package installation
- Generates the lock file (project.assets.json)

#### ProjectRestoreCommand (`NuGet.Commands.ProjectRestoreCommand`)
- Walks dependencies for specific target frameworks
- Handles runtime-specific restores
- Downloads and installs packages

#### LockFileBuilder (`NuGet.Commands.LockFileBuilder`)
- Generates the project.assets.json file
- Critical for build integration

## Implementation Strategy for `dn`

### Option 1: Use NuGet Libraries Directly (RECOMMENDED)

**Approach**: Add NuGet.Commands and dependencies, call RestoreRunner.RunAsync()

**Pros**:
- Leverage battle-tested code
- Automatic compatibility with official NuGet
- Get all features (lock files, auditing, etc.)
- Easier to maintain

**Cons**:
- Larger dependency footprint
- May include features we don't need

**Implementation Steps**:
1. Add PackageReferences to `Dn.csproj`:
   ```xml
   <PackageReference Include="NuGet.Commands" Version="..." />
   <PackageReference Include="NuGet.ProjectModel" Version="..." />
   ```

2. Create `PackageSpec` from our parsed project:
   ```csharp
   // Convert MiniBuild's ResolvedProject to NuGet's PackageSpec
   var packageSpec = new PackageSpec {
       Name = projectName,
       FilePath = projectPath,
       TargetFrameworks = { ... },
       RestoreMetadata = new ProjectRestoreMetadata { ... }
   };
   ```

3. Build DependencyGraphSpec:
   ```csharp
   var dgSpec = new DependencyGraphSpec();
   dgSpec.AddProject(packageSpec);
   dgSpec.AddRestore(projectPath);
   ```

4. Call RestoreRunner:
   ```csharp
   var restoreArgs = new RestoreArgs {
       CacheContext = new SourceCacheContext(),
       Log = logger,
       // ... configure sources, global packages folder, etc.
   };

   var summaries = await RestoreRunner.RunAsync(dgSpec, restoreArgs, token);
   ```

5. Read generated project.assets.json in BuildCommand

### Option 2: Custom Minimal Implementation

**Approach**: Implement only what we need using low-level NuGet.Protocol

**Pros**:
- Smaller footprint
- Full control over behavior

**Cons**:
- Much more work
- Risk of incompatibilities
- Need to maintain compatibility as NuGet evolves

**Not recommended** - the official libraries are well-designed for reuse.

## Simplified Plan

### Phase 1: Basic Integration (MVP)
1. Add NuGet library dependencies to `Dn.csproj`
2. Extend MiniBuild to parse PackageReference items (with Version)
3. Create adapter to convert `ResolvedProject` → `PackageSpec`
4. Implement `RestoreCommand.Execute()` that calls `RestoreRunner.RunAsync()`
5. Test with simple project (single PackageReference)

### Phase 2: Build Integration
1. Update `BuildCommand` to read `project.assets.json`
2. Add package assembly references to Csc compilation
3. Test full restore → build workflow

### Phase 3: Polish
1. Add proper error handling and logging
2. Support NuGet.Config file reading
3. Handle edge cases (missing packages, version conflicts)
4. Add comprehensive tests

## Next Steps

1. ✅ Research complete - we now understand the architecture
2. Start with Option 1 (use NuGet libraries directly)
3. Begin Phase 1 implementation

## References

- RestoreRunner: `nuget.client/src/NuGet.Core/NuGet.Commands/RestoreCommand/RestoreRunner.cs`
- RestoreCommand: `nuget.client/src/NuGet.Core/NuGet.Commands/RestoreCommand/RestoreCommand.cs`
- PackageSpec: `nuget.client/src/NuGet.Core/NuGet.ProjectModel/PackageSpec.cs`
- DependencyGraphSpec: `nuget.client/src/NuGet.Core/NuGet.ProjectModel/DependencyGraphSpec.cs`
