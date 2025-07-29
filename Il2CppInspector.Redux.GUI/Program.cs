using Il2CppInspector.Redux.FrontendCore;
using Il2CppInspector.Redux.GUI;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, FrontendCoreJsonSerializerContext.Default);
});

builder.Services.Configure<JsonHubProtocolOptions>(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, FrontendCoreJsonSerializerContext.Default);
});

builder.Services.AddFrontendCore();
builder.Services.AddSingleton<UiProcessService>();
builder.Services.AddHostedService(p => p.GetRequiredService<UiProcessService>());

var app = builder.Build();

app.UseCors();

app.MapFrontendCore();

await app.StartAsync();

var serverUrl = app.Urls.First();
var port = new Uri(serverUrl).Port;

#if DEBUG
Console.WriteLine($"Listening on port {port}");
#else
app.Services.GetRequiredService<UiProcessService>().LaunchUiProcess(port);
#endif

await app.WaitForShutdownAsync();