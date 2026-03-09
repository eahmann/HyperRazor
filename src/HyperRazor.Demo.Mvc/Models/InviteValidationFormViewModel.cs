using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Models;

public sealed class InviteValidationFormViewModel
{
    public required HrzValidationRootId RootId { get; init; }

    public required string IdPrefix { get; init; }

    public required string PanelTitle { get; init; }

    public required string Action { get; init; }

    public string HxPost { get; init; } = string.Empty;

    public string PanelDescription { get; init; } = "Try invalid input first, then send a valid invite.";

    public string Instructions { get; init; } =
        "Submit invalid input first, then valid input. Invalid responses rerender the form fragment with server field errors.";

    public string ButtonText { get; init; } = "Validate Invite";

    public string SuccessVerb { get; init; } = "Created";

    public string? LiveValidationPath { get; init; }

    public bool EnableClientValidation { get; init; }

    public InviteUserInput Input { get; init; } = new();

    public bool Success { get; init; }

    public int Count { get; init; }
}
