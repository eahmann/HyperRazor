using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace HyperRazor.Components.Services;

public interface IHrzSwapService
{
    event EventHandler? ContentItemsUpdated;

    bool ContentAvailable { get; }

    void QueueComponent<TComponent>(
        string targetId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent;

    void QueueComponent<TComponent>(
        string targetId,
        object? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent;

    void QueueFragment(
        string targetId,
        RenderFragment fragment,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null);

    void QueueHtml(
        string targetId,
        string html,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null);

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
