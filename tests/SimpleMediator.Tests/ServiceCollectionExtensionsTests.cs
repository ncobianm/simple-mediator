using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Interfaces;

namespace SimpleMediator.Tests;

public class ServiceCollectionExtensionsTests
{
    private readonly ServiceCollection _services = new();

    [Fact]
    public async Task AddSimpleMediator_RegistersMediatorAndHandlersFromAssembly()
    {
        _services.AddSimpleMediator(cfg => cfg.RegisterServicesFromAssemblyContaining<EchoHandler>());
        using var provider = _services.BuildServiceProvider();

        var response = await provider.GetRequiredService<IMediator>().SendAsync(new Echo("Hello"));

        Assert.Equal("Hello", response);
    }

    [Fact]
    public void AddSimpleMediator_SameAssemblyTwice_RegistersHandlerOnce()
    {
        _services.AddSimpleMediator(cfg => cfg
            .RegisterServicesFromAssemblyContaining<EchoHandler>()
            .RegisterServicesFromAssemblyContaining<EchoHandler>());

        Assert.Single(_services, d => d.ServiceType == typeof(IRequestHandler<Echo, string>));
    }

    [Fact]
    public void AddSimpleMediator_WithoutAssemblies_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => _services.AddSimpleMediator(_ => { }));
    }

    [Fact]
    public void AddSimpleMediator_AppliesLifetimeToMediatorHandlersAndBehaviors()
    {
        _services.AddSimpleMediator(cfg =>
        {
            cfg.Lifetime = ServiceLifetime.Scoped;
            cfg.RegisterServicesFromAssemblyContaining<EchoHandler>().AddOpenBehavior(typeof(FirstBehavior<,>));
        });

        Assert.Equal(ServiceLifetime.Scoped, _services.Single(d => d.ServiceType == typeof(IMediator)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, _services.Single(d => d.ServiceType == typeof(IRequestHandler<Echo, string>)).Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, _services.Single(d => d.ServiceType == typeof(IPipelineBehavior<,>)).Lifetime);
    }

    [Fact]
    public void AddOpenBehavior_RegistersBehaviorsInOrder()
    {
        _services.AddSimpleMediator(cfg => cfg
            .RegisterServicesFromAssemblyContaining<EchoHandler>()
            .AddOpenBehavior(typeof(FirstBehavior<,>))
            .AddOpenBehavior(typeof(SecondBehavior<,>)));
        using var provider = _services.BuildServiceProvider();

        var behaviors = provider.GetServices<IPipelineBehavior<Echo, string>>();

        Assert.Collection(behaviors,
            b => Assert.IsType<FirstBehavior<Echo, string>>(b),
            b => Assert.IsType<SecondBehavior<Echo, string>>(b));
    }

    [Fact]
    public void AddOpenBehavior_WithTypeThatIsNotAnOpenBehavior_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _services.AddSimpleMediator(cfg => cfg.AddOpenBehavior(typeof(EchoHandler))));
    }
}

public record Echo(string Message) : IRequest<string>;

public class EchoHandler : IRequestHandler<Echo, string>
{
    public Task<string> HandleAsync(Echo request, CancellationToken cancellationToken = default) => Task.FromResult(request.Message);
}

public class FirstBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default) => next();
}

public class SecondBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default) => next();
}
