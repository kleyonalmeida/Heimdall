namespace Heimdall.Infrastructure.Presentation;

public static class ConsolePresenter
{
    private const string HeimdallEyeArt = @"
              _.-'''''-._
           ,-'             `-.
         ,'                   `.
        /      .-'''''-.       \
       |      /         \       |
       |     |    .-.    |      |
       |     |   ( ● )   |      |
       |     |    '-'    |      |
       |      \         /       |
        \      '-.....-'       /
         `.                  ,'
           `-._           _,-'
               `'-------'`
";

    private const string Title = "HEIMDALL";
    private const string Subtitle = "cem olhos, nenhum descanso  //  Linux CVE Auditor";

    public static void PrintBanner(TextWriter? writer = null)
    {
        writer ??= Console.Error;
        
        writer.WriteLine(HeimdallEyeArt);
        writer.WriteLine($"    {Title}");
        writer.WriteLine($"  {Subtitle}");
        writer.WriteLine();
    }
}
