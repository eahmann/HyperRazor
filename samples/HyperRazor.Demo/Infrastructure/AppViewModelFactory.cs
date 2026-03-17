using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HyperRazor.Demo.Infrastructure;

public sealed class AppViewModelFactory
{
    private static readonly IReadOnlyList<string> Teams =
    [
        "Finance Ops",
        "Support",
        "Risk",
        "Platform",
        "Vendor Access",
        "Customer Success"
    ];

    private static readonly IReadOnlyList<string> AccessTiers =
    [
        "Analyst",
        "Approver",
        "Manager",
        "Privileged"
    ];

    private readonly WorkspaceCatalog _workspaces;
    private readonly PeopleDirectoryService _directory;
    private readonly ProvisioningStore _provisioning;

    public AppViewModelFactory(
        WorkspaceCatalog workspaces,
        PeopleDirectoryService directory,
        ProvisioningStore provisioning)
    {
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));
    }

    public PortalPageModel CreatePortalPage(
        string? selectedWorkspaceKey = null,
        ModelStateDictionary? modelState = null,
        PortalEntryInput? input = null)
    {
        var selectedWorkspace = _workspaces.Resolve(selectedWorkspaceKey);
        var resolvedInput = input ?? new PortalEntryInput
        {
            Workspace = selectedWorkspace.Key
        };

        if (string.IsNullOrWhiteSpace(resolvedInput.Workspace))
        {
            resolvedInput.Workspace = selectedWorkspace.Key;
        }

        var errors = BuildErrors(modelState);
        return new PortalPageModel(
            new PortalEntryViewModel(
                resolvedInput,
                _workspaces.All,
                errors,
                BuildSummary(errors)),
            _workspaces.All);
    }

    public UsersPageModel CreateUsersPage(
        string? workspaceKey,
        ModelStateDictionary? modelState = null,
        InviteUserInput? input = null,
        string? operationId = null)
    {
        var workspace = _workspaces.Resolve(workspaceKey);
        var resolvedInput = input ?? InviteUserInput.CreateDefault(workspace.Key);
        resolvedInput.Workspace = workspace.Key;

        _provisioning.TryGetOperation(operationId, workspace.Key, out var activeOperation);
        var errors = BuildErrors(modelState);

        return new UsersPageModel(
            workspace,
            _provisioning.GetDashboard(workspace.Key),
            new InviteFormViewModel(
                resolvedInput,
                workspace,
                Teams,
                AccessTiers,
                errors,
                BuildSummary(errors)),
            _directory.Search(workspace.Key),
            activeOperation);
    }

    public BrandingPageModel CreateBrandingPage(string? workspaceKey)
    {
        var workspace = _workspaces.Resolve(workspaceKey);
        return new BrandingPageModel(workspace, _provisioning.GetDashboard(workspace.Key));
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildErrors(ModelStateDictionary? modelState)
    {
        if (modelState is null || modelState.IsValid)
        {
            return new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        }

        return modelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => NormalizeFieldKey(entry.Key),
                entry => (IReadOnlyList<string>)entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The submitted value is invalid." : error.ErrorMessage)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> BuildSummary(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        return errors
            .Values
            .SelectMany(messages => messages)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeFieldKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return string.Empty;
        }

        var separatorIndex = key.LastIndexOf('.');
        return separatorIndex >= 0
            ? key[(separatorIndex + 1)..]
            : key;
    }
}
