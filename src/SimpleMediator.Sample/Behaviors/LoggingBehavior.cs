namespace SimpleMediator.Sample.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Log] Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[Log] Handled {typeof(TRequest).Name} -> {response}");
        return response;
    }
}
