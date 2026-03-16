using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Demo.Mvc.Models;

public sealed class MixedValidationInput
{
    [Required(ErrorMessage = "Environment is required.")]
    public string Environment { get; set; } = "sandbox";

    public bool RequiresApproval { get; set; }

    [Range(1, 50, ErrorMessage = "Seat count must be between 1 and 50.")]
    public int SeatCount { get; set; } = 5;

    [Required(ErrorMessage = "Notes are required.")]
    [MinLength(10, ErrorMessage = "Notes must be at least 10 characters.")]
    public string Notes { get; set; } = "Requesting a staged rollout.";
}
