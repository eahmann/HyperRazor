namespace HyperRazor.Demo.Infrastructure;

public sealed class ProvisioningOperation
{
    public ProvisioningOperation(
        string operationId,
        WorkspaceInfo workspace,
        InviteUserInput invite,
        DateTimeOffset createdAt)
    {
        OperationId = operationId;
        WorkspaceKey = workspace.Key;
        WorkspaceName = workspace.Name;
        DisplayName = invite.DisplayName;
        Email = invite.Email;
        Team = invite.Team;
        AccessTier = invite.AccessTier;
        Manager = invite.Manager;
        StartDate = invite.StartDate ?? DateOnly.FromDateTime(DateTime.Today);
        Justification = invite.Justification;
        CreatedAt = createdAt;
    }

    public string OperationId { get; }

    public string WorkspaceKey { get; }

    public string WorkspaceName { get; }

    public string DisplayName { get; }

    public string Email { get; }

    public string Team { get; }

    public string AccessTier { get; }

    public string Manager { get; }

    public DateOnly StartDate { get; }

    public string Justification { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsCompleted { get; private set; }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
