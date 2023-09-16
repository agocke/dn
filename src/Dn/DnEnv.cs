
public sealed class DnEnv
{
    public string WorkingDirectory { get; }
    public TextWriter Out { get; }

    public DnEnv(string workingDirectory, TextWriter output)
    {
        WorkingDirectory = workingDirectory;
        Out = output;
    }
}