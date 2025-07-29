using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI;

public class ServiceTypeResolver(IServiceProvider serviceProvider) : ITypeResolver
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public object? Resolve(Type? type)
        => type == null 
            ? null 
            : _serviceProvider.GetService(type);
}