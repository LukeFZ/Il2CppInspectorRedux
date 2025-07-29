using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public class CppScaffoldingOutput : IOutputFormatProvider
{
    public static string Id => "cppscaffolding";

    private class Settings(Dictionary<string, string> settings)
    {
        public readonly string UnityVersion = settings.GetValueOrDefault("unityversion", "");
        public readonly CppCompilerType Compiler = settings.GetAsEnumOrDefault("compiler", CppCompilerType.GCC);
    }

    public async Task Export(AppModel model, UiClient client, string outputPath, Dictionary<string, string> settingsDict)
    {
        var settings = new Settings(settingsDict);
        
        await client.ShowLogMessage($"Building application model for Unity {settings.UnityVersion}/{settings.Compiler}");
        model.Build(new UnityVersion(settings.UnityVersion), settings.Compiler);

        await client.ShowLogMessage("Generating C++ scaffolding");
        var scaffolding = new CppScaffolding(model);

        await client.ShowLogMessage("Writing C++ scaffolding");
        scaffolding.Write(outputPath);
    }
}