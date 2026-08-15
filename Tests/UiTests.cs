using Heimdall.Infrastructure.Presentation;
using Xunit;

namespace Heimdall.Tests;

public class UiTests
{
    [Fact]
    public void EyeSpinner_NonTtyStream_DoesNotThrow()
    {
        using var writer = new StringWriter();
        using var spinner = new EyeSpinner(intervalSeconds: 0.01, writer: writer);
        spinner.Status("testando");

        Assert.True(true);
    }

    [Fact]
    public void EyeSpinner_Log_WritesMessageToStream()
    {
        using var writer = new StringWriter();
        using (var spinner = new EyeSpinner(intervalSeconds: 0.01, writer: writer))
        {
            spinner.Log("mensagem de teste");
        }

        var output = writer.ToString();
        Assert.Contains("mensagem de teste", output);
    }

    [Fact]
    public void PrintBanner_WritesTitleAndAsciiArt()
    {
        using var writer = new StringWriter();
        ConsolePresenter.PrintBanner(writer);

        var output = writer.ToString();
        Assert.Contains("O Guardião Inabalável.", output);
    }
}
