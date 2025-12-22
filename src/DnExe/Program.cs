// See https://aka.ms/new-console-template for more information

using Dn;
using Serde.CmdLine;
using Spectre.Console;

var console = AnsiConsole.Console;

if (!CmdLine.TryParse<DnArgs>(args, console, out var dnArgs))
{
    return 1;
}

var env = new DnEnv(Environment.CurrentDirectory, console);

return dnArgs.SubCommand switch
{
    SubCommand.BuildArgs buildArgs => BuildCommand.Execute(env, buildArgs),
    SubCommand.RestoreArgs restoreArgs => await RestoreCommand.ExecuteAsync(env, restoreArgs),
};