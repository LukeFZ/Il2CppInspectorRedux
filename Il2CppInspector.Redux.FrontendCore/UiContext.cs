using System.Diagnostics;
using Il2CppInspector.Cpp.UnityHeaders;
using Il2CppInspector.Model;
using Il2CppInspector.Redux.FrontendCore.Outputs;
using Il2CppInspector.Reflection;
using Inspector = Il2CppInspector.Il2CppInspector;

namespace Il2CppInspector.Redux.FrontendCore;

public class UiContext
{
    private const string BugReportSuffix =
        """

                                           
        If you believe this is a bug in Il2CppInspectorRedux, please use the CLI version to generate the complete output and paste it when filing a bug report.
        Do not send a screenshot of this error!                              
        """;

    private Metadata? _metadata;
    private IFileFormatStream? _binary;
    private readonly List<AppModel> _appModels = [];
    private readonly List<UnityHeaders> _potentialUnityVersions = [];

    private readonly LoadOptions _loadOptions = new();

    private readonly List<(string FormatId, string OutputDirectory, Dictionary<string, string> Settings)> _queuedExports = [];

    private async Task<bool> TryLoadMetadataFromStream(UiClient client, MemoryStream stream)
    {
        try
        {
            _metadata = Metadata.FromStream(stream, client.EventHandler);
            return true;
        }
        catch (Exception e)
        {
            await client.ShowErrorToast($"{e.Message}{BugReportSuffix}");
        }

        return false;
    }

    private async Task<bool> TryLoadBinaryFromStream(UiClient client, MemoryStream stream)
    {
        await client.ShowLogMessage("Processing binary");

        try
        {
            var file = FileFormatStream.Load(stream, _loadOptions, client.EventHandler)
                       ?? throw new InvalidOperationException("Failed to determine binary file format.");

            if (file.NumImages == 0)
                throw new InvalidOperationException("Failed to find any binary images in the file");

            _binary = file;
            return true;
        }
        catch (Exception e)
        {
            await client.ShowErrorToast($"{e.Message}{BugReportSuffix}");
        }

        return false;
    }

    private async Task<bool> TryInitializeInspector(UiClient client)
    {
        Debug.Assert(_binary != null);
        Debug.Assert(_metadata != null);

        _appModels.Clear();

        var inspectors = Inspector.LoadFromStream(_binary, _metadata, client.EventHandler);

        if (inspectors.Count == 0)
        {
            await client.ShowErrorToast(
                """
                Failed to auto-detect any IL2CPP binary images in the provided files.
                This may mean the binary file is packed, encrypted or obfuscated, that the file 
                is not an IL2CPP image or that Il2CppInspector was not able to automatically find the required data. 
                Please check the binary file in a disassembler to ensure that it is an unencrypted IL2CPP binary before submitting a bug report!
                """);

            _binary = null;
            return false;
        }

        foreach (var inspector in inspectors)
        {
            await client.ShowLogMessage(
                $"Building .NET type model for {inspector.BinaryImage.Format}/{inspector.BinaryImage.Arch} image");

            try
            {
                var typeModel = new TypeModel(inspector);

                // Just create the app model, do not initialize it - this is done lazily depending on the exports
                _appModels.Add(new AppModel(typeModel, makeDefaultBuild: false));
            }
            catch (Exception e)
            {
                await client.ShowErrorToast($"Failed to build type model: {e.Message}{BugReportSuffix}");

                // Clear out failed metadata and binary so subsequent loads do not use any stale data.
                _metadata = null;
                _binary = null;

                return false;
            }
        }

        _potentialUnityVersions.Clear();
        _potentialUnityVersions.AddRange(UnityHeaders.GuessHeadersForBinary(_appModels[0].Package.Binary));

        return true;
    }

    public async Task Initialize(UiClient client, CancellationToken cancellationToken = default)
    {
        await client.ShowSuccessToast("SignalR initialized!", cancellationToken);
    }

    public async Task LoadInputFiles(UiClient client, List<string> inputFiles,
        CancellationToken cancellationToken = default)
    {
        await using (await LoadingSession.Start(client))
        {
            var streams = Inspector.GetStreamsFromPackage(inputFiles);
            if (streams != null)
            {
                // The input files contained a package that provides the metadata and binary.
                // Use these instead of parsing all files individually.
                if (!await TryLoadMetadataFromStream(client, streams.Value.Metadata))
                    return;

                if (!await TryLoadBinaryFromStream(client, streams.Value.Binary))
                    return;
            }
            else
            {
                foreach (var inputFile in inputFiles)
                {
                    if (_metadata != null && _binary != null)
                        break;

                    await client.ShowLogMessage($"Processing {inputFile}", cancellationToken);

                    var data = await File.ReadAllBytesAsync(inputFile, cancellationToken);
                    var stream = new MemoryStream(data);

                    if ( _metadata == null && PathHeuristics.IsMetadataPath(inputFile))
                    {
                        if (await TryLoadMetadataFromStream(client, stream))
                        {
                            await client.ShowSuccessToast($"Loaded metadata (v{_metadata!.Version}) from {inputFile}", cancellationToken);
                        }
                    }
                    else if (_binary == null && PathHeuristics.IsBinaryPath(inputFile))
                    {
                        stream.Position = 0;
                        _loadOptions.BinaryFilePath = inputFile;

                        if (await TryLoadBinaryFromStream(client, stream))
                        {
                            await client.ShowSuccessToast($"Loaded binary from {inputFile}", cancellationToken);
                        }
                    }
                }
            }

            if (_metadata != null && _binary != null)
            {
                if (await TryInitializeInspector(client))
                {
                    await client.ShowSuccessToast($"Successfully loaded IL2CPP (v{_appModels[0].Package.Version}) data!", cancellationToken);
                    await client.OnImportCompleted(cancellationToken);
                }
            }
        }
    }

    public Task QueueExport(UiClient client, string exportFormatId, string outputDirectory,
        Dictionary<string, string> settings, CancellationToken cancellationToken = default)
    {
        _queuedExports.Add((exportFormatId, outputDirectory, settings));
        return Task.CompletedTask;
    }

    public async Task StartExport(UiClient client, CancellationToken cancellationToken = default)
    {
        // todo: support different app model selection (when loading packages)
        Debug.Assert(_appModels.Count > 0);

        await using (await LoadingSession.Start(client))
        {
            var model = _appModels[0];

            foreach (var (formatId, outputDirectory, settings) in _queuedExports)
            {
                try
                {
                    var outputFormat = OutputFormatRegistry.GetOutputFormat(formatId);
                    await outputFormat.Export(model, client, outputDirectory, settings);
                }
                catch (Exception ex)
                {
                    await client.ShowErrorToast($"Export for format {formatId} failed: {ex}",
                        cancellationToken);
                }
            }

            _queuedExports.Clear();
        }

        await client.ShowSuccessToast("Export finished", cancellationToken);
    }

    public Task<List<string>> GetPotentialUnityVersions()
    {
        return Task.FromResult(_potentialUnityVersions.Select(x => x.VersionRange.Min.ToString()).ToList());
    }

    public async Task ExportIl2CppFiles(UiClient client, string outputDirectory, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_appModels.Count > 0);
        var pkg = _appModels[0].Package;

        await using (await LoadingSession.Start(client))
        {
            await Task.Run(async () =>
            {
                Directory.CreateDirectory(outputDirectory);

                await client.ShowLogMessage("Extracting IL2CPP binary", cancellationToken);
                pkg.SaveBinaryToFile(Path.Join(outputDirectory, pkg.BinaryImage.DefaultFilename));

                await client.ShowLogMessage("Extracting IL2CPP metadata", cancellationToken);
                pkg.SaveMetadataToFile(Path.Join(outputDirectory, "global-metadata.dat"));

                await client.ShowSuccessToast("Successfully extracted IL2CPP files.", cancellationToken);
            }, cancellationToken);
        }
    }

    public static Task<string> GetInspectorVersion()
    {
        return Task.FromResult(typeof(UiContext).Assembly.GetAssemblyVersion() ?? "<unknown>");
    }
}