using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Rendering;

public sealed class HrxOptions
{
    private Type _rootComponent = typeof(HrxApp<HrxAppLayout>);

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

    public bool UseMinimalLayoutForHtmx { get; set; } = true;

    public bool AllowRawContentOnNonHtmx { get; set; }

    public HrxLayoutBoundaryOptions LayoutBoundary { get; set; } = new();

    private static void EnsureComponentType(Type type, string argumentName)
    {
        if (!typeof(IComponent).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type must implement {nameof(IComponent)}.", argumentName);
        }
    }
}
