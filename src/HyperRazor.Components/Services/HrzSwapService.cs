using HyperRazor.Components;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace HyperRazor.Components.Services;

#pragma warning disable ASP0006

public sealed class HrzSwapService : IHrzSwapService
{
    private const string ForceSwapRenderingItemKey = "HyperRazor.Rendering.ForceSwapRendering";

    private static readonly IReadOnlyDictionary<string, object?> EmptyParameters =
        new Dictionary<string, object?>(0, StringComparer.Ordinal);

    private readonly List<HrzSwapItem> _items = [];
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HrzSwapOptions _options;
    private readonly IServiceProvider? _services;
    private readonly ILoggerFactory? _loggerFactory;

    public HrzSwapService(IHttpContextAccessor httpContextAccessor, IOptions<HrzSwapOptions> options)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _services = null;
        _loggerFactory = null;
    }

    public HrzSwapService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<HrzSwapOptions> options,
        IServiceProvider services,
        ILoggerFactory loggerFactory)
        : this(httpContextAccessor, options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public event EventHandler? ContentItemsUpdated;

    public bool ContentAvailable => _items.Count > 0;

    public void QueueComponent<TComponent>(
        string targetId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent
    {
        AddSwappableComponent<TComponent>(targetId, parameters, swapStyle, selector);
    }

    public void QueueComponent<TComponent>(
        string targetId,
        object? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent
    {
        AddSwappableComponent<TComponent>(targetId, parameters, swapStyle, selector);
    }

    public void QueueFragment(
        string targetId,
        RenderFragment fragment,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
    {
        AddSwappableFragment(targetId, fragment, swapStyle, selector);
    }

    public void QueueHtml(
        string targetId,
        string html,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
    {
        AddSwappableContent(targetId, html, swapStyle, selector);
    }

    public void AddSwappableComponent<TComponent>(
        string targetId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent
    {
        EnsureTargetId(targetId);

        var normalizedParameters = parameters ?? EmptyParameters;
        var fragment = BuildComponentFragment<TComponent>(normalizedParameters);
        _items.Add(new HrzSwapItem(
            Type: HrzSwapItemType.Swappable,
            TargetId: targetId,
            SwapStyle: swapStyle,
            Selector: NormalizeSelector(selector),
            Fragment: fragment,
            RawHtml: null));
        NotifyContentItemsUpdated();
    }

    public void AddSwappableComponent<TComponent>(
        string targetId,
        object? parameters = null,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
        where TComponent : IComponent
    {
        AddSwappableComponent<TComponent>(
            targetId,
            CreateParameterDictionary(parameters),
            swapStyle,
            selector);
    }

    public void AddSwappableFragment(
        string targetId,
        RenderFragment fragment,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
    {
        EnsureTargetId(targetId);
        ArgumentNullException.ThrowIfNull(fragment);

        _items.Add(new HrzSwapItem(
            Type: HrzSwapItemType.Swappable,
            TargetId: targetId,
            SwapStyle: swapStyle,
            Selector: NormalizeSelector(selector),
            Fragment: fragment,
            RawHtml: null));
        NotifyContentItemsUpdated();
    }

    public void AddSwappableContent(
        string targetId,
        string html,
        SwapStyle swapStyle = SwapStyle.OuterHtml,
        string? selector = null)
    {
        EnsureTargetId(targetId);

        var content = html ?? string.Empty;
        RenderFragment fragment = builder => builder.AddMarkupContent(0, content);

        _items.Add(new HrzSwapItem(
            Type: HrzSwapItemType.Swappable,
            TargetId: targetId,
            SwapStyle: swapStyle,
            Selector: NormalizeSelector(selector),
            Fragment: fragment,
            RawHtml: null));
        NotifyContentItemsUpdated();
    }

    public void AddRawContent(string html)
    {
        _items.Add(new HrzSwapItem(
            Type: HrzSwapItemType.RawHtml,
            TargetId: string.Empty,
            SwapStyle: SwapStyle.None,
            Selector: null,
            Fragment: null,
            RawHtml: html ?? string.Empty));
        NotifyContentItemsUpdated();
    }

    public RenderFragment RenderToFragment(bool clear = false)
    {
        var request = GetCurrentRequest();
        var includeSwappables = ShouldForceSwapRendering() || (request.IsHtmx && !request.IsHistoryRestoreRequest);
        var includeRaw = includeSwappables || _options.AllowRawContentOnNonHtmx;

        var snapshot = _items
            .Where(item => item.Type == HrzSwapItemType.Swappable ? includeSwappables : includeRaw)
            .ToArray();

        if (clear)
        {
            ClearInternal(notify: false);
        }

        return builder =>
        {
            var sequence = 0;

            foreach (var item in snapshot)
            {
                if (item.Type == HrzSwapItemType.RawHtml)
                {
                    builder.AddMarkupContent(sequence++, item.RawHtml ?? string.Empty);
                    continue;
                }

                builder.OpenComponent<HrzSwappable>(sequence++);
                builder.AddAttribute(sequence++, nameof(HrzSwappable.TargetId), item.TargetId);
                builder.AddAttribute(sequence++, nameof(HrzSwappable.SwapStyle), item.SwapStyle);

                if (!string.IsNullOrWhiteSpace(item.Selector))
                {
                    builder.AddAttribute(sequence++, nameof(HrzSwappable.Selector), item.Selector);
                }

                builder.AddAttribute(sequence++, nameof(HrzSwappable.ChildContent), item.Fragment);
                builder.CloseComponent();
            }
        };
    }

    private bool ShouldForceSwapRendering()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return false;
        }

        return context.Items.TryGetValue(ForceSwapRenderingItemKey, out var value)
            && value is true;
    }

    public async Task<string> RenderToString(bool clear = false, CancellationToken cancellationToken = default)
    {
        if (_services is null || _loggerFactory is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RenderToString)} requires {nameof(IServiceProvider)} and {nameof(ILoggerFactory)} to be available.");
        }

        var fragment = RenderToFragment(clear: clear);

        var parameters = new Dictionary<string, object?>
        {
            [nameof(HrzFragmentGroup.Fragments)] = new RenderFragment[] { fragment }
        };

        await using var renderer = new HtmlRenderer(_services, _loggerFactory);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rendered = await renderer.RenderComponentAsync<HrzFragmentGroup>(
                ParameterView.FromDictionary(parameters));
            return rendered.ToHtmlString();
        });
    }

    public void Clear()
    {
        ClearInternal(notify: true);
    }

    private HtmxRequest GetCurrentRequest()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return new HtmxRequest();
        }

        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        return HtmxRequest.FromHeaders(headers);
    }

    private static RenderFragment BuildComponentFragment<TComponent>(IReadOnlyDictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        return builder =>
        {
            builder.OpenComponent<TComponent>(0);

            var sequence = 1;
            foreach (var entry in parameters)
            {
                builder.AddAttribute(sequence++, entry.Key, entry.Value);
            }

            builder.CloseComponent();
        };
    }

    private static IReadOnlyDictionary<string, object?> CreateParameterDictionary(object? parameters)
    {
        if (parameters is null)
        {
            return EmptyParameters;
        }

        if (parameters is IReadOnlyDictionary<string, object?> typedReadOnly)
        {
            return typedReadOnly;
        }

        if (parameters is IReadOnlyDictionary<string, object> readOnlyObject)
        {
            return readOnlyObject.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal);
        }

        if (parameters is IDictionary<string, object?> typedDictionary)
        {
            return new Dictionary<string, object?>(typedDictionary, StringComparer.Ordinal);
        }

        if (parameters is IDictionary<string, object> dictionary)
        {
            return dictionary.ToDictionary(pair => pair.Key, pair => (object?)pair.Value, StringComparer.Ordinal);
        }

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var properties = parameters
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            result[property.Name] = property.GetValue(parameters);
        }

        return result;
    }

    private static void EnsureTargetId(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetId));
        }
    }

    private static string? NormalizeSelector(string? selector)
    {
        return string.IsNullOrWhiteSpace(selector) ? null : selector.Trim();
    }

    private void NotifyContentItemsUpdated()
    {
        ContentItemsUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void ClearInternal(bool notify)
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        if (notify)
        {
            NotifyContentItemsUpdated();
        }
    }
}

#pragma warning restore ASP0006
