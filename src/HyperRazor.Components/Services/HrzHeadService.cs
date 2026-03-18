using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Components.Services;

#pragma warning disable ASP0006

public sealed class HrzHeadService : IHrzHeadService
{
    private readonly List<HeadItem> _items = [];
    private readonly IHttpContextAccessor _httpContextAccessor;
    private long _nextSequence;

    public HrzHeadService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public event EventHandler? ContentItemsUpdated;

    public bool ContentAvailable => _items.Count > 0;

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(title));
        }

        UpsertItem(
            HeadItemKind.Title,
            HeadItem.TitleKey,
            builder =>
            {
                builder.OpenElement(0, "title");
                builder.AddContent(1, title.Trim());
                builder.CloseElement();
            });
    }

    public void AddTitle(string title)
    {
        SetTitle(title);
    }

    public void AddMeta(string name, string content, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        UpsertItem(
            HeadItemKind.Meta,
            NormalizeKey(key),
            builder =>
            {
                builder.OpenElement(0, "meta");
                builder.AddAttribute(1, "name", name.Trim());
                builder.AddAttribute(2, "content", content ?? string.Empty);
                builder.CloseElement();
            });
    }

    public void AddLink(string href, string rel = "stylesheet", string? key = null)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(href));
        }

        UpsertItem(
            HeadItemKind.Link,
            NormalizeKey(key),
            builder =>
            {
                builder.OpenElement(0, "link");
                builder.AddAttribute(1, "rel", string.IsNullOrWhiteSpace(rel) ? "stylesheet" : rel.Trim());
                builder.AddAttribute(2, "href", href.Trim());
                builder.CloseElement();
            });
    }

    public void AddScript(string src, IReadOnlyDictionary<string, object?>? attributes = null, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(src))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(src));
        }

        UpsertItem(
            HeadItemKind.Script,
            NormalizeKey(key),
            builder =>
            {
                builder.OpenElement(0, "script");
                builder.AddAttribute(1, "src", src.Trim());

                var sequence = 2;
                foreach (var attribute in OrderAttributes(attributes))
                {
                    if (string.Equals(attribute.Key, "src", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    builder.AddAttribute(sequence++, attribute.Key, attribute.Value);
                }

                builder.CloseElement();
            });
    }

    public void AddStyle(string cssText, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(cssText))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cssText));
        }

        UpsertItem(
            HeadItemKind.Style,
            NormalizeKey(key),
            builder =>
            {
                builder.OpenElement(0, "style");
                builder.AddContent(1, cssText.Trim());
                builder.CloseElement();
            });
    }

    public void AddHeadFragment(RenderFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        AddItem(HeadItemKind.Fragment, fragment);
    }

    public void AddRawContent(string html)
    {
        AddItem(HeadItemKind.Raw, builder => builder.AddMarkupContent(0, html ?? string.Empty));
    }

    public RenderFragment RenderToFragment(bool clear = false)
    {
        var request = GetCurrentRequest();
        if (!request.IsPartialRequest)
        {
            if (clear)
            {
                ClearInternal(notify: false);
            }

            return _ => { };
        }

        var snapshot = _items
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Sequence)
            .Select(item => item.Fragment)
            .ToArray();
        if (clear)
        {
            ClearInternal(notify: false);
        }

        return builder =>
        {
            if (snapshot.Length == 0)
            {
                return;
            }

            builder.OpenElement(0, "head");
            builder.AddAttribute(1, "hx-head", "merge");
            var sequence = 2;
            foreach (var item in snapshot)
            {
                builder.AddContent(sequence++, item);
            }

            builder.CloseElement();
        };
    }

    public void Clear()
    {
        ClearInternal(notify: true);
    }

    private HtmxRequest GetCurrentRequest()
    {
        return _httpContextAccessor.HttpContext?.HtmxRequest() ?? new HtmxRequest();
    }

    private void NotifyContentItemsUpdated()
    {
        ContentItemsUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void AddItem(HeadItemKind kind, RenderFragment fragment)
    {
        _items.Add(new HeadItem(kind, _nextSequence++, Key: null, fragment));
        NotifyContentItemsUpdated();
    }

    private void UpsertItem(HeadItemKind kind, string? key, RenderFragment fragment)
    {
        if (key is not null)
        {
            var index = _items.FindIndex(item => item.Kind == kind && string.Equals(item.Key, key, StringComparison.Ordinal));
            if (index >= 0)
            {
                var existing = _items[index];
                _items[index] = existing with { Fragment = fragment };
                NotifyContentItemsUpdated();
                return;
            }
        }

        _items.Add(new HeadItem(kind, _nextSequence++, key, fragment));
        NotifyContentItemsUpdated();
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

    private static IEnumerable<KeyValuePair<string, object?>> OrderAttributes(IReadOnlyDictionary<string, object?>? attributes)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return Enumerable.Empty<KeyValuePair<string, object?>>();
        }

        return attributes.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static string? NormalizeKey(string? key)
    {
        return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    private enum HeadItemKind
    {
        Title = 0,
        Meta = 1,
        Link = 2,
        Style = 3,
        Script = 4,
        Fragment = 5,
        Raw = 6
    }

    private sealed record HeadItem(HeadItemKind Kind, long Sequence, string? Key, RenderFragment Fragment)
    {
        internal const string TitleKey = "__title";

        public int SortOrder => (int)Kind;
    }
}

#pragma warning restore ASP0006
