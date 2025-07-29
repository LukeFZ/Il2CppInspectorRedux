using Il2CppInspector.Model;
using Il2CppInspector.Outputs;

namespace Il2CppInspector.Redux.FrontendCore.Outputs;

public class CSharpStubOutput : IOutputFormatProvider
{
    public static string Id => "cs";

    private class Settings(Dictionary<string, string> settings)
    {
        public readonly CSharpLayout Layout = settings.GetAsEnumOrDefault("layout", CSharpLayout.SingleFile);
        public readonly bool FlattenHierarchy = settings.GetAsBooleanOrDefault("flattenhierarchy", false);
        public readonly TypeSortingMode SortingMode = settings.GetAsEnumOrDefault("sortingmode", TypeSortingMode.Alphabetical);
        public readonly bool SuppressMetadata = settings.GetAsBooleanOrDefault("suppressmetadata", false);
        public readonly bool MustCompile = settings.GetAsBooleanOrDefault("mustcompile", false);
        public readonly bool SeperateAssemblyAttributes = settings.GetAsBooleanOrDefault("seperateassemblyattributes", true);
    }

    public async Task Export(AppModel model, UiClient client, string outputPath, Dictionary<string, string> settingsDict)
    {
        var settings = new Settings(settingsDict);

        var writer = new CSharpCodeStubs(model.TypeModel)
        {
            SuppressMetadata = settings.SuppressMetadata,
            MustCompile = settings.MustCompile
        };

        await client.ShowLogMessage("Writing C# type definitions");

        var outputPathFile = Path.Join(outputPath, "il2cpp.cs");

        switch (settings.Layout, settings.SortingMode)
        {
            case (CSharpLayout.SingleFile, TypeSortingMode.TypeDefinitionIndex):
                writer.WriteSingleFile(outputPathFile, info => info.Index);
                break;
            case (CSharpLayout.SingleFile, TypeSortingMode.Alphabetical):
                writer.WriteSingleFile(outputPathFile, info => info.Name);
                break;
            
            case (CSharpLayout.Namespace, TypeSortingMode.TypeDefinitionIndex):
                writer.WriteFilesByNamespace(outputPath, info => info.Index, settings.FlattenHierarchy);
                break;
            case (CSharpLayout.Namespace, TypeSortingMode.Alphabetical):
                writer.WriteFilesByNamespace(outputPath, info => info.Name, settings.FlattenHierarchy);
                break;

            case (CSharpLayout.Assembly, TypeSortingMode.TypeDefinitionIndex):
                writer.WriteFilesByAssembly(outputPath, info => info.Index, settings.SeperateAssemblyAttributes);
                break;
            case (CSharpLayout.Assembly, TypeSortingMode.Alphabetical):
                writer.WriteFilesByAssembly(outputPath, info => info.Name, settings.SeperateAssemblyAttributes);
                break;

            case (CSharpLayout.Class, _):
                writer.WriteFilesByClass(outputPath, settings.FlattenHierarchy);
                break;
            
            case (CSharpLayout.Tree, _):
                writer.WriteFilesByClassTree(outputPath, settings.SeperateAssemblyAttributes);
                break;
        }
    }
}