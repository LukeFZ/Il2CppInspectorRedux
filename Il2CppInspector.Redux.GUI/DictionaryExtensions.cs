namespace Il2CppInspector.Redux.GUI;

public static class DictionaryExtensions
{
    public static bool GetAsBooleanOrDefault(this Dictionary<string, string> dict, string key, bool defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && bool.TryParse(value, out var boolResult))
            return boolResult;

        return defaultValue;
    }

    public static T GetAsEnumOrDefault<T>(this Dictionary<string, string> dict, string key, T defaultValue)
        where T : struct, Enum
    {
        if (dict.TryGetValue(key, out var value) && Enum.TryParse<T>(value, true, out var enumResult))
            return enumResult;

        return defaultValue;
    }
}