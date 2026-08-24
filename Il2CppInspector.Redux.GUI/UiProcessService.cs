using System.Diagnostics;

namespace Il2CppInspector.Redux.GUI;

public class UiProcessService(IHostApplicationLifetime lifetime) : BackgroundService
{
    // TODO: This needs to be adjusted for multiplatform support
    private const string UiExecutableName = "il2cppinspectorredux.exe";

    private Process? _uiProcess;
    private string? _uiExectuablePath;
    private readonly TaskCompletionSource<Process> _uiProcessCreatedTask = new();

    public void LaunchUiProcess(int port)
    {
        _uiExectuablePath ??= ExtractUiExecutable();
        var process = Process.Start(_uiExectuablePath, [port.ToString()]);
        _uiProcessCreatedTask.SetResult(process);
    }

    private static string ExtractUiExecutable()
    {
        try
        {
            using var executable =
                typeof(UiProcessService).Assembly.GetManifestResourceStream(
                    $"{typeof(UiProcessService).Namespace!}.{UiExecutableName}");

            if (executable == null)
                throw new FileNotFoundException("Failed to open resource as stream.");

            var tempDir = Directory.CreateTempSubdirectory("il2cppinspectorredux-ui");
            var uiExePath = Path.Join(tempDir.FullName, UiExecutableName);

            using var fs = File.Create(uiExePath);
            executable.CopyTo(fs);
            return uiExePath;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to find embedded UI executable: {ex}");
        }
    }

    private async Task<Process> WaitForUiLaunchAsync(CancellationToken cancellationToken)
    {
        _uiProcess = await _uiProcessCreatedTask.Task.WaitAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return _uiProcess;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var process = await WaitForUiLaunchAsync(stoppingToken);
        await process.WaitForExitAsync(stoppingToken);
        lifetime.StopApplication();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_uiProcess is { HasExited: false }) 
            _uiProcess.Kill();

        if (_uiExectuablePath != null)
            File.Delete(_uiExectuablePath);

        return base.StopAsync(cancellationToken);
    }
}