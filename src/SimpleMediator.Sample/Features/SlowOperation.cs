using SimpleMediator.Interfaces;

namespace SimpleMediator.Sample.Features;

public record SlowOperation(TimeSpan Duration) : IRequest<string>;

public class SlowOperationHandler : IRequestHandler<SlowOperation, string>
{
    public async Task<string> HandleAsync(SlowOperation request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(request.Duration, cancellationToken);
        return "Slow operation completed";
    }
}
