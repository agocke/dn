
using System.Text;
using Xunit.Abstractions;

namespace Dn.Test;

public sealed class TestWriter : TextWriter
{
    private readonly ITestOutputHelper _outputHelper;

    public TestWriter(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        _outputHelper.WriteLine(value);
    }
}