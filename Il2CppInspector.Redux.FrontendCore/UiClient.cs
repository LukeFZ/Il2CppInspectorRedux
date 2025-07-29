using Microsoft.AspNetCore.SignalR;

namespace Il2CppInspector.Redux.FrontendCore;

public class UiClient(ISingleClientProxy client)
{
    private EventHandler<string>? _handler;

    public EventHandler<string> EventHandler
    {
        get
        {
            _handler ??= (_, status) =>
            {
#pragma warning disable CS4014
                ShowLogMessage(status);
#pragma warning restore CS4014
            };

            return _handler;
        }
    }

    public async Task ShowLogMessage(string message, CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(ShowLogMessage), message, cancellationToken);

    public async Task BeginLoading(CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(BeginLoading), cancellationToken);

    public async Task FinishLoading(CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(FinishLoading), cancellationToken);

    public async Task ShowInfoToast(string message, CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(ShowInfoToast), message, cancellationToken);

    public async Task ShowSuccessToast(string message, CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(ShowSuccessToast), message, cancellationToken);

    public async Task ShowErrorToast(string message, CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(ShowErrorToast), message, cancellationToken);

    public async Task OnImportCompleted(CancellationToken cancellationToken = default)
        => await client.SendAsync(nameof(OnImportCompleted), cancellationToken);
}