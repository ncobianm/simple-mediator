using System.Collections.Concurrent;
using SimpleMediator.Interfaces;
using SimpleMediator.Wrappers;

namespace SimpleMediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var wrapper = WrapperCache<TResponse>.Wrappers.GetOrAdd(request.GetType(), static requestType =>
            (RequestHandlerWrapper<TResponse>)Activator.CreateInstance(
                typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(requestType, typeof(TResponse)))!);

        return wrapper.HandleAsync(request, _serviceProvider, cancellationToken);
    }

    // One cache per response type, keyed by request type. Reflection only runs the first time a request type is sent.
    private static class WrapperCache<TResponse>
    {
        public static readonly ConcurrentDictionary<Type, RequestHandlerWrapper<TResponse>> Wrappers = new();
    }
}
