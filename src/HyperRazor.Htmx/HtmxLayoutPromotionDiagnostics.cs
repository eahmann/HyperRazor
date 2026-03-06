namespace HyperRazor.Htmx;

public sealed record HtmxLayoutPromotionDiagnostics(
    string? ClientLayoutFamily,
    string RouteLayoutFamily,
    string PromotionMode,
    bool PromotionApplied);
