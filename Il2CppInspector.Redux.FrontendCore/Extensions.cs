using System.Reflection;

namespace Il2CppInspector.Redux.FrontendCore;

public static class Extensions
{
    internal static bool GetAsBooleanOrDefault(this Dictionary<string, string> dict, string key, bool defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && bool.TryParse(value, out var boolResult))
            return boolResult;

        return defaultValue;
    }

    internal static T GetAsEnumOrDefault<T>(this Dictionary<string, string> dict, string key, T defaultValue)
        where T : struct, Enum
    {
        if (dict.TryGetValue(key, out var value) && Enum.TryParse<T>(value, true, out var enumResult))
            return enumResult;

        return defaultValue;
    }

    internal static string? GetAssemblyVersion(this Assembly assembly)
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static WebApplication MapFrontendCore(this WebApplication app)
    {
        app.MapHub<Il2CppHub>("/il2cpp");
        return app;
    }

    public static IServiceCollection AddFrontendCore(this IServiceCollection services)
    {
        services.AddSignalR(config =>
        {
#if DEBUG
    config.EnableDetailedErrors = true;
#endif
        });

        return services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                        origin.StartsWith("http://localhost") || origin.StartsWith("http://tauri.localhost"))
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST")
                    .AllowCredentials();
            });
        });
    }
}