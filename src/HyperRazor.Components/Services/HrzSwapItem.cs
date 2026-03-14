using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

internal sealed record HrzSwapItem(
    string TargetId,
    string SwapDescriptor,
    RenderFragment Fragment);
