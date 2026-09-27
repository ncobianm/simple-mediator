namespace SimpleMediator;

/// <summary>
/// Response type of requests without a response (<see cref="IRequest"/>).
/// </summary>
public readonly record struct Unit
{
    public static Unit Value => default;

    public override string ToString() => "()";
}
