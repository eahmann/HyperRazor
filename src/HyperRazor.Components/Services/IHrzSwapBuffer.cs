using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

internal interface IHrzSwapBuffer
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    RenderFragment RenderToFragment(bool clear = false);
}
