using System.Text.Json.Serialization;

namespace Il2CppInspector.Redux.FrontendCore;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class FrontendCoreJsonSerializerContext : JsonSerializerContext;