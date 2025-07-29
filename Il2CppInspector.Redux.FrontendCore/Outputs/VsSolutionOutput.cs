using Il2CppInspector.Model;
using Il2CppInspector.Outputs;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public class VsSolutionOutput : IOutputFormatProvider
{
    public static string Id => "vssolution";

    private class Settings(Dictionary<string, string> settings)
    {
        public readonly string UnityPath = settings.GetValueOrDefault("unitypath", "");
        public readonly string UnityAssembliesPath = settings.GetValueOrDefault("unityassembliespath", "");
    }

    public async Task Export(AppModel model, UiClient client, string outputPath, Dictionary<string, string> settingsDict)
    {
        var settings = new Settings(settingsDict);

        var writer = new CSharpCodeStubs(model.TypeModel)
        {
            MustCompile = true,
            SuppressMetadata = true
        };

        await client.ShowLogMessage("Writing Visual Studio solution");
        writer.WriteSolution(outputPath, settings.UnityPath, settings.UnityAssembliesPath);
    }
}