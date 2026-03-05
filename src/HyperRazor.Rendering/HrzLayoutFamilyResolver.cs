using System.Collections.Concurrent;
using HyperRazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace HyperRazor.Rendering;

public sealed class HrzLayoutFamilyResolver : IHrzLayoutFamilyResolver
{
    private const string LayoutSuffix = "Layout";

    private readonly HrzOptions _options;
    private readonly ConcurrentDictionary<Type, string> _pageCache = new();
    private readonly ConcurrentDictionary<Type, string> _layoutCache = new();

    public HrzLayoutFamilyResolver(IOptions<HrzOptions> options)
    {
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public string ResolveForPageComponent(Type pageComponentType)
    {
        ArgumentNullException.ThrowIfNull(pageComponentType);

        return _pageCache.GetOrAdd(pageComponentType, ResolvePageFamilyCore);
    }

    public string ResolveForLayoutType(Type layoutType)
    {
        ArgumentNullException.ThrowIfNull(layoutType);

        return _layoutCache.GetOrAdd(layoutType, ResolveLayoutFamilyCore);
    }

    private string ResolvePageFamilyCore(Type pageComponentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(pageComponentType))
        {
            throw new ArgumentException($"Type must implement {nameof(IComponent)}.", nameof(pageComponentType));
        }

        var layoutAttribute = (LayoutAttribute?)Attribute.GetCustomAttribute(
            pageComponentType,
            typeof(LayoutAttribute),
            inherit: true);

        return layoutAttribute?.LayoutType is null
            ? _options.LayoutBoundary.DefaultLayoutFamily
            : ResolveForLayoutType(layoutAttribute.LayoutType);
    }

    private string ResolveLayoutFamilyCore(Type layoutType)
    {
        var explicitFamily = (HrzLayoutFamilyAttribute?)Attribute.GetCustomAttribute(
            layoutType,
            typeof(HrzLayoutFamilyAttribute),
            inherit: true);

        if (explicitFamily is not null)
        {
            return explicitFamily.Family;
        }

        var typeName = layoutType.Name;
        if (typeName.EndsWith(LayoutSuffix, StringComparison.OrdinalIgnoreCase)
            && typeName.Length > LayoutSuffix.Length)
        {
            var family = typeName[..^LayoutSuffix.Length];
            if (!string.IsNullOrWhiteSpace(family))
            {
                return family.ToLowerInvariant();
            }
        }

        return _options.LayoutBoundary.DefaultLayoutFamily;
    }
}
