namespace HyperRazor.Demo.Infrastructure;

public sealed record InviteSummary(
    string DisplayName,
    string Email,
    string AccessTier,
    string Team,
    string Manager,
    string Status,
    string Stamp,
    string Tone);
