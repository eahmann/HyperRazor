using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Models;

public static class UserInviteValidationRoots
{
    public static HrzValidationRootId MvcLocal { get; } = new("users-invite");
    public static HrzValidationRootId MvcProxy { get; } = new("validation-mvc-proxy");
    public static HrzValidationRootId MinimalLocal { get; } = new("validation-minimal-local");
    public static HrzValidationRootId MinimalProxy { get; } = new("validation-minimal-proxy");
}
