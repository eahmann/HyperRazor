using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Demo.Mvc.Models;

public sealed class InviteUserInput
{
    [Required(ErrorMessage = "Display name is required.")]
    [MinLength(3, ErrorMessage = "Display name must be at least 3 characters.")]
    public string DisplayName { get; set; } = "Riley Stone";

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid address.")]
    public string Email { get; set; } = "riley@example.com";
}
