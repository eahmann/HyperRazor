namespace HyperRazor.Components.Validation;

public sealed record HrzValidationRootId(string Value)
{
    public override string ToString() => Value;
}
