namespace HyperRazor.Demo.Infrastructure;

public sealed class PeopleDirectoryService
{
    private static readonly IReadOnlyList<DirectoryPerson> Directory =
    [
        new("atlas", "Maya Brooks", "maya.brooks@example.com", "Finance Ops", "Workspace Admin", "Active", "2 minutes ago"),
        new("atlas", "Noah Patel", "noah.patel@example.com", "Risk", "Analyst", "Active", "14 minutes ago"),
        new("atlas", "Riley Chen", "riley.chen@example.com", "Treasury", "Approver", "Invited", "1 hour ago"),
        new("atlas", "Jordan Cruz", "jordan.cruz@example.com", "Vendor Access", "Reviewer", "Active", "Today, 8:42 AM"),
        new("atlas", "Sofia Turner", "sofia.turner@example.com", "Finance Ops", "Analyst", "Needs Review", "Yesterday"),
        new("northstar", "Avery Collins", "avery.collins@example.com", "Customer Success", "Manager", "Active", "5 minutes ago"),
        new("northstar", "Leo Martinez", "leo.martinez@example.com", "Support", "Analyst", "Active", "26 minutes ago"),
        new("northstar", "Iris Kim", "iris.kim@example.com", "Field Ops", "Approver", "Invited", "Today, 9:18 AM"),
        new("northstar", "Ethan Ward", "ethan.ward@example.com", "Operations", "Coordinator", "Suspended", "Yesterday"),
        new("northstar", "Zoe Ramirez", "zoe.ramirez@example.com", "Support", "Escalation Lead", "Active", "2 hours ago"),
        new("lattice", "Harper Nguyen", "harper.nguyen@example.com", "Platform", "Sandbox Owner", "Active", "just now"),
        new("lattice", "Miles Carter", "miles.carter@example.com", "QA", "Analyst", "Active", "17 minutes ago"),
        new("lattice", "Nina Shah", "nina.shah@example.com", "Enablement", "Trainer", "Invited", "Today, 7:55 AM"),
        new("lattice", "Owen Foster", "owen.foster@example.com", "Platform", "Privileged", "Needs Review", "Yesterday"),
        new("lattice", "Lena Park", "lena.park@example.com", "QA", "Analyst", "Active", "Today, 10:02 AM")
    ];

    public DirectorySearchResult Search(string workspaceKey, string? query = null, string? status = null)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        var normalizedStatus = status?.Trim() ?? string.Empty;

        IEnumerable<DirectoryPerson> entries = Directory.Where(person =>
            string.Equals(person.WorkspaceKey, workspaceKey, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            entries = entries.Where(person =>
                person.DisplayName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || person.Email.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || person.Team.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                || person.Role.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            entries = entries.Where(person =>
                string.Equals(person.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase));
        }

        var materialized = entries
            .OrderBy(person => person.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        return new DirectorySearchResult(
            workspaceKey,
            normalizedQuery,
            normalizedStatus,
            materialized.Length,
            materialized);
    }
}
