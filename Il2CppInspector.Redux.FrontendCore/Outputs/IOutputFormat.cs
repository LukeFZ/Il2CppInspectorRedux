using Il2CppInspector.Model;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public interface IOutputFormat
{
    public Task Export(AppModel model, UiClient client, string outputPath,
        Dictionary<string, string> settingsDict);
}

public interface IOutputFormatProvider : IOutputFormat
{
    public static abstract string Id { get; }
}