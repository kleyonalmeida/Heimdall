namespace Heimdall.Infrastructure.Presentation;

public static class ConsolePresenter
{
    private const string HeimdallHArt = @"
  ██╗  ██╗██████╗ ██╗███╗   ██╗██████╗  █████╗ ██╗     ██╗
  ██║  ██║██╔══██╗██║████╗ ████║██╔══██╗██╔══██╗██║     ██║
  ███████║███████║██║██╔████╔██║██║  ██║███████║██║     ██║
  ██╔══██║██╔══██║██║██║╚██╔╝██║██║  ██║██╔══██║██║     ██║
  ██║  ██║██║  ██║██║██║ ╚═╝ ██║██████╔╝██║  ██║███████╗███████╗
  ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝╚═╝     ╚═╝╚═════╝ ╚═╝  ╚═╝╚══════╝╚══════╝
";
private const string Subtitle = "O Guardião Inabalável.  //  Linux CVE Auditor";

    public static void PrintBanner(TextWriter? writer = null)
    {
        writer ??= Console.Error;
        
        writer.WriteLine(HeimdallHArt);
        writer.WriteLine($"  {Subtitle}");
        writer.WriteLine();
    }
}
