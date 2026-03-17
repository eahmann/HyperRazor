using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Demo.Infrastructure;

public sealed class InviteUserInput
{
    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [RegularExpression(@".+@example\.com$", ErrorMessage = "Use an @example.com address for the internal demo directory.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Team { get; set; } = string.Empty;

    [Required]
    public string AccessTier { get; set; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string Manager { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    [StringLength(280, MinimumLength = 12)]
    public string Justification { get; set; } = string.Empty;

    public string Workspace { get; set; } = string.Empty;

    public static InviteUserInput CreateDefault(string workspaceKey)
    {
        return new InviteUserInput
        {
            AccessTier = "Analyst",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            Workspace = workspaceKey
        };
    }
}
