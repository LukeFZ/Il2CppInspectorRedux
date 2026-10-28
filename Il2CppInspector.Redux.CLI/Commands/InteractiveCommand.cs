using Il2CppInspector.Redux.FrontendCore.Outputs;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal sealed class InteractiveCommand(PortProvider portProvider) : BaseCommand<InteractiveCommand.Settings>(portProvider)
{
    public sealed class Settings : CommandSettings;

    protected override async Task<int> ExecuteAsync(CliClient client, Settings settings)
    {
        Console.CancelKeyPress += (e, evt) =>
        {
            Environment.Exit(0);
        };

        AnsiConsole.MarkupLine("[white bold]Welcome to the interactive Il2CppInspectorRedux CLI![/]");
        AnsiConsole.MarkupLine("(For regular CLI usage, use the process command)");
        AnsiConsole.MarkupLine("");

        while (!client.ImportCompleted)
        {
            var inputPath = AnsiConsole
                .Ask<string>("Please enter the file path to a [green bold]valid input file[/] and press enter.")
                .Trim('"', '\'');

            if (!File.Exists(inputPath) && !Directory.Exists(inputPath))
            {
                AnsiConsole.MarkupLine("[red bold]Error: Input file does not exist.[/]");
                continue;
            }

            await client.SubmitInputFiles([inputPath]);
            await client.WaitForLoadingToFinishAsync();
        }

        AnsiConsole.MarkupLine("Successfully loaded IL2CPP data from the provided files!");

        var unityVersions = await client.GetPotentialUnityVersions();

        string outputPath;
        while (true)
        {
            outputPath = AnsiConsole.Ask<string>("Please enter your desired output folder path:");
            if (File.Exists(outputPath))
            {
                AnsiConsole.MarkupLine("[red bold]Error: Your output folder is a file.[/]");
                continue;
            }

            break;
        }

        var outputFormats = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Which [white bold]output formats[/] do you want? (more options are available in the GUI/manual CLI)")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle a fruit, [green]<enter>[/] to accept)[/]")
                .AddChoices("Dummy DLLs", "Disassembler Script"));

        if (outputFormats.Contains("Dummy DLLs"))
        {
            AnsiConsole.MarkupLine("[gray]Creating dummy DLLs...[/]");

            var suppressMetadata =
                await AnsiConsole.AskAsync("Should metadata attributes be suppressed?", false);

            var directory = Path.Join(outputPath, "dll");
            await client.QueueExport(DummyDllOutput.Id, directory, new Dictionary<string, string>
            {
                ["suppressmetadata"] = suppressMetadata.ToString()
            });

            AnsiConsole.MarkupLine("[green bold]Successfully[/] [white bold]created dummy DLLs![/]");
        }

        if (outputFormats.Contains("Disassembler Script"))
        {
            AnsiConsole.MarkupLine("[gray]Creating diassembler script...[/]");

            var disassembler =
                await AnsiConsole.PromptAsync(
                    new SelectionPrompt<DisassemblerType>().Title("Which disassembler are you using?")
                        .AddChoices(DisassemblerType.IDA, DisassemblerType.Ghidra, DisassemblerType.BinaryNinja));

            await client.QueueExport(DisassemblerMetadataOutput.Id, outputPath,
                new Dictionary<string, string>
                {
                    ["disassembler"] = disassembler.ToString(),
                    ["unityversion"] = unityVersions.First()
                });

            AnsiConsole.MarkupLine("[green bold]Successfully[/] [white bold]created disassembler script![/]");
        }

        return 0;
    }
}