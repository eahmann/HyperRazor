using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Rendering;

internal sealed class HrzLayoutTypeResolver : IHrzLayoutTypeResolver
{
    private readonly ConcurrentDictionary<Type, Type?> _componentCache = new();

    public Type? ResolveForComponent(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"Type must implement {nameof(IComponent)}.", nameof(componentType));
        }

        return _componentCache.GetOrAdd(
            componentType,
            static type => ((LayoutAttribute?)Attribute.GetCustomAttribute(
                type,
                typeof(LayoutAttribute),
                inherit: true))?.LayoutType);
    }
}
