namespace HyperRazor.Demo.Infrastructure;

public sealed class WorkspaceCatalog
{
    private readonly IReadOnlyDictionary<string, WorkspaceInfo> _lookup;

    public WorkspaceCatalog()
    {
        All =
        [
            new WorkspaceInfo(
                "atlas",
                "Atlas Finance",
                "Production",
                "US Central",
                "Primary finance workspace for approvals, reconciliations, and vendor access.",
                "ATLAS26",
                184,
                "Priya Shah",
                new InviteSummary(
                    "Taylor Reed",
                    "taylor.reed@example.com",
                    "Analyst",
                    "Finance Ops",
                    "Priya Shah",
                    "Provisioned",
                    "Today, 9:14 AM",
                    "success"),
                [
                    new ActivityEntry("Quarter-close access sweep completed", "Removed 14 stale reviewer grants before close.", "Today, 10:05 AM", "info"),
                    new ActivityEntry("Vendor onboarding queue trimmed", "Two pending finance invites were auto-approved after manager attestation.", "Today, 8:22 AM", "success"),
                    new ActivityEntry("Break-glass admin review due", "One privileged finance assignment needs re-attestation before noon.", "Yesterday", "warning")
                ]),
            new WorkspaceInfo(
                "northstar",
                "Northstar Support",
                "Production",
                "EU West",
                "Customer support workspace with regional queues, shifts, and escalation access.",
                "NSTAR26",
                126,
                "Alicia Moore",
                new InviteSummary(
                    "Morgan Lake",
                    "morgan.lake@example.com",
                    "Manager",
                    "Support",
                    "Alicia Moore",
                    "Provisioned",
                    "Today, 7:48 AM",
                    "success"),
                [
                    new ActivityEntry("Weekend shift roster synced", "Roster changes were pushed into access policies for the new shift rotation.", "Today, 9:41 AM", "info"),
                    new ActivityEntry("Supervisor escalation grant expired", "Temporary billing escalation access was removed on schedule.", "Today, 6:30 AM", "success"),
                    new ActivityEntry("Phone queue role drift detected", "One support identity still has last quarter's supervisor grant.", "Yesterday", "warning")
                ]),
            new WorkspaceInfo(
                "lattice",
                "Lattice Sandbox",
                "Sandbox",
                "US East",
                "Non-production workspace for playbooks, dry runs, and release rehearsals.",
                "LATTICE26",
                42,
                "Harper Nguyen",
                new InviteSummary(
                    "Jordan Avery",
                    "jordan.avery@example.com",
                    "Privileged",
                    "Platform",
                    "Harper Nguyen",
                    "Provisioned",
                    "Yesterday",
                    "success"),
                [
                    new ActivityEntry("Release rehearsal cloned", "A fresh sandbox cohort was cloned from yesterday's support data set.", "Today, 8:05 AM", "info"),
                    new ActivityEntry("Privileged drill completed", "The quarterly sandbox emergency-access playbook completed cleanly.", "Yesterday", "success"),
                    new ActivityEntry("Pending test identity cleanup", "Four stale sandbox identities are waiting for auto-expiry.", "Yesterday", "warning")
                ])
        ];

        _lookup = All.ToDictionary(workspace => workspace.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<WorkspaceInfo> All { get; }

    public WorkspaceInfo Default => All[0];

    public WorkspaceInfo Resolve(string? key)
    {
        return TryResolve(key, out var workspace)
            ? workspace
            : Default;
    }

    public bool TryResolve(string? key, out WorkspaceInfo workspace)
    {
        if (!string.IsNullOrWhiteSpace(key) && _lookup.TryGetValue(key.Trim(), out var resolvedWorkspace))
        {
            workspace = resolvedWorkspace;
            return true;
        }

        workspace = Default;
        return false;
    }

    public bool MatchesAccessCode(string workspaceKey, string? accessCode)
    {
        if (!_lookup.TryGetValue(workspaceKey, out var workspace))
        {
            return false;
        }

        return string.Equals(
            workspace.AccessCode,
            accessCode?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
