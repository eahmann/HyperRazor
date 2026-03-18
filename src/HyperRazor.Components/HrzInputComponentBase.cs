using Microsoft.AspNetCore.Components;
using HyperRazor.Components.Validation;

namespace HyperRazor.Components;

public abstract class HrzInputComponentBase : ComponentBase
{
    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private HrzFieldScope? FieldScope { get; set; }

    protected HrzFieldScope ResolvedField => FieldScope
        ?? throw new InvalidOperationException($"{GetType().Name} requires a cascading {nameof(HrzField<object>)}.");

    protected override void OnParametersSet()
    {
        _ = ResolvedField;
    }

    protected IReadOnlyDictionary<string, object> BuildControlAttributes(
        IReadOnlyDictionary<string, object?>? elementAttributes = null)
    {
        return ResolvedField.BuildControlAttributes(
            elementAttributes,
            Class,
            AdditionalAttributes);
    }
}
