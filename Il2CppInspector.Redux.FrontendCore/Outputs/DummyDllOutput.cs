using Il2CppInspector.Model;
using Il2CppInspector.Outputs;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public class DummyDllOutput : IOutputFormatProvider
{
    public static string Id => "dummydlls";

    private class Settings(Dictionary<string, string> dict)
    {
        public readonly bool SuppressMetadata = dict.GetAsBooleanOrDefault("suppressmetadata", false);
    }

    public async Task Export(AppModel model, UiClient client, string outputPath, Dictionary<string, string> settingsDict)
    {
        var outputSettings = new Settings(settingsDict);

        await client.ShowLogMessage("Generating .NET dummy assemblies");
        var shims = new AssemblyShims(model.TypeModel)
        {
            SuppressMetadata = outputSettings.SuppressMetadata
        };

        shims.Write(outputPath, client.EventHandler);
    }
}