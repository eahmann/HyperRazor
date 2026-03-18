namespace HyperRazor.Rendering;

internal sealed record HrzPageNavigationPlan(
    string TargetLayoutKey,
    string? CurrentLayoutKey,
    HrzPageNavigationResponseMode Mode)
{
    public bool? RenderHeadContent => Mode == HrzPageNavigationResponseMode.RootSwap ? true : null;

    public bool? RenderSwapContent => Mode == HrzPageNavigationResponseMode.RootSwap ? true : null;
}
