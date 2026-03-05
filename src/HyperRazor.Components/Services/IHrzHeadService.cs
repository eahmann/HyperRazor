using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

public interface IHrzHeadService
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    void AddTitle(string title);

    void AddMeta(string name, string content);

    void AddLink(string href, string rel = "stylesheet");

    void AddHeadFragment(RenderFragment fragment);

    void AddRawContent(string html);

    RenderFragment RenderToFragment(bool clear = false);

    void Clear();
}
