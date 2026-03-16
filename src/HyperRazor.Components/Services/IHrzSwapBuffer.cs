using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

public interface IHrzSwapBuffer
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    RenderFragment RenderToFragment(bool clear = false);
}
