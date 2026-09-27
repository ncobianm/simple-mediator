using SimpleMediator.Interfaces;
using SimpleMediator.Sample.Validation;

namespace SimpleMediator.Sample.Features;

public record CreateUser(string Email) : IRequest<Guid>;

public class CreateUserValidator : IValidator<CreateUser>
{
    public IEnumerable<string> Validate(CreateUser request)
    {
        if (!request.Email.Contains('@'))
        {
            yield return "Email must contain '@'.";
        }
    }
}

public class CreateUserHandler : IRequestHandler<CreateUser, Guid>
{
    public Task<Guid> HandleAsync(CreateUser request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}
