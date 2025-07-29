using Spectre.Console;
using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI.Commands;

internal abstract class ManualCommand<T>(PortProvider portProvider) : BaseCommand<T>(portProvider) where T : ManualCommandOptions
{
    public override ValidationResult Validate(CommandContext context, T settings)
    {
        foreach (var inputPath in settings.InputPaths)
        {
            if (!Path.Exists(inputPath))
                return ValidationResult.Error($"Provided input path {inputPath} does not exit.");
        }

        if (File.Exists(settings.OutputPath))
            return ValidationResult.Error("Provided output path already exists as a file.");

        return ValidationResult.Success();
    }
}