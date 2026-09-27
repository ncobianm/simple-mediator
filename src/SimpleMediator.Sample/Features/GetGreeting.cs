using SimpleMediator.Interfaces;

namespace SimpleMediator.Sample.Features;

public record GetGreeting(string Name) : IRequest<string>;

public class GetGreetingHandler : IRequestHandler<GetGreeting, string>
{
    public Task<string> HandleAsync(GetGreeting request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
