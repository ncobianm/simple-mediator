using SimpleMediator.Interfaces;

namespace SimpleMediator.Wrappers;

internal abstract class RequestHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> HandleAsync(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        // Resolve handler
        var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) as IRequestHandler<TRequest, TResponse> ??
                      throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

        // Build innermost delegate (the handler)
        RequestHandlerDelegate<TResponse> pipeline = () => handler.HandleAsync(typedRequest, cancellationToken);

        // Resolve pipeline behaviors
        if (serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>)) is IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
        {
            // Wrap each behavior around the pipeline (reverse so first-registered = outermost)
            foreach (var behavior in behaviors.Reverse())
            {
                var next = pipeline;
                pipeline = () => behavior.HandleAsync(typedRequest, next, cancellationToken);
            }
        }

        return pipeline();
    }
}
