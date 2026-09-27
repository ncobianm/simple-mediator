namespace SimpleMediator.Sample.Validation;

public class ValidationException(IReadOnlyList<string> errors) : Exception(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
