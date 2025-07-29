namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public static class OutputFormatRegistry
{
    public static IEnumerable<string> AvailableOutputFormats => OutputFormats.Keys;

    private static readonly Dictionary<string, IOutputFormat> OutputFormats = [];

    public static void RegisterOutputFormat<T>() where T : IOutputFormatProvider, new()
    {
        if (OutputFormats.ContainsKey(T.Id))
            throw new InvalidOperationException("An output format with this id was already registered.");

        OutputFormats[T.Id] = new T();
    }

    public static IOutputFormat GetOutputFormat(string id)
    {
        if (!OutputFormats.TryGetValue(id, out var format))
            throw new ArgumentException($"Failed to find output format for id {id}", nameof(id));

        return format;
    }

    private static void RegisterBuiltinOutputFormats()
    {
        RegisterOutputFormat<CSharpStubOutput>();
        RegisterOutputFormat<VsSolutionOutput>();
        RegisterOutputFormat<DummyDllOutput>();
        RegisterOutputFormat<DisassemblerMetadataOutput>();
        RegisterOutputFormat<CppScaffoldingOutput>();
    }

    static OutputFormatRegistry()
    {
        RegisterBuiltinOutputFormats();
    }
}