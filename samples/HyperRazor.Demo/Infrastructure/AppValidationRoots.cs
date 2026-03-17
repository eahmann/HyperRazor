using HyperRazor.Components.Validation;

namespace HyperRazor.Demo.Infrastructure;

public static class AppValidationRoots
{
    public static HrzValidationRootId PortalEntry { get; } = new("portal-entry");

    public static HrzValidationRootId UserInvite { get; } = new("user-invite");
}
