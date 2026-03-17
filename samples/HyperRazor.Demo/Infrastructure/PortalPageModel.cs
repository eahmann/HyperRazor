namespace HyperRazor.Demo.Infrastructure;

public sealed record PortalPageModel(
    PortalEntryViewModel Form,
    IReadOnlyList<WorkspaceInfo> Workspaces);
