using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Models;

public static class MixedValidationDefinitions
{
    public static MixedValidationFormViewModel Authoring(
        MixedValidationInput? input = null,
        bool success = false)
    {
        return new MixedValidationFormViewModel
        {
            RootId = UserInviteValidationRoots.MixedAuthoring,
            IdPrefix = "validation-mixed-authoring",
            PanelTitle = "Mixed Input Authoring Surface",
            Action = "/validation/mixed",
            HxPost = "/validation/mixed",
            PanelDescription = "Exercise the shared authoring surface across text area, select, checkbox, and number inputs.",
            Instructions = "Use Production with Seat Count above 10 to trigger server validation, then toggle Requires Approval to clear the dependent server state out of band.",
            ButtonText = "Validate Mixed Surface",
            LiveValidationPath = "/validation/mixed/live",
            EnableClientValidation = true,
            Input = input ?? new MixedValidationInput(),
            Success = success
        };
    }

    public static bool TryResolve(
        HrzValidationRootId rootId,
        MixedValidationInput? input,
        out MixedValidationFormViewModel form)
    {
        ArgumentNullException.ThrowIfNull(rootId);

        form = rootId.Value switch
        {
            "validation-mixed-authoring" => Authoring(input),
            _ => null!
        };

        return form is not null;
    }
}
