using System.Globalization;
using Il2CppInspector.Cpp;
using Il2CppInspector.Redux.FrontendCore;
using Il2CppInspector.Redux.FrontendCore.Outputs;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal sealed class ProcessCommand(PortProvider portProvider) : ManualCommand<ProcessCommand.Settings>(portProvider)
{
    // NOTE: There might be a better option than replicating all available flags here (and in the TS UI).
    // Investigate this in the future.

    public sealed class Settings : ManualCommandSettings
    {
        // C++ Scaffolding
        [CommandOption("--output-cpp-scaffolding")]
        public bool CppScaffolding { get; init; } = false;

        [CommandOption("--unity-version")]
        public string? UnityVersion { get; init; }

        [CommandOption("--compiler-type")]
        public CppCompilerType CompilerType { get; init; } = CppCompilerType.GCC;

        // C# stub
        [CommandOption("-s|--output-csharp-stub")]
        public bool CSharpStubs { get; init; } = false;

        [CommandOption("--layout")]
        public CSharpLayout Layout { get; init; } = CSharpLayout.SingleFile;

        [CommandOption("--flatten-hierarchy")]
        public bool FlattenHierarchy { get; init; } = false;

        [CommandOption("--sorting-mode")]
        public TypeSortingMode SortingMode { get; init; } = TypeSortingMode.Alphabetical;

        [CommandOption("--suppress-metadata")]
        public bool SuppressMetadata { get; init; } = false;

        [CommandOption("--compilable")]
        public bool MustCompile { get; init; } = false;

        [CommandOption("--separate-assembly-attributes")]
        public bool SeparateAssemblyAttributes { get; init; } = true;

        // Disassembler metadata
        [CommandOption("-m|--output-disassembler-metadata")]
        public bool DisassemblerMetadata { get; init; } = false;

        [CommandOption("--disassembler")]
        public DisassemblerType Disassembler { get; init; } = DisassemblerType.IDA;

        // Dummy DLL output
        [CommandOption("-d|--output-dummy-dlls")]
        public bool DummyDlls { get; init; } = false;

        // Visual Studio solution
        [CommandOption("--output-vs-solution")]
        public bool VsSolution { get; init; } = false;

        [CommandOption("--unity-path")]
        public string? UnityPath { get; init; }

        [CommandOption("--unity-assemblies-path")]
        public string? UnityAssembliesPath { get; init; }

        [CommandOption("--extract-il2cpp-files")]
        public string? ExtractIl2CppFilesPath { get; init; }

        [CommandOption("--image-base")]
        public string? ImageBase { get; init; }

        [CommandOption("--name-translation-map")]
        public string? NameTranslationMap { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CliClient client, Settings settings)
    {
        var inspectorVersion = await client.GetInspectorVersion();
        AnsiConsole.MarkupLineInterpolated($"Using inspector [gray]{inspectorVersion}[/]");

        var imageBase = 0uL;
        if (settings.ImageBase != null)
        {
            imageBase = ulong.Parse(settings.ImageBase,
                settings.ImageBase.StartsWith("0x") 
                    ? NumberStyles.HexNumber 
                    : NumberStyles.Integer);

            AnsiConsole.MarkupLineInterpolated($"Setting image base to [white]0x{imageBase:x}[/]");
        }

        await client.SetSettings(new InspectorSettings(imageBase, settings.NameTranslationMap));

        await client.SubmitInputFiles(settings.InputPaths.ToList());
        await client.WaitForLoadingToFinishAsync();
        if (!client.ImportCompleted)
        {
            AnsiConsole.MarkupLine("[bold][red]FAILED[/] to load IL2CPP data from the given inputs.[/]");
            return 1;
        }

        if (settings.ExtractIl2CppFilesPath != null)
        {
            await client.ExportIl2CppFiles(settings.ExtractIl2CppFilesPath);
            await client.WaitForLoadingToFinishAsync();
        }

        var outputPath = settings.OutputPath ?? Directory.GetCurrentDirectory();
        var unityVersions = await client.GetPotentialUnityVersions();

        if (settings.CppScaffolding)
        {
            var directory = Path.Join(outputPath, "cpp");
            await client.QueueExport(CppScaffoldingOutput.Id, directory, new Dictionary<string, string>
            {
                ["unityversion"] = settings.UnityVersion ?? unityVersions.First(),
                ["compilertype"] = settings.CompilerType.ToString()
            });
        }

        if (settings.CSharpStubs)
        {
            var directory = Path.Join(outputPath, "cs");
            await client.QueueExport(CSharpStubOutput.Id, directory, new Dictionary<string, string>
            {
                ["layout"] = settings.Layout.ToString(),
                ["flattenhierarchy"] = settings.FlattenHierarchy.ToString(),
                ["sortingmode"] = settings.SortingMode.ToString(),
                ["suppressmetadata"] = settings.SuppressMetadata.ToString(),
                ["mustcompile"] = settings.MustCompile.ToString(),
                ["separateassemblyattributes"] = settings.SeparateAssemblyAttributes.ToString()
            });
        }

        if (settings.DisassemblerMetadata)
        {
            await client.QueueExport(DisassemblerMetadataOutput.Id, outputPath, 
                new Dictionary<string, string>
            {
                ["disassembler"] = settings.Disassembler.ToString(),
                ["unityversion"] = settings.UnityVersion ?? unityVersions.First()
            });
        }

        if (settings.DummyDlls)
        {
            var directory = Path.Join(outputPath, "dll");
            await client.QueueExport(DummyDllOutput.Id, directory, new Dictionary<string, string>
            {
                ["suppressmetadata"] = settings.SuppressMetadata.ToString()
            });
        }

        if (settings.VsSolution)
        {
            var directory = Path.Join(outputPath, "vs");
            await client.QueueExport(VsSolutionOutput.Id, directory, new Dictionary<string, string>
            {
                ["unitypath"] = settings.UnityPath ?? "",
                ["unityassembliespath"] = settings.UnityAssembliesPath ?? ""
            });
        }

        await client.StartExport();
        await client.WaitForLoadingToFinishAsync();
        return 0;
    }

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if (settings.UnityPath != null && !Path.Exists(settings.UnityPath))
            return ValidationResult.Error($"Provided Unity path {settings.UnityPath} does not exist.");

        if (settings.UnityAssembliesPath != null && !Path.Exists(settings.UnityAssembliesPath))
            return ValidationResult.Error($"Provided Unity assemblies path {settings.UnityAssembliesPath} does not exist.");

        if (settings.ExtractIl2CppFilesPath != null && File.Exists(settings.ExtractIl2CppFilesPath))
            return ValidationResult.Error(
                $"Provided extracted IL2CPP files path {settings.ExtractIl2CppFilesPath} already exists as a file.");

        if (settings.NameTranslationMap != null && !File.Exists(settings.NameTranslationMap))
            return ValidationResult.Error($"Provided name translation map {settings.NameTranslationMap} does not exist.");

        if (settings is
            {
                CppScaffolding: false, CSharpStubs: false, DisassemblerMetadata: false, DummyDlls: false,
                VsSolution: false
            })
            return ValidationResult.Error("At least one output format must be specified.");

        return base.Validate(context, settings);
    }
}