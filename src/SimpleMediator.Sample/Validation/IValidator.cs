namespace SimpleMediator.Sample.Validation;

public interface IValidator<in TRequest>
{
    IEnumerable<string> Validate(TRequest request);
}
