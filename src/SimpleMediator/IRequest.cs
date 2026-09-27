namespace SimpleMediator;

public interface IRequest<TResponse> { }

/// <summary>
/// A request without a response. Internally it is handled as a request returning <see cref="Unit"/>,
/// so pipeline behaviors also apply to it.
/// </summary>
public interface IRequest : IRequest<Unit> { }
