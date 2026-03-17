namespace HyperRazor.Demo.Infrastructure;

public sealed record WorkspaceInfo(
    string Key,
    string Name,
    string Environment,
    string Region,
    string Summary,
    string AccessCode,
    int SeedUserCount,
    string OperationsOwner,
    InviteSummary? SeedLatestInvite,
    IReadOnlyList<ActivityEntry> SeedActivity);
