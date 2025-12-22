
using System.Text;
using Spectre.Console;

namespace Dn;

internal sealed class ConsoleWriter(IAnsiConsole console) : TextWriter
{
    public override Encoding Encoding { get; } = new UTF8Encoding(false);

    public override void Write(char value)
    {
        console.Write(value.ToString());
    }

    public override void Write(string? value)
    {
        console.Write(value ?? string.Empty);
    }
}