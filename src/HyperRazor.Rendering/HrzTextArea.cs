using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzTextArea : ComponentBase
{
    [CascadingParameter]
    private HrzFieldContext? FieldContext { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();

    private IReadOnlyDictionary<string, object> _attributes = default!;
    private string? _content;

    protected override void OnParametersSet()
    {
        if (FieldContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzTextArea)} requires an ambient {nameof(HrzFieldContext)}.");
        }

        _attributes = HrzFieldControlRendering.BuildAttributes(
            FieldContext,
            AdditionalAttributes,
            allowLiveValidation: true);

        var currentValue = Convert.ToString(FieldContext.CurrentValue, CultureInfo.InvariantCulture);
        _content = HrzFormRendering.ValueOrAttempted(FieldContext.Form.SubmitValidationState, FieldContext.Path, currentValue);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "textarea");
        builder.AddMultipleAttributes(1, _attributes);
        builder.AddContent(2, _content);
        builder.CloseElement();
    }
}
