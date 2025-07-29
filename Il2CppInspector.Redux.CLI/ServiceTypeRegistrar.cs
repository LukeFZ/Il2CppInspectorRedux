using Spectre.Console.Cli;

namespace Il2CppInspector.Redux.CLI;

public class ServiceTypeRegistrar(IServiceCollection serviceCollection) : ITypeRegistrar
{
    private readonly IServiceCollection _serviceCollection = serviceCollection;
    private ServiceTypeResolver? _resolver;

    public void Register(Type service, Type implementation)
    {
        _serviceCollection.AddSingleton(service, implementation);
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _serviceCollection.AddSingleton(service, implementation);
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _serviceCollection.AddSingleton(service, _ => factory());
    }

    public ITypeResolver Build()
    {
        _resolver ??= new ServiceTypeResolver(_serviceCollection.BuildServiceProvider());
        return _resolver;
    }
}