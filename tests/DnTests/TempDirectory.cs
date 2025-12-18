using IOPath = System.IO.Path;

namespace Dn.Test;

public readonly record struct TempDirectory(string Path) : IDisposable
{
    private static readonly string TestRootPath = IOPath.GetFullPath(ExecTests.ResolveRelativePath("../../artifacts/test/"));

    public static TempDirectory CreateSubDirectory()
    {
        Directory.CreateDirectory(TestRootPath);
        string dir = IOPath.Combine(TestRootPath, IOPath.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return new TempDirectory(dir);
    }

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
