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

        // Build innermost delegate (the handler). The handler is resolved lazily so behaviors that
        // short-circuit avoid constructing it, and a missing handler error flows through the behaviors.
        RequestHandlerDelegate<TResponse> pipeline = token =>
        {
            var handler = serviceProvider.GetService(typeof(IRequestHandler<TRequest, TResponse>)) as IRequestHandler<TRequest, TResponse> ??
                          throw new InvalidOperationException($"No handler registered for {typeof(TRequest).Name}");

            return handler.HandleAsync(typedRequest, token);
        };

        // Resolve pipeline behaviors
        if (serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>)) is IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
        {
            // Wrap each behavior around the pipeline (reverse so first-registered = outermost)
            foreach (var behavior in behaviors.Reverse())
            {
                var next = pipeline;
                // If the behavior calls next() without a token, keep the one it received
                pipeline = token => behavior.HandleAsync(typedRequest, t => next(t == default ? token : t), token);
            }
        }

        return pipeline(cancellationToken);
    }
}
