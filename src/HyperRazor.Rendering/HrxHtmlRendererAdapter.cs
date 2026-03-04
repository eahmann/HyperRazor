using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Rendering;

public sealed class HrxHtmlRendererAdapter : IHrxHtmlRendererAdapter, IAsyncDisposable
{
    private readonly HtmlRenderer _renderer;

    public HrxHtmlRendererAdapter(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _renderer = new HtmlRenderer(services, loggerFactory);
    }

    public Task<string> RenderAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        return _renderer.Dispatcher.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rendered = await _renderer.RenderComponentAsync(
                componentType,
                ParameterView.FromDictionary(ToMutableDictionary(parameters)));

            return rendered.ToHtmlString();
        });
    }

    public ValueTask DisposeAsync()
    {
        return _renderer.DisposeAsync();
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
}
