using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Interfaces;
using SimpleMediator.Sample.Behaviors;
using SimpleMediator.Sample.Features;
using SimpleMediator.Sample.Validation;

var services = new ServiceCollection();

// Mediator, handlers found in this assembly and pipeline behaviors (first added = outermost)
services.AddSimpleMediator(cfg => cfg
    .RegisterServicesFromAssemblyContaining<Program>()
    .AddOpenBehavior(typeof(LoggingBehavior<,>))
    .AddOpenBehavior(typeof(TimeoutBehavior<,>))
    .AddOpenBehavior(typeof(ValidationBehavior<,>)));

// Validators used by the ValidationBehavior
services.AddTransient<IValidator<CreateUser>, CreateUserValidator>();

await using var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();

// 1. Simple request/response
var greeting = await mediator.SendAsync(new GetGreeting("World"));
Console.WriteLine(greeting);

// 2. Valid request passes validation
var userId = await mediator.SendAsync(new CreateUser("user@example.com"));
Console.WriteLine($"User created: {userId}");

// 3. Invalid request is stopped by the validation behavior
try
{
    await mediator.SendAsync(new CreateUser("invalid-email"));
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}

// 4. The timeout behavior passes its own token to the rest of the pipeline and cancels slow handlers
try
{
    await mediator.SendAsync(new SlowOperation(TimeSpan.FromSeconds(5)));
}
catch (OperationCanceledException)
{
    Console.WriteLine("Slow operation cancelled by timeout");
}
