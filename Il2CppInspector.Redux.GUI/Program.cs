using Il2CppInspector.Redux.GUI;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddSignalR(config =>
{
#if DEBUG
    config.EnableDetailedErrors = true;
#endif
});

builder.Services.Configure<JsonHubProtocolOptions>(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin => origin.StartsWith("http://localhost") || origin.StartsWith("http://tauri.localhost"))
            .AllowAnyHeader()
            .WithMethods("GET", "POST")
            .AllowCredentials();
    });
});

builder.Services.AddSingleton<UiProcessService>();
builder.Services.AddSingleton<IHostedService>(p => p.GetRequiredService<UiProcessService>());

var app = builder.Build();

app.UseCors();

app.MapHub<Il2CppHub>("/il2cpp");

await app.StartAsync();

var serverUrl = app.Urls.First();
var port = new Uri(serverUrl).Port;

#if DEBUG
Console.WriteLine($"Listening on port {port}");
#else
app.Services.GetRequiredService<UiProcessService>().LaunchUiProcess(port);
#endif

await app.WaitForShutdownAsync();