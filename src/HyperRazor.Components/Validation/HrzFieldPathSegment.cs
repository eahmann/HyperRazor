namespace HyperRazor.Components.Validation;

public sealed record HrzFieldPathSegment
{
    private string _propertyName = string.Empty;
    private IReadOnlyList<int> _indices = Array.Empty<int>();

    public HrzFieldPathSegment(string propertyName, IReadOnlyList<int>? indices)
    {
        PropertyName = propertyName;
        Indices = indices ?? Array.Empty<int>();
    }

    public string PropertyName
    {
        get => _propertyName;
        init => _propertyName = value ?? throw new ArgumentNullException(nameof(value));
    }

    public IReadOnlyList<int> Indices
    {
        get => _indices;
        init => _indices = CopyIndices(value);
    }

    public void Deconstruct(out string propertyName, out IReadOnlyList<int> indices)
    {
        propertyName = PropertyName;
        indices = Indices;
    }

    private static IReadOnlyList<int> CopyIndices(IReadOnlyList<int>? indices)
    {
        if (indices is null || indices.Count == 0)
        {
            return Array.Empty<int>();
        }

        var copy = new int[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            copy[i] = indices[i];
        }

        return Array.AsReadOnly(copy);
    }
}
