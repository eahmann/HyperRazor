using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

internal enum HrxSwapItemType
{
    Swappable,
    RawHtml
}

internal sealed record HrxSwapItem(
    HrxSwapItemType Type,
    string TargetId,
    SwapStyle SwapStyle,
    string? Selector,
    RenderFragment? Fragment,
    string? RawHtml);
