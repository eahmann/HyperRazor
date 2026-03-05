using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Components.Services;

#pragma warning disable ASP0006

public sealed class HrzHeadService : IHrzHeadService
{
    private readonly List<RenderFragment> _items = [];
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HrzHeadService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public event EventHandler? ContentItemsUpdated;

    public bool ContentAvailable => _items.Count > 0;

    public void AddTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(title));
        }

        AddHeadFragment(builder =>
        {
            builder.OpenElement(0, "title");
            builder.AddContent(1, title.Trim());
            builder.CloseElement();
        });
    }

    public void AddMeta(string name, string content)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        AddHeadFragment(builder =>
        {
            builder.OpenElement(0, "meta");
            builder.AddAttribute(1, "name", name.Trim());
            builder.AddAttribute(2, "content", content ?? string.Empty);
            builder.CloseElement();
        });
    }

    public void AddLink(string href, string rel = "stylesheet")
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(href));
        }

        AddHeadFragment(builder =>
        {
            builder.OpenElement(0, "link");
            builder.AddAttribute(1, "rel", string.IsNullOrWhiteSpace(rel) ? "stylesheet" : rel.Trim());
            builder.AddAttribute(2, "href", href.Trim());
            builder.CloseElement();
        });
    }

    public void AddHeadFragment(RenderFragment fragment)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        _items.Add(fragment);
        NotifyContentItemsUpdated();
    }

    public void AddRawContent(string html)
    {
        AddHeadFragment(builder => builder.AddMarkupContent(0, html ?? string.Empty));
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

        var snapshot = _items.ToArray();
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
