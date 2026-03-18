namespace HyperRazor.Components.Validation;

public sealed record HrzFieldPathSegment(
    string PropertyName,
    IReadOnlyList<int> Indices);
