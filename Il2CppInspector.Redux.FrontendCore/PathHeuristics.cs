namespace Il2CppInspector.Redux.FrontendCore;

public static class PathHeuristics
{
    private static readonly string[] AllowedMetadataExtensionComponents =
    [
        "dat", "dec"
    ];

    private static readonly string[] AllowedMetadataNameComponents =
    [
        "metadata"
    ];

    private static readonly string[] AllowedBinaryPathComponents =
    [
        "GameAssembly",
        "il2cpp",
        "UnityFramework"
    ];

    private static readonly string[] AllowedBinaryExtensionComponents =
    [
        "dll", "so", "exe", "bin", "prx", "sprx", "dylib"
    ];

    public static bool IsMetadataPath(string path)
    {
        var extension = Path.GetExtension(path);
        if (AllowedMetadataExtensionComponents.Any(extension.Contains))
            return true;

        var filename = Path.GetFileNameWithoutExtension(path);
        if (AllowedMetadataNameComponents.Any(filename.Contains))
            return true;

        return false;
    }

    public static bool IsBinaryPath(string path)
    {
        var extension = Path.GetExtension(path);

        // empty to allow macho binaries which do not have an extension
        if (extension == "" || AllowedBinaryExtensionComponents.Any(extension.Contains))
            return true;

        var filename = Path.GetFileNameWithoutExtension(path);
        if (AllowedBinaryPathComponents.Any(filename.Contains))
            return true;

        return false;
    }
}