
ROSLYN_VERSION = 4.8.0-1.final
TASK_DIR = artifacts/restore/microsoft.net.compilers.toolset/$(ROSLYN_VERSION)/tasks/
TASK_NAME = Microsoft.Build.Tasks.CodeAnalysis.dll

build: $(TASK_DIR)/$(TASK_NAME)
	dotnet build src/DnExe/DnExe.csproj

$(TASK_DIR)/$(TASK_NAME): $(TASK_DIR)/netcore/$(TASK_NAME)
	dotnet run --project tools/FixupRoslyn/FixupRoslyn.csproj -- $(TASK_DIR)/netcore/Microsoft.Build.Tasks.CodeAnalysis.dll

$(TASK_DIR)/netcore/$(TASK_NAME):
	dotnet restore src/BuildPackageRestore/BuildPackageRestore.csproj

test: build
	dotnet test dn.sln