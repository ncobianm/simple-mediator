using SimpleMediator.Interfaces;

namespace SimpleMediator.Sample.Behaviors;

public class TimeoutBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        // The token passed to next() is the one received by the rest of the pipeline and the handler
        return await next(timeoutCts.Token);
    }
}
