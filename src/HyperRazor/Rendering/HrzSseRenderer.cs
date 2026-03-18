using HyperRazor.Components;
using HyperRazor.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.ServerSentEvents;

namespace HyperRazor.Rendering;

public sealed class HrzSseRenderer : IHrzSseRenderer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHrzComponentHostRenderer _hostRenderer;

    public HrzSseRenderer(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider services)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        ArgumentNullException.ThrowIfNull(services);
        _hostRenderer = services.GetService<IHrzComponentHostRenderer>()
            ?? new HrzComponentHostRenderer(
                services.GetRequiredService<IHrzHtmlRendererAdapter>(),
                services.GetRequiredService<IHrzLayoutTypeResolver>(),
                services.GetRequiredService<IOptions<HrzOptions>>());
    }

    public Task<SseItem<string>> RenderComponent<TComponent>(
        object? data = null,
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return RenderComponent<TComponent>(
            HrzParameterDictionaryFactory.Create(data),
            eventType,
            id,
            retryAfter,
            cancellationToken);
    }

    public Task<SseItem<string>> RenderComponent<TComponent>(
        object? data,
        HrzSseControlEvent controlEvent,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return RenderComponent<TComponent>(
            HrzParameterDictionaryFactory.Create(data),
            controlEvent,
            id,
            retryAfter,
            cancellationToken);
    }

    public Task<SseItem<string>> RenderComponent<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        return RenderHostMessageAsync(
            componentType: typeof(TComponent),
            componentParameters: data,
            eventType: eventType,
            id: id,
            retryAfter: retryAfter,
            cancellationToken: cancellationToken);
    }

    public Task<SseItem<string>> RenderComponent<TComponent>(
        IReadOnlyDictionary<string, object?> data,
        HrzSseControlEvent controlEvent,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(data);

        return RenderComponent<TComponent>(
            data,
            controlEvent.ToEventName(),
            id,
            retryAfter,
            cancellationToken);
    }

    public Task<SseItem<string>> RenderFragments(
        string? eventType = null,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        return RenderHostMessageAsync(
            componentType: typeof(HrzFragmentGroup),
            componentParameters: new Dictionary<string, object?>
            {
                [nameof(HrzFragmentGroup.Fragments)] = fragments
            },
            eventType: eventType,
            id: id,
            retryAfter: retryAfter,
            cancellationToken: cancellationToken);
    }

    public Task<SseItem<string>> RenderFragments(
        HrzSseControlEvent controlEvent,
        string? id = null,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments)
    {
        return RenderFragments(
            controlEvent.ToEventName(),
            id,
            retryAfter,
            cancellationToken,
            fragments);
    }

    private async Task<SseItem<string>> RenderHostMessageAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> componentParameters,
        string? eventType,
        string? id,
        TimeSpan? retryAfter,
        CancellationToken cancellationToken)
    {
        var context = GetHttpContext();
        context.Items[HrzComponentContextItemKeys.ForceSwapRendering] = true;

        try
        {
            var html = await _hostRenderer.RenderIsolatedAsync(
                new HrzComponentHostRenderRequest(
                    ComponentType: componentType,
                    ComponentParameters: componentParameters,
                    LayoutType: _hostRenderer.ResolveLayoutType(componentType),
                    CurrentLayoutKey: null,
                    HttpContext: context,
                    ModelState: _hostRenderer.ResolveModelState(context),
                    IsPartial: true,
                    IsHtmxRequest: false,
                    IsHistoryRestoreRequest: false,
                    RenderHeadContent: false,
                    RenderSwapContent: true),
                cancellationToken);
            return new SseItem<string>(html, eventType)
            {
                EventId = id,
                ReconnectionInterval = retryAfter
            };
        }
        finally
        {
            context.Items.Remove(HrzComponentContextItemKeys.ForceSwapRendering);
            context.RequestServices.GetService<IHrzHeadService>()?.Clear();
            context.RequestServices.GetService<IHrzSwapService>()?.Clear();
        }
    }

    private HttpContext GetHttpContext()
    {
        return _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext is available for HyperRazor SSE rendering.");
    }
}
