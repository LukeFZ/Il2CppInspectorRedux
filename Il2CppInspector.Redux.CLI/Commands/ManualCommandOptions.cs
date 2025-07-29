using System.ComponentModel;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal class ManualCommandOptions : CommandSettings
{
    [CommandArgument(0, "<InputPath>")]
    [Description("Paths to the input files. Will be subsequently loaded until binary and metadata were found.")]
    public string[] InputPaths { get; init; } = [];

    [CommandOption("-o|--output")]
    [Description("Path to the output folder")]
    public string OutputPath { get; init; } = "";
}