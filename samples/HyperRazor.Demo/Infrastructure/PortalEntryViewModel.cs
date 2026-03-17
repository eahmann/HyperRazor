namespace HyperRazor.Demo.Infrastructure;

public sealed record PortalEntryViewModel(
    PortalEntryInput Input,
    IReadOnlyList<WorkspaceInfo> Workspaces,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors,
    IReadOnlyList<string> SummaryErrors)
{
    public bool HasErrors => SummaryErrors.Count > 0;

    public bool HasError(string key)
    {
        return Errors.ContainsKey(key);
    }

    public IReadOnlyList<string> GetErrors(string key)
    {
        return Errors.TryGetValue(key, out var messages)
            ? messages
            : Array.Empty<string>();
    }
}
