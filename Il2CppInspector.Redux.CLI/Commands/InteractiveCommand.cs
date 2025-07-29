using Microsoft.AspNetCore.SignalR.Client;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal class InteractiveCommand(PortProvider portProvider) : BaseCommand<InteractiveCommand.Options>(portProvider)
{
    public class Options : CommandSettings;

    protected override async Task<int> ExecuteAsync(CliClient client, Options settings)
    {
        await Task.Delay(1000);
        await AnsiConsole.AskAsync<string>("meow?");
        return 0;
    }
}