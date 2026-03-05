using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

public interface IHrxSwapService
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    void AddSwappableComponent<TComponent>(
        string targetId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent;

    void AddSwappableComponent<TComponent>(
        string targetId,
        object? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent;

    void AddSwappableFragment(
        string targetId,
        RenderFragment fragment,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null);

    void AddSwappableContent(
        string targetId,
        string html,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null);

    void AddRawContent(string html);

    RenderFragment RenderToFragment(bool clear = false);

    Task<string> RenderToString(bool clear = false, CancellationToken cancellationToken = default);

    void Clear();
}
