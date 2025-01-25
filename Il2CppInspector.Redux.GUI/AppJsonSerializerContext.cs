using System.Text.Json.Serialization;

namespace Il2CppInspector.Redux.GUI;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;