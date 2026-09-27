using Microsoft.Extensions.DependencyInjection.Extensions;
using SimpleMediator;
using SimpleMediator.DependencyInjection;
using SimpleMediator.Interfaces;

// Same namespace as IServiceCollection so the extension is available without extra usings
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleMediator(this IServiceCollection services, Action<SimpleMediatorConfiguration> configure)
    {
        var configuration = new SimpleMediatorConfiguration();
        configure(configuration);

        if (configuration.Assemblies.Count == 0)
        {
            throw new InvalidOperationException($"No assemblies to scan. Call {nameof(SimpleMediatorConfiguration.RegisterServicesFromAssembly)} at least once.");
        }

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), configuration.Lifetime));

        foreach (var (serviceType, implementationType) in FindHandlers(configuration))
        {
            services.Add(new ServiceDescriptor(serviceType, implementationType, configuration.Lifetime));
        }

        foreach (var behaviorType in configuration.OpenBehaviors)
        {
            services.TryAddEnumerable(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, configuration.Lifetime));
        }

        return services;
    }

    private static IEnumerable<(Type ServiceType, Type ImplementationType)> FindHandlers(SimpleMediatorConfiguration configuration) =>
        from type in configuration.Assemblies.SelectMany(a => a.GetTypes())
        where type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
        from handlerInterface in type.GetInterfaces()
        where handlerInterface.IsGenericType && handlerInterface.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
        select (handlerInterface, type);
}
