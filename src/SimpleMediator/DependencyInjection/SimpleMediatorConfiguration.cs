using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Interfaces;

namespace SimpleMediator.DependencyInjection;

public class SimpleMediatorConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<Type> _openBehaviors = [];

    /// <summary>
    /// Lifetime used to register the mediator, the handlers and the behaviors. Defaults to <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;
    internal IReadOnlyList<Type> OpenBehaviors => _openBehaviors;

    public SimpleMediatorConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    public SimpleMediatorConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds an open generic behavior (e.g. <c>typeof(LoggingBehavior&lt;,&gt;)</c>) applied to every request.
    /// Behaviors run in the order they are added: the first one is the outermost.
    /// </summary>
    public SimpleMediatorConfiguration AddOpenBehavior(Type openBehaviorType)
    {
        var implementsBehavior = openBehaviorType.IsGenericTypeDefinition &&
                                 openBehaviorType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
        if (!implementsBehavior)
        {
            throw new ArgumentException($"{openBehaviorType.FullName} must be an open generic type implementing {typeof(IPipelineBehavior<,>).Name}.", nameof(openBehaviorType));
        }

        _openBehaviors.Add(openBehaviorType);
        return this;
    }
}
