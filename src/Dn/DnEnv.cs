using Spectre.Console;

public sealed record DnEnv(
    string WorkingDirectory,
    IAnsiConsole Out
);
