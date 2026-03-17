namespace HyperRazor.Demo.Infrastructure;

public sealed record DirectoryPerson(
    string WorkspaceKey,
    string DisplayName,
    string Email,
    string Team,
    string Role,
    string Status,
    string LastSeen);
