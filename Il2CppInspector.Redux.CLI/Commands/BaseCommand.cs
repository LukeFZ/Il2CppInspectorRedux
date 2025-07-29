using Il2CppInspector.Redux.FrontendCore;
using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal abstract class BaseCommand<T>(PortProvider portProvider) : AsyncCommand<T> where T : CommandSettings
{
    private const string HubPath = "/il2cpp"; // TODO: Make this into a shared constant

    private readonly int _serverPort = portProvider.Port;

    protected abstract Task<int> ExecuteAsync(CliClient client, T settings);

    public override async Task<int> ExecuteAsync(CommandContext context, T settings)
    {
        var connection = new HubConnectionBuilder().WithUrl($"http://localhost:{_serverPort}{HubPath}")
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0,
                    FrontendCoreJsonSerializerContext.Default);
            })
            .Build();

        await connection.StartAsync();

        int result;
        using (var client = new CliClient(connection))
        {
            await client.OnUiLaunched();
            result = await ExecuteAsync(client, settings);
        }

        await connection.StopAsync();

        return result;
    }
}