namespace SimpleMediator;

public interface IRequestHandler<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for a request without a response. It is resolved as <c>IRequestHandler&lt;TRequest, Unit&gt;</c>,
/// so it must be registered under that service type (<c>AddSimpleMediator</c> does it automatically).
/// </summary>
public interface IRequestHandler<TRequest> : IRequestHandler<TRequest, Unit> where TRequest : IRequest<Unit>
{
    new Task HandleAsync(TRequest request, CancellationToken cancellationToken = default);

    async Task<Unit> IRequestHandler<TRequest, Unit>.HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        await HandleAsync(request, cancellationToken);
        return Unit.Value;
    }
}
