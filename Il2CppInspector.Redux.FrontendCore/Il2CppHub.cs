using Microsoft.AspNetCore.SignalR;

namespace Il2CppInspector.Redux.FrontendCore;

public class Il2CppHub : Hub
{
    private const string ContextKey = "context";

    private UiContext State
    {
        get
        {
            if (!Context.Items.TryGetValue(ContextKey, out var context)
                || context is not UiContext ctx)
            {
                Context.Items[ContextKey] = ctx = new UiContext();
            }

            return ctx;
        }
    }

    private UiClient Client => new(Clients.Caller);

    public async Task OnUiLaunched()
    {
        await State.Initialize(Client);
    }

    public async Task SubmitInputFiles(List<string> inputFiles)
    {
        await State.LoadInputFiles(Client, inputFiles);
    }

    public async Task QueueExport(string exportTypeId, string outputDirectory, Dictionary<string, string> settings)
    {
        await State.QueueExport(Client, exportTypeId, outputDirectory, settings);
    }

    public async Task StartExport()
    {
        await State.StartExport(Client);
    }

    public async Task<IEnumerable<string>> GetPotentialUnityVersions()
    {
        return await State.GetPotentialUnityVersions();
    }

    public async Task ExportIl2CppFiles(string outputDirectory)
    {
        await State.ExportIl2CppFiles(Client, outputDirectory);
    }
    public async Task<string> GetInspectorVersion()
    {
        return await UiContext.GetInspectorVersion();
    }
}