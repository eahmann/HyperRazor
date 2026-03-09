namespace HyperRazor.Rendering;

public sealed class HrzFieldContext
{
    public required HrzFormContext Form { get; init; }

    public required HrzFieldPath Path { get; init; }

    public required HrzFieldDescriptor Descriptor { get; init; }

    public required string HtmlName { get; init; }

    public required string HtmlId { get; init; }

    public required string MessageId { get; init; }

    public required object? CurrentValue { get; init; }
}
