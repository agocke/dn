# dn

A mini-sdk for .NET. This is called a mini SDK because it should include support for
the official .NET SDK top-level verbs (build, restore, publish, etc), but will not
contain an MSBild execution engine, meaning no support for Tasks or Targets.

The result should be smaller, faster, and simpler than the official .NET SDK. Many
projects may not need MSBuild functionality at all, and `dn` is intended to be a
drop-in replacement for those projects. For more complex projects that are currently
using Tasks and Targets, the recommendation is to wrap `dn` in one of the many
pre-existing build systems available in the ecosystem, like `Make`, `CMake`, `Bazel`,
`Buck`, or `Meson`.

## Contributing

To build the repo you must have `make` installed. To build, run `make` in the top-level
directory.