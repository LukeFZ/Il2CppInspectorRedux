using System.Threading.Channels;
using Il2CppInspector.Redux.FrontendCore;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;

namespace Il2CppInspector.Redux.CLI;

public class CliClient : IDisposable
{
    public bool ImportCompleted { get; private set; }

    private volatile int _finishedLoadingCount = 0;

    private readonly HubConnection _connection;
    private readonly List<IDisposable> _commandListeners = [];

    private Channel<string>? _logMessageChannel;

    public CliClient(HubConnection connection)
    {
        _connection = connection;

        _commandListeners.Add(_connection.On<string>(nameof(UiClient.ShowLogMessage), ShowLogMessage));
        _commandListeners.Add(_connection.On(nameof(UiClient.BeginLoading), BeginLoading));
        _commandListeners.Add(_connection.On(nameof(UiClient.FinishLoading), FinishLoading));
        _commandListeners.Add(_connection.On<string>(nameof(UiClient.ShowInfoToast), ShowInfoToast));
        _commandListeners.Add(_connection.On<string>(nameof(UiClient.ShowSuccessToast), ShowSuccessToast));
        _commandListeners.Add(_connection.On<string>(nameof(UiClient.ShowErrorToast), ShowErrorToast));
        _commandListeners.Add(_connection.On(nameof(UiClient.OnImportCompleted), OnImportCompleted));
    }

    public async ValueTask OnUiLaunched(CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync(nameof(Il2CppHub.OnUiLaunched), cancellationToken);
    }

    public async ValueTask SubmitInputFiles(List<string> inputFiles, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync(nameof(Il2CppHub.SubmitInputFiles), inputFiles, cancellationToken);
    }

    public async ValueTask QueueExport(string exportTypeId, string outputDirectory, Dictionary<string, string> settings, 
        CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync(nameof(Il2CppHub.QueueExport), exportTypeId, outputDirectory, settings, cancellationToken);
    }

    public async ValueTask StartExport(CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync(nameof(Il2CppHub.StartExport), cancellationToken);
    }

    public async ValueTask<List<string>> GetPotentialUnityVersions(CancellationToken cancellationToken = default)
        => await _connection.InvokeAsync<List<string>>(nameof(Il2CppHub.GetPotentialUnityVersions), cancellationToken);

    public async ValueTask ExportIl2CppFiles(string outputDirectory, CancellationToken cancellationToken = default)
    {
        await _connection.InvokeAsync(nameof(Il2CppHub.ExportIl2CppFiles), outputDirectory, cancellationToken);
    }

    public async ValueTask<string> GetInspectorVersion(CancellationToken cancellationToken = default) 
        => await _connection.InvokeAsync<string>(nameof(Il2CppHub.GetInspectorVersion), cancellationToken);

    public async ValueTask WaitForLoadingToFinishAsync(CancellationToken cancellationToken = default)
    {
        var currentLoadingCount = _finishedLoadingCount;
        while (_finishedLoadingCount == currentLoadingCount)
            await Task.Delay(10, cancellationToken);
    }

    private async Task ShowLogMessage(string message)
    {
        if (_logMessageChannel == null)
        {
            AnsiConsole.MarkupLine($"[white bold]{message}[/]");
            return;
        }

        await _logMessageChannel.Writer.WriteAsync(message);
    }

    private void BeginLoading()
    {
        _logMessageChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });

        AnsiConsole.Status()
            .Spinner(Spinner.Known.Triangle)
            .StartAsync("Loading", async ctx =>
            {
                await foreach (var newLogMessage in _logMessageChannel.Reader.ReadAllAsync())
                {
                    ctx.Status(newLogMessage);
                }
            });
    }

    private void FinishLoading()
    {
        _logMessageChannel?.Writer.Complete();
        Interlocked.Increment(ref _finishedLoadingCount);
    }

    private static void ShowInfoToast(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold white]INFO: {message}[/]");
    }

    private static void ShowSuccessToast(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold][green]SUCCESS: [/] [white]{message}[/][/]");
    }

    private static void ShowErrorToast(string message)
    {
        AnsiConsole.MarkupLineInterpolated($"[bold][red]ERROR: [/] [white]{message}[/][/]");
    }

    private void OnImportCompleted()
    {
        ImportCompleted = true;
    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (var listener in _commandListeners)
            listener.Dispose();
    }
}