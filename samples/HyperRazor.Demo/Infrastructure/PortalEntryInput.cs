using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Demo.Infrastructure;

public sealed class PortalEntryInput
{
    [Required]
    [EmailAddress]
    [RegularExpression(@".+@example\.com$", ErrorMessage = "Use an @example.com address for the demo portal.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(24, MinimumLength = 6)]
    [RegularExpression(@"[A-Za-z0-9]{6,24}", ErrorMessage = "Use a 6-24 character letters-and-digits access code.")]
    public string AccessCode { get; set; } = string.Empty;

    [Required]
    public string Workspace { get; set; } = string.Empty;
}
