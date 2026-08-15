namespace Heimdall.Infrastructure.Presentation;

public class EyeSpinner : IDisposable
{
    private static readonly string[] EyeFrames = ["◉", "◉", "◉", "◔", "─", "◔", "◉"];
    private readonly double _intervalSeconds;
    private readonly TextWriter _writer;
    private readonly Lock _lock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task? _renderTask;
    private string _status = "iniciando...";
    private readonly bool _isTty;

    public EyeSpinner(double intervalSeconds = 0.15, TextWriter? writer = null)
    {
        _intervalSeconds = intervalSeconds;
        _writer = writer ?? Console.Error;
        _isTty = !Console.IsOutputRedirected && !Console.IsErrorRedirected;

        _renderTask = Task.Run(RenderLoopAsync);
    }

    public void Status(string text)
    {
        lock (_lock)
        {
            _status = text;
        }
    }

    public void Log(string text)
    {
        lock (_lock)
        {
            if (_isTty)
            {
                _writer.Write("\r" + new string(' ', 79) + "\r");
            }
            _writer.WriteLine(text);
            _writer.Flush();
        }
    }

    private async Task RenderLoopAsync()
    {
        var frameIndex = 0;
        while (!_cts.Token.IsCancellationRequested)
        {
            lock (_lock)
            {
                if (_isTty)
                {
                    var frame = EyeFrames[frameIndex % EyeFrames.Length];
                    var line = $"{frame}  {_status}";
                    _writer.Write($"\r{line.PadRight(79)[..79]}");
                    _writer.Flush();
                }
            }
            frameIndex++;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _renderTask?.Wait(1000);
        }
        catch
        {
            // Ignore cancellation/wait exceptions on dispose
        }

        if (_isTty)
        {
            _writer.Write("\r" + new string(' ', 79) + "\r");
            _writer.Flush();
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
