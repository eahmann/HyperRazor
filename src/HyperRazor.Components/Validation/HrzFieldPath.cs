namespace HyperRazor.Components.Validation;

public sealed class HrzFieldPath : IEquatable<HrzFieldPath>
{
    public string Value { get; }

    internal HrzFieldPath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public bool Equals(HrzFieldPath? other) =>
        other is not null
        && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) =>
        obj is HrzFieldPath other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;
}
