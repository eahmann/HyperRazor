namespace HyperRazor.Htmx.AspNetCore;

public sealed record HtmxLayoutPromotionDiagnostics(
    string? ClientLayoutFamily,
    string RouteLayoutFamily,
    string PromotionMode,
    bool PromotionApplied);
