namespace Il2CppInspector.Redux.FrontendCore;

public class LoadingSession : IAsyncDisposable
{
    private readonly UiClient _client;

    private LoadingSession(UiClient client)
    {
        _client = client;
    }

    public static async Task<LoadingSession> Start(UiClient client)
    {
        await client.BeginLoading();
        return new LoadingSession(client);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.FinishLoading();
        GC.SuppressFinalize(this);
    }
}