namespace HyperRazor.Rendering;

public sealed class HrzFormContext
{
    public required object Model { get; init; }

    public required Type ModelType { get; init; }

    public required string FormName { get; init; }

    public required HrzValidationRootId RootId { get; init; }

    public required string FormId { get; init; }

    public required string SummaryId { get; init; }

    public required bool Enhance { get; init; }

    public required HrzValidationDescriptor Descriptor { get; init; }

    public required IReadOnlyDictionary<HrzFieldPath, string> FieldIds { get; init; }

    public HrzSubmitValidationState? SubmitValidationState { get; init; }
}
