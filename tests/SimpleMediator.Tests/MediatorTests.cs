using NSubstitute;

namespace SimpleMediator.Tests;

public class MediatorTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IRequestHandler<Ping, string> _handler = Substitute.For<IRequestHandler<Ping, string>>();
    private readonly Mediator _mediator;

    public MediatorTests()
    {
        _mediator = new Mediator(_serviceProvider);
    }

    [Fact]
    public async Task SendAsync_ReturnsHandlerResponse()
    {
        RegisterHandler();
        _handler.HandleAsync(Arg.Any<Ping>(), Arg.Any<CancellationToken>()).Returns("Pong");

        var response = await _mediator.SendAsync(new Ping());

        Assert.Equal("Pong", response);
    }

    [Fact]
    public async Task SendAsync_PassesRequestAndCancellationTokenToHandler()
    {
        RegisterHandler();
        var request = new Ping();
        using var cts = new CancellationTokenSource();

        await _mediator.SendAsync(request, cts.Token);

        await _handler.Received(1).HandleAsync(request, cts.Token);
    }

    [Fact]
    public async Task SendAsync_WithoutHandler_ThrowsInvalidOperationExceptionWithRequestFullName()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.SendAsync(new Ping()));

        Assert.Contains(typeof(Ping).FullName!, exception.Message);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.SendAsync<string>(null!));
    }

    [Fact]
    public async Task SendAsync_WithNullRequestWithoutResponse_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _mediator.SendAsync((IRequest)null!));
    }

    [Fact]
    public async Task SendAsync_WithHandlerForBaseRequestType_UsesItThroughContravariance()
    {
        IRequestHandler<Ping, string> handler = new AnyStringRequestHandler();
        _serviceProvider.GetService(typeof(IRequestHandler<Ping, string>)).Returns(handler);

        var response = await _mediator.SendAsync(new Ping());

        Assert.Equal("Handled", response);
    }

    [Fact]
    public async Task SendAsync_WithBehavior_InvokesBehaviorAndHandler()
    {
        RegisterHandler();
        var behavior = PassThroughBehavior();
        RegisterBehaviors(behavior);
        var request = new Ping();
        using var cts = new CancellationTokenSource();

        await _mediator.SendAsync(request, cts.Token);

        await behavior.Received(1).HandleAsync(request, Arg.Any<RequestHandlerDelegate<string>>(), cts.Token);
        await _handler.Received(1).HandleAsync(request, cts.Token);
    }

    [Fact]
    public async Task SendAsync_WhenBehaviorPassesToken_HandlerReceivesIt()
    {
        RegisterHandler();
        using var cts = new CancellationTokenSource();
        RegisterBehaviors(TokenReplacingBehavior(cts.Token));

        await _mediator.SendAsync(new Ping());

        await _handler.Received(1).HandleAsync(Arg.Any<Ping>(), cts.Token);
    }

    [Fact]
    public async Task SendAsync_WhenInnerBehaviorOmitsToken_HandlerReceivesTokenFromOuterBehavior()
    {
        RegisterHandler();
        using var cts = new CancellationTokenSource();
        RegisterBehaviors(TokenReplacingBehavior(cts.Token), PassThroughBehavior());

        await _mediator.SendAsync(new Ping());

        await _handler.Received(1).HandleAsync(Arg.Any<Ping>(), cts.Token);
    }

    [Fact]
    public async Task SendAsync_WithMultipleBehaviors_RunsThemInRegistrationOrder()
    {
        var calls = new List<string>();
        RegisterHandler();
        _handler.HandleAsync(Arg.Any<Ping>(), Arg.Any<CancellationToken>()).Returns("Pong").AndDoes(_ => calls.Add("handler"));
        RegisterBehaviors(PassThroughBehavior(() => calls.Add("first")), PassThroughBehavior(() => calls.Add("second")));

        await _mediator.SendAsync(new Ping());

        Assert.Equal(["first", "second", "handler"], calls);
    }

    [Fact]
    public async Task SendAsync_WhenBehaviorShortCircuits_DoesNotResolveHandler()
    {
        RegisterHandler();
        var behavior = Substitute.For<IPipelineBehavior<Ping, string>>();
        behavior.HandleAsync(Arg.Any<Ping>(), Arg.Any<RequestHandlerDelegate<string>>(), Arg.Any<CancellationToken>()).Returns("Short-circuited");
        RegisterBehaviors(behavior);

        var response = await _mediator.SendAsync(new Ping());

        Assert.Equal("Short-circuited", response);
        _serviceProvider.DidNotReceive().GetService(typeof(IRequestHandler<Ping, string>));
    }

    [Fact]
    public async Task SendAsync_WithoutHandler_ExceptionFlowsThroughBehaviors()
    {
        Exception? observed = null;
        var behavior = Substitute.For<IPipelineBehavior<Ping, string>>();
        behavior.HandleAsync(Arg.Any<Ping>(), Arg.Any<RequestHandlerDelegate<string>>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                try
                {
                    return await call.Arg<RequestHandlerDelegate<string>>()();
                }
                catch (Exception ex)
                {
                    observed = ex;
                    throw;
                }
            });
        RegisterBehaviors(behavior);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _mediator.SendAsync(new Ping()));

        Assert.IsType<InvalidOperationException>(observed);
    }

    [Fact]
    public async Task SendAsync_WithRequestWithoutResponse_InvokesHandler()
    {
        _serviceProvider.GetService(typeof(IRequestHandler<Command, Unit>)).Returns(new CommandHandler());
        var command = new Command();

        await _mediator.SendAsync(command);

        Assert.True(command.Handled);
    }

    private void RegisterHandler()
    {
        _serviceProvider.GetService(typeof(IRequestHandler<Ping, string>)).Returns(_handler);
    }

    private void RegisterBehaviors(params IPipelineBehavior<Ping, string>[] behaviors)
    {
        _serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<Ping, string>>)).Returns(behaviors);
    }

    private static IPipelineBehavior<Ping, string> PassThroughBehavior(Action? onInvoke = null)
    {
        var behavior = Substitute.For<IPipelineBehavior<Ping, string>>();
        behavior.HandleAsync(Arg.Any<Ping>(), Arg.Any<RequestHandlerDelegate<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                onInvoke?.Invoke();
                return call.Arg<RequestHandlerDelegate<string>>()();
            });
        return behavior;
    }

    private static IPipelineBehavior<Ping, string> TokenReplacingBehavior(CancellationToken token)
    {
        var behavior = Substitute.For<IPipelineBehavior<Ping, string>>();
        behavior.HandleAsync(Arg.Any<Ping>(), Arg.Any<RequestHandlerDelegate<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<RequestHandlerDelegate<string>>()(token));
        return behavior;
    }
}

public record Ping : IRequest<string>;

public class Command : IRequest
{
    public bool Handled { get; set; }
}

public class CommandHandler : IRequestHandler<Command>
{
    public Task HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        request.Handled = true;
        return Task.CompletedTask;
    }
}

public class AnyStringRequestHandler : IRequestHandler<IRequest<string>, string>
{
    public Task<string> HandleAsync(IRequest<string> request, CancellationToken cancellationToken = default) => Task.FromResult("Handled");
}
