using Microsoft.AspNetCore.Components;

namespace HyperRazor.Components.Services;

public interface IHrzSwapService
{
    void Replace<TComponent>(
        string target,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent;

    void Replace(
        string target,
        RenderFragment fragment,
        HrzSwapOptions? options = null);

    void Append<TComponent>(
        string target,
        string itemId,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent;

    void Append(
        string target,
        string itemId,
        RenderFragment fragment,
        HrzSwapOptions? options = null);

    void Prepend<TComponent>(
        string target,
        string itemId,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent;

    void Prepend(
        string target,
        string itemId,
        RenderFragment fragment,
        HrzSwapOptions? options = null);

    Task<string> RenderToString(bool clear = false, CancellationToken cancellationToken = default);

    void Clear();
}
