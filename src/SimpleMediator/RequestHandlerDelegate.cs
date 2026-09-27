namespace SimpleMediator;

/// <summary>
/// Invokes the next step of the pipeline. Passing a token replaces the one received by the current behavior
/// for the rest of the pipeline; omitting it (or passing <c>default</c>) keeps the current one.
/// </summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
