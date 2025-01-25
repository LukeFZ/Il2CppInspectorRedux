using System.Diagnostics;

namespace Il2CppInspector.Redux.GUI;

public class UiProcessService(IHostApplicationLifetime lifetime) : BackgroundService
{
    // NOTE: this is not really a good solution for getting people to launch the correct program.
    private const string UiExecutableName = "./resources/il2cppinspectorredux.exe";

    private Process? _uiProcess;

    public void LaunchUiProcess(int port)
    {
        _uiProcess = Process.Start(new ProcessStartInfo(UiExecutableName, [port.ToString()]));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (_uiProcess == null)
            await Task.Delay(TimeSpan.FromMilliseconds(10), stoppingToken);

        await _uiProcess.WaitForExitAsync(stoppingToken);
        lifetime.StopApplication();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        if (_uiProcess is { HasExited: false }) 
            _uiProcess.Kill();

        return base.StopAsync(cancellationToken);
    }
}