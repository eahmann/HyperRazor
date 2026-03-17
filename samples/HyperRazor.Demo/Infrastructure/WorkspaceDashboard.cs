namespace HyperRazor.Demo.Infrastructure;

public sealed record WorkspaceDashboard(
    int UserCount,
    InviteSummary? LatestInvite,
    IReadOnlyList<ActivityEntry> Activity);
