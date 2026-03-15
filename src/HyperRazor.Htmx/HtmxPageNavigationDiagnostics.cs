namespace HyperRazor.Htmx;

public sealed record HtmxPageNavigationDiagnostics(
    string? CurrentUrl,
    string? SourceLayout,
    string? TargetLayout,
    string Mode);
