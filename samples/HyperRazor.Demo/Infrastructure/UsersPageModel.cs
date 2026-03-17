namespace HyperRazor.Demo.Infrastructure;

public sealed record UsersPageModel(
    WorkspaceInfo Workspace,
    WorkspaceDashboard Dashboard,
    InviteFormViewModel Form,
    DirectorySearchResult Directory,
    ProvisioningOperation? ActiveOperation);
