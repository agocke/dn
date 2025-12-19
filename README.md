# dn

A re-implementation of the dotnet SDK optimized for performance.

The goal is compatibility with the .NET SDK, except for two aspects:

- MSBuild Tasks
- MSBuild Targets

These components require a fully-compatible MSBuild engine and are not compatible with the performance goals.

## Contributing

To build the repo you must have `make` installed. To build, run `make` in the top-level
directory.
