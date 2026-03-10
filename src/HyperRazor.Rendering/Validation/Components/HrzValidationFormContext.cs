using HyperRazor.Rendering;

namespace HyperRazor.Components;

internal sealed class HrzValidationFormContext
{
    public required object Model { get; init; }

    public required HrzValidationRootId RootId { get; init; }

    public required string IdPrefix { get; init; }

    public required HrzSubmitValidationState? ValidationState { get; init; }

    public required bool EnableClientValidation { get; init; }

    public string? LiveValidationPath { get; init; }

    public string LiveTrigger { get; init; } = "input changed delay:400ms, blur";

    public string LiveInclude { get; init; } = "closest form";

    public string SummaryId => $"{IdPrefix}-server-summary";

    public string FormId => $"{IdPrefix}-form";
}
