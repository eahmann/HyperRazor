using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Rendering;

public sealed class HrzHtmlRendererAdapter : IHrzHtmlRendererAdapter, IAsyncDisposable
{
    private readonly HtmlRenderer _renderer;
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;

    public HrzHtmlRendererAdapter(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _renderer = new HtmlRenderer(_services, _loggerFactory);
    }

    public Task<string> RenderAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        return RenderWithRendererAsync(_renderer, componentType, parameters, disposeAfterRender: false, cancellationToken);
    }

    public Task<string> RenderIsolatedAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        var renderer = new HtmlRenderer(_services, _loggerFactory);
        return RenderWithRendererAsync(renderer, componentType, parameters, disposeAfterRender: true, cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _renderer.DisposeAsync();
    }

    private static Task<string> RenderWithRendererAsync(
        HtmlRenderer renderer,
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        bool disposeAfterRender,
        CancellationToken cancellationToken)
    {
        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            if (disposeAfterRender)
            {
                await using (renderer)
                {
                    return await RenderCoreAsync(renderer, componentType, parameters, cancellationToken);
                }
            }

            return await RenderCoreAsync(renderer, componentType, parameters, cancellationToken);
        });
    }

    private static Dictionary<string, object?> ToMutableDictionary(IReadOnlyDictionary<string, object?> parameters)
    {
        var mutable = new Dictionary<string, object?>(parameters.Count, StringComparer.Ordinal);
        foreach (var entry in parameters)
        {
            mutable[entry.Key] = entry.Value;
        }

        return mutable;
    }

    private static async Task<string> RenderCoreAsync(
        HtmlRenderer renderer,
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rendered = await renderer.RenderComponentAsync(
            componentType,
            ParameterView.FromDictionary(ToMutableDictionary(parameters)));

        return rendered.ToHtmlString();
    }
}
