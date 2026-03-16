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
    private HrzValidationFieldContext? FieldContext { get; set; }

    private protected HrzValidationFieldContext ResolvedFieldContext => FieldContext
        ?? throw new InvalidOperationException($"{GetType().Name} requires a cascading {nameof(HrzField<object>)}.");

    protected override void OnParametersSet()
    {
        _ = ResolvedFieldContext;
    }

    protected IReadOnlyDictionary<string, object> BuildControlAttributes(
        IReadOnlyDictionary<string, object?>? elementAttributes = null)
    {
        return ResolvedFieldContext.View.BuildControlAttributes(
            elementAttributes,
            Class,
            AdditionalAttributes);
    }
}
