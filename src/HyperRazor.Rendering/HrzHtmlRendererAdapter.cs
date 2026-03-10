using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Rendering;

public sealed class HrzHtmlRendererAdapter : IHrzHtmlRendererAdapter, IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;

    public HrzHtmlRendererAdapter(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public Task<string> RenderAsync(
        Type componentType,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(parameters);

        var renderer = new HtmlRenderer(_services, _loggerFactory);

        return renderer.Dispatcher.InvokeAsync(async () =>
        {
            await using (renderer)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rendered = await renderer.RenderComponentAsync(
                    componentType,
                    ParameterView.FromDictionary(ToMutableDictionary(parameters)));

                return rendered.ToHtmlString();
            }
        });
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
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
