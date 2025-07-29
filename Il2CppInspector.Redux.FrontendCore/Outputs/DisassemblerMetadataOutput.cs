using Il2CppInspector.Cpp;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Outputs;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public class DisassemblerMetadataOutput : IOutputFormatProvider
{
    public static string Id => "disassemblermetadata";

    private class Settings(Dictionary<string, string> dict)
    {
        public readonly DisassemblerType Disassembler = dict.GetAsEnumOrDefault("disassembler", DisassemblerType.IDA);
        public readonly string UnityVersion = dict.GetValueOrDefault("unityversion", "");
    }

    public async Task Export(AppModel model, UiClient client, string outputPath, Dictionary<string, string> settingsDict)
    {
        var settings = new Settings(settingsDict);

        await client.ShowLogMessage($"Building application model for Unity {settings.UnityVersion}/{CppCompilerType.GCC}");
        model.Build(new UnityVersion(settings.UnityVersion), CppCompilerType.GCC);

        var headerPath = Path.Join(outputPath, "il2cpp.h");
        {
            await client.ShowLogMessage("Generating C++ types");
            var cppScaffolding = new CppScaffolding(model, useBetterArraySize: true);

            await client.ShowLogMessage("Writing C++ types");
            cppScaffolding.WriteTypes(headerPath);
        }

        var metadataPath = Path.Join(outputPath, "il2cpp.json");
        {
            await client.ShowLogMessage("Generating disassembler metadata");
            var jsonMetadata = new JSONMetadata(model);

            await client.ShowLogMessage("Writing disassembler metadata");
            jsonMetadata.Write(metadataPath);
        }

        if (settings.Disassembler != DisassemblerType.None)
        {
            var scriptPath = Path.Join(outputPath, "il2cpp.py");
            await client.ShowLogMessage($"Generating python script for {settings.Disassembler}");
            var script = new PythonScript(model);

            await client.ShowLogMessage($"Writing python script for {settings.Disassembler}");
            script.WriteScriptToFile(scriptPath, settings.Disassembler.ToString(), headerPath, metadataPath);
        }
    }
}