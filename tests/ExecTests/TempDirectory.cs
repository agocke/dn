using IOPath = System.IO.Path;

namespace Dn.Test;

public readonly record struct TempDirectory(string Path) : IDisposable
{
    public static TempDirectory TestRoot = CreateSubDirectory(ExecTests.ResolveRelativePath("../../artifacts/test/"));

    public static TempDirectory CreateSubDirectory(string basePath)
    {
        string dir = IOPath.Combine(basePath, IOPath.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return new TempDirectory(dir);
    }

    public TempDirectory CreateSubDirectory() => CreateSubDirectory(Path);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Copy a file from another path into this directory.
    /// </summary>
    public string CopyFile(string path)
    {
        var newPath = IOPath.Combine(Path, IOPath.GetFileName(path));
        File.Copy(path, newPath);
        return newPath;
    }
}