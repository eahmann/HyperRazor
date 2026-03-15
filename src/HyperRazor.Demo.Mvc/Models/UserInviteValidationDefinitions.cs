using HyperRazor.Components.Validation;

namespace HyperRazor.Demo.Mvc.Models;

public static class UserInviteValidationDefinitions
{
    public static InviteValidationFormViewModel MvcLocal(
        InviteUserInput? input = null,
        bool success = false,
        int count = 0)
    {
        return new InviteValidationFormViewModel
        {
            RootId = UserInviteValidationRoots.MvcLocal,
            IdPrefix = "validation",
            PanelTitle = "Invite Validation (MVC)",
            Action = "/users/invite",
            HxPost = "/users/invite",
            ButtonText = "Validate Invite",
            Input = input ?? new InviteUserInput(),
            Success = success,
            Count = count
        };
    }

    public static InviteValidationFormViewModel MvcProxy(
        InviteUserInput? input = null,
        bool success = false,
        int count = 0)
    {
        return new InviteValidationFormViewModel
        {
            RootId = UserInviteValidationRoots.MvcProxy,
            IdPrefix = "validation-mvc-proxy",
            PanelTitle = "MVC Backend Proxy",
            Action = "/validation/mvc-proxy",
            HxPost = "/validation/mvc-proxy",
            PanelDescription = "MVC validates local binding first, then hands off to a backend policy check.",
            ButtonText = "Validate Via Backend",
            Instructions = "The MVC action validates local binding first, then maps backend 422 JSON back into HTML.",
            Input = input ?? new InviteUserInput(),
            Success = success,
            Count = count
        };
    }

    public static InviteValidationFormViewModel MinimalLocal(
        InviteUserInput? input = null,
        bool success = false,
        int count = 0)
    {
        return new InviteValidationFormViewModel
        {
            RootId = UserInviteValidationRoots.MinimalLocal,
            IdPrefix = "validation-minimal-local",
            PanelTitle = "Minimal API Local Validation",
            Action = "/validation/minimal/local",
            HxPost = "/validation/minimal/local",
            PanelDescription = "Minimal API binds and validates the form locally, then rerenders HTML without a controller.",
            ButtonText = "Validate Via Minimal API",
            Instructions = "This route binds and validates form data through the Minimal API helper path.",
            Input = input ?? new InviteUserInput(),
            Success = success,
            Count = count
        };
    }

    public static InviteValidationFormViewModel MinimalProxy(
        InviteUserInput? input = null,
        bool success = false,
        int count = 0)
    {
        return new InviteValidationFormViewModel
        {
            RootId = UserInviteValidationRoots.MinimalProxy,
            IdPrefix = "validation-minimal-proxy",
            PanelTitle = "Minimal API Backend Proxy",
            Action = "/validation/minimal/proxy",
            HxPost = "/validation/minimal/proxy",
            PanelDescription = "Minimal API short-circuits local invalid input and only calls the backend when local binding succeeds.",
            ButtonText = "Validate Minimal + Backend",
            Instructions = "Local client checks run immediately. Use backend-taken@example.com for email-only live server feedback, then try shared-mailbox@example.com with and without a team display name to see dependent-field OOB updates before submit.",
            LiveValidationPath = "/validation/live",
            EnableClientValidation = true,
            Input = input ?? new InviteUserInput(),
            Success = success,
            Count = count
        };
    }

    public static bool TryResolve(
        HrzValidationRootId rootId,
        InviteUserInput? input,
        out InviteValidationFormViewModel form)
    {
        ArgumentNullException.ThrowIfNull(rootId);

        form = rootId.Value switch
        {
            "users-invite" => MvcLocal(input),
            "validation-mvc-proxy" => MvcProxy(input),
            "validation-minimal-local" => MinimalLocal(input),
            "validation-minimal-proxy" => MinimalProxy(input),
            _ => null!
        };

        return form is not null;
    }
}
