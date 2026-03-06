using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace HyperRazor.Components.Services;

public interface IHrzHeadService
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    void SetTitle(string title);

    void AddTitle(string title);

    void AddMeta(string name, string content, string? key = null);

    void AddLink(string href, string rel = "stylesheet", string? key = null);

    void AddScript(string src, IReadOnlyDictionary<string, object?>? attributes = null, string? key = null);

    void AddStyle(string cssText, string? key = null);

    void AddHeadFragment(RenderFragment fragment);

    void AddRawContent(string html);

    RenderFragment RenderToFragment(bool clear = false);

    void Clear();
}
