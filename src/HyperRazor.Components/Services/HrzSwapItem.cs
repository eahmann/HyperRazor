using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

internal enum HrzSwapItemType
{
    Swappable,
    RawHtml
}

internal sealed record HrzSwapItem(
    HrzSwapItemType Type,
    string TargetId,
    SwapStyle SwapStyle,
    string? Selector,
    RenderFragment? Fragment,
    string? RawHtml);
