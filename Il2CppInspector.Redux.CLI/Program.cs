using Il2CppInspector.Redux.CLI;
using Il2CppInspector.Redux.CLI.Commands;
using Il2CppInspector.Redux.FrontendCore;
using Microsoft.AspNetCore.SignalR;
using Spectre.Console.Cli;

var builder = WebApplication.CreateSlimBuilder();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, FrontendCoreJsonSerializerContext.Default);
});

builder.Services.Configure<JsonHubProtocolOptions>(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, FrontendCoreJsonSerializerContext.Default);
});

builder.Services.AddFrontendCore();
builder.Logging.ClearProviders();

var app = builder.Build();

app.UseCors();

app.MapFrontendCore();

await app.StartAsync();

var serverUrl = app.Urls.First();
var port = new Uri(serverUrl).Port;

var commandServiceProvider = new ServiceCollection();
commandServiceProvider.AddSingleton(new PortProvider(port));

var commandTypeRegistrar = new ServiceTypeRegistrar(commandServiceProvider);
var consoleApp = new CommandApp<InteractiveCommand>(commandTypeRegistrar);

consoleApp.Configure(config =>
{
    config.AddCommand<ProcessCommand>("process")
        .WithDescription("Processes the provided input data into one or more output formats.");
});

await consoleApp.RunAsync(args);
await app.StopAsync();