using HyperRazor.Components.Validation;

namespace HyperRazor.Demo.Mvc.Models;

public sealed class MixedValidationFormViewModel
{
    public required HrzValidationRootId RootId { get; init; }

    public required string IdPrefix { get; init; }

    public required string PanelTitle { get; init; }

    public required string Action { get; init; }

    public string HxPost { get; init; } = string.Empty;

    public string PanelDescription { get; init; } =
        "A mixed-input validation surface using textarea, select, checkbox, and number fields.";

    public string Instructions { get; init; } =
        "Switch to Production, set Seat Count above 10, and toggle Requires Approval to watch non-text live validation update the dependent server slot.";

    public string ButtonText { get; init; } = "Validate Mixed Form";

    public string? LiveValidationPath { get; init; }

    public bool EnableClientValidation { get; init; }

    public MixedValidationInput Input { get; init; } = new();

    public bool Success { get; init; }
}
