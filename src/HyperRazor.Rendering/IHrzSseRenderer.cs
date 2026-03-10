using Microsoft.AspNetCore.Components;
using System.Net.ServerSentEvents;

namespace HyperRazor.Rendering;

public interface IHrzSseRenderer
{
    Task<SseItem<string>> RenderComponent<TComponent>(
        object? data = null,
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<SseItem<string>> RenderComponent<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<SseItem<string>> RenderFragments(
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments);
}
