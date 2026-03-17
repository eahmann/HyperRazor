namespace HyperRazor.Demo.Infrastructure;

public sealed record DirectorySearchResult(
    string WorkspaceKey,
    string Query,
    string Status,
    int TotalCount,
    IReadOnlyList<DirectoryPerson> Entries)
{
    public bool HasFilters => !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(Status);
}
