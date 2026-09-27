using SimpleMediator.Interfaces;
using SimpleMediator.Sample.Validation;

namespace SimpleMediator.Sample.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        var errors = validators.SelectMany(v => v.Validate(request)).ToList();
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return next();
    }
}
