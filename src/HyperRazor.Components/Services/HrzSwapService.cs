using HyperRazor.Components;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Components.Services;

#pragma warning disable ASP0006

public sealed class HrzSwapService : IHrzSwapService, IHrzSwapBuffer
{
    private readonly List<HrzSwapItem> _items = [];
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider? _services;
    private readonly ILoggerFactory? _loggerFactory;
    private event EventHandler? _contentItemsUpdated;

    public HrzSwapService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _services = null;
        _loggerFactory = null;
    }

    public HrzSwapService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider services,
        ILoggerFactory loggerFactory)
        : this(httpContextAccessor)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    event EventHandler? IHrzSwapBuffer.ContentItemsUpdated
    {
        add => _contentItemsUpdated += value;
        remove => _contentItemsUpdated -= value;
    }

    bool IHrzSwapBuffer.ContentAvailable => _items.Count > 0;

    public void Replace<TComponent>(
        string target,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent
    {
        Replace(
            target,
            BuildComponentFragment<TComponent>(HrzParameterDictionaryFactory.Create(parameters)),
            options);
    }

    public void Replace(
        string target,
        RenderFragment fragment,
        HrzSwapOptions? options = null)
    {
        QueueSwap(
            target,
            targetIdFactory: _ => ResolveReplaceTargetId(target, options),
            swapDescriptorFactory: resolvedTargetId => BuildSwapDescriptor(HrzSwapOperation.Replace, target, resolvedTargetId, options),
            fragment,
            options,
            itemId: null);
    }

    public void Append<TComponent>(
        string target,
        string itemId,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent
    {
        Append(
            target,
            itemId,
            BuildComponentFragment<TComponent>(HrzParameterDictionaryFactory.Create(parameters)),
            options);
    }

    public void Append(
        string target,
        string itemId,
        RenderFragment fragment,
        HrzSwapOptions? options = null)
    {
        QueueSwap(
            target,
            targetIdFactory: resolvedItemId => resolvedItemId,
            swapDescriptorFactory: resolvedItemId => BuildSwapDescriptor(HrzSwapOperation.Append, target, resolvedItemId, options),
            fragment,
            options,
            itemId);
    }

    public void Prepend<TComponent>(
        string target,
        string itemId,
        object? parameters = null,
        HrzSwapOptions? options = null)
        where TComponent : IComponent
    {
        Prepend(
            target,
            itemId,
            BuildComponentFragment<TComponent>(HrzParameterDictionaryFactory.Create(parameters)),
            options);
    }

    public void Prepend(
        string target,
        string itemId,
        RenderFragment fragment,
        HrzSwapOptions? options = null)
    {
        QueueSwap(
            target,
            targetIdFactory: resolvedItemId => resolvedItemId,
            swapDescriptorFactory: resolvedItemId => BuildSwapDescriptor(HrzSwapOperation.Prepend, target, resolvedItemId, options),
            fragment,
            options,
            itemId);
    }

    RenderFragment IHrzSwapBuffer.RenderToFragment(bool clear)
    {
        return CreateBufferedFragment(clear);
    }

    private RenderFragment CreateBufferedFragment(bool clear = false)
    {
        var request = GetCurrentRequest();
        var includeSwappables = ShouldForceSwapRendering() || (request.IsHtmx && !request.IsHistoryRestoreRequest);
        var snapshot = includeSwappables ? _items.ToArray() : [];

        if (clear)
        {
            ClearInternal(notify: false);
        }

        return builder =>
        {
            var sequence = 0;

            foreach (var item in snapshot)
            {
                builder.OpenComponent<HrzSwappable>(sequence++);
                builder.AddAttribute(sequence++, nameof(HrzSwappable.TargetId), item.TargetId);
                builder.AddAttribute(sequence++, nameof(HrzSwappable.SwapDescriptor), item.SwapDescriptor);
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

        return context.Items.TryGetValue(HrzComponentContextItemKeys.ForceSwapRendering, out var value)
            && value is true;
    }

    public async Task<string> RenderToString(bool clear = false, CancellationToken cancellationToken = default)
    {
        if (_services is null || _loggerFactory is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RenderToString)} requires {nameof(IServiceProvider)} and {nameof(ILoggerFactory)} to be available.");
        }

        var fragment = CreateBufferedFragment(clear: clear);

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

    private void QueueSwap(
        string target,
        Func<string, string> targetIdFactory,
        Func<string, string> swapDescriptorFactory,
        RenderFragment fragment,
        HrzSwapOptions? options,
        string? itemId)
    {
        EnsureTarget(target);
        ArgumentNullException.ThrowIfNull(fragment);

        var normalizedOptions = options ?? new HrzSwapOptions();
        var resolvedItemId = normalizedOptions.TargetKind == HrzSwapTargetKind.Region
            ? NormalizeTargetId(itemId ?? target)
            : NormalizeTargetId(itemId ?? normalizedOptions.TargetId);
        var targetId = targetIdFactory(resolvedItemId);

        _items.Add(new HrzSwapItem(
            TargetId: targetId,
            SwapDescriptor: swapDescriptorFactory(resolvedItemId),
            Fragment: fragment));
        NotifyContentItemsUpdated();
    }

    private HtmxRequest GetCurrentRequest()
    {
        return _httpContextAccessor.HttpContext?.HtmxRequest() ?? new HtmxRequest();
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

    private static void EnsureTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(target));
        }
    }

    private static string ResolveReplaceTargetId(string target, HrzSwapOptions? options)
    {
        var normalizedOptions = options ?? new HrzSwapOptions();
        return normalizedOptions.TargetKind == HrzSwapTargetKind.Region
            ? NormalizeTargetId(target)
            : NormalizeTargetId(normalizedOptions.TargetId);
    }

    private static string NormalizeTargetId(string? targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(targetId));
        }

        return targetId.Trim();
    }

    private static string NormalizeSelector(string target)
    {
        return string.IsNullOrWhiteSpace(target)
            ? throw new ArgumentException("Value cannot be null or whitespace.", nameof(target))
            : target.Trim();
    }

    private static string BuildSwapDescriptor(
        HrzSwapOperation operation,
        string target,
        string _,
        HrzSwapOptions? options)
    {
        var normalizedOptions = options ?? new HrzSwapOptions();
        var selector = normalizedOptions.TargetKind == HrzSwapTargetKind.Region
            ? $"#{NormalizeTargetId(target)}"
            : NormalizeSelector(target);

        return operation switch
        {
            HrzSwapOperation.Replace when normalizedOptions.TargetKind == HrzSwapTargetKind.Region => "innerHTML",
            HrzSwapOperation.Replace => $"outerHTML:{selector}",
            HrzSwapOperation.Append => $"beforeend:{selector}",
            HrzSwapOperation.Prepend => $"afterbegin:{selector}",
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    private void NotifyContentItemsUpdated()
    {
        _contentItemsUpdated?.Invoke(this, EventArgs.Empty);
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
