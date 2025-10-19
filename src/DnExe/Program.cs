// See https://aka.ms/new-console-template for more information

using Dn;

// Check if we have a command
if (args.Length > 0)
{
    var command = args[0].ToLowerInvariant();

    int exitCode = command switch
    {
        "build" => BuildCommand.Run(args),
        "restore" => RestoreCommand.Run(args),
        _ => BuildCommand.Run(args) // Default to build for backwards compatibility
    };

    Console.WriteLine(exitCode == 0 ? "done" : "failed");
    return exitCode;
}
else
{
    // No arguments - show help
    Console.WriteLine("dn - A mini .NET SDK");
    Console.WriteLine();
    Console.WriteLine("Commands:");
    Console.WriteLine("  restore   Restore project dependencies");
    Console.WriteLine("  build     Build a .NET project");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dn restore [project-path]");
    Console.WriteLine("  dn build [project-path]");
    return 0;
}