namespace HyperRazor.Rendering;

public sealed class HrzValidationDescriptor
{
    public required Type ModelType { get; init; }

    public required IReadOnlyDictionary<HrzFieldPath, HrzFieldDescriptor> Fields { get; init; }
}

public sealed class HrzFieldDescriptor
{
    public required HrzFieldPath Path { get; init; }

    public required string HtmlName { get; init; }

    public string? DisplayName { get; init; }

    public IReadOnlyDictionary<string, string> LocalRules { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public HrzLiveRuleDescriptor? LiveRule { get; init; }
}

public sealed class HrzLiveRuleDescriptor
{
    public required string Endpoint { get; init; }

    public IReadOnlyList<HrzFieldPath> AdditionalFields { get; init; } = Array.Empty<HrzFieldPath>();

    public string Trigger { get; init; } = "input changed delay:400ms, blur";
}
