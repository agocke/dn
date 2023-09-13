
build: toolset_restore
	dotnet build src/DnExe/DnExe.csproj

toolset_restore:
	dotnet restore src/BuildPackageRestore/BuildPackageRestore.csproj