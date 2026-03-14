using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Rendering;

public sealed class HrzOptions
{
    private Type _rootComponent = typeof(HrzApp<HrzAppLayout>);

    public Type RootComponent
    {
        get => _rootComponent;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureComponentType(value, nameof(value));
            _rootComponent = value;
        }
    }

    public bool AllowRawContentOnNonHtmx { get; set; }

    private static void EnsureComponentType(Type type, string argumentName)
    {
        if (!typeof(IComponent).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type must implement {nameof(IComponent)}.", argumentName);
        }
    }
}
