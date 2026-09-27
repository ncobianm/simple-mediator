namespace SimpleMediator.Sample.Features;

public record DeleteUser(Guid UserId) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUser>
{
    public Task HandleAsync(DeleteUser request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"User deleted: {request.UserId}");
        return Task.CompletedTask;
    }
}
