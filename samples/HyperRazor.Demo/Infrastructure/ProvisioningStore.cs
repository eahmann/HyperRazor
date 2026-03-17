using System.Collections.Concurrent;

namespace HyperRazor.Demo.Infrastructure;

public sealed class ProvisioningStore
{
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<string, ProvisioningOperation> _operations =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WorkspaceState> _workspaceState;
    private int _sequence = 1040;

    public ProvisioningStore(WorkspaceCatalog workspaces)
    {
        ArgumentNullException.ThrowIfNull(workspaces);

        _workspaceState = workspaces.All.ToDictionary(
            workspace => workspace.Key,
            workspace => WorkspaceState.Create(workspace),
            StringComparer.OrdinalIgnoreCase);
    }

    public ProvisioningOperation StartOperation(WorkspaceInfo workspace, InviteUserInput input)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(input);

        var operation = new ProvisioningOperation(
            $"op-{Interlocked.Increment(ref _sequence):D4}",
            workspace,
            input,
            DateTimeOffset.UtcNow);

        _operations[operation.OperationId] = operation;
        return operation;
    }

    public WorkspaceDashboard GetDashboard(string workspaceKey)
    {
        lock (_gate)
        {
            return _workspaceState[workspaceKey].Snapshot();
        }
    }

    public bool TryGetOperation(string? operationId, string workspaceKey, out ProvisioningOperation? operation)
    {
        operation = null;

        if (string.IsNullOrWhiteSpace(operationId))
        {
            return false;
        }

        if (!_operations.TryGetValue(operationId, out var stored))
        {
            return false;
        }

        if (!string.Equals(stored.WorkspaceKey, workspaceKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        operation = stored;
        return true;
    }

    public WorkspaceDashboard CompleteOperation(string operationId)
    {
        lock (_gate)
        {
            if (!_operations.TryGetValue(operationId, out var operation))
            {
                throw new InvalidOperationException($"Unknown provisioning operation '{operationId}'.");
            }

            var state = _workspaceState[operation.WorkspaceKey];
            if (!operation.IsCompleted)
            {
                operation.MarkCompleted();

                var now = DateTimeOffset.UtcNow;
                state.UserCount += 1;
                state.LatestInvite = new InviteSummary(
                    operation.DisplayName,
                    operation.Email,
                    operation.AccessTier,
                    operation.Team,
                    operation.Manager,
                    "Provisioned",
                    FormatStamp(now),
                    "success");

                state.Activity.Insert(0, new ActivityEntry(
                    $"Provisioned {operation.DisplayName}",
                    $"{operation.AccessTier} access is active under {operation.Manager} in {operation.Team}.",
                    FormatStamp(now),
                    "success"));

                if (state.Activity.Count > 6)
                {
                    state.Activity.RemoveRange(6, state.Activity.Count - 6);
                }
            }

            return state.Snapshot();
        }
    }

    private static string FormatStamp(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("MMM d, h:mm tt");
    }

    private sealed class WorkspaceState
    {
        public required int UserCount { get; set; }

        public required InviteSummary? LatestInvite { get; set; }

        public required List<ActivityEntry> Activity { get; init; }

        public static WorkspaceState Create(WorkspaceInfo workspace)
        {
            return new WorkspaceState
            {
                UserCount = workspace.SeedUserCount,
                LatestInvite = workspace.SeedLatestInvite,
                Activity = workspace.SeedActivity.ToList()
            };
        }

        public WorkspaceDashboard Snapshot()
        {
            return new WorkspaceDashboard(
                UserCount,
                LatestInvite,
                Activity.ToArray());
        }
    }
}
