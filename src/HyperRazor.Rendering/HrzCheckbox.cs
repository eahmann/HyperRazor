using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzCheckbox : ComponentBase
{
    [CascadingParameter]
    private HrzFieldContext? FieldContext { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();

    private IReadOnlyDictionary<string, object> _attributes = default!;
    private bool _checked;

    protected override void OnParametersSet()
    {
        if (FieldContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzCheckbox)} requires an ambient {nameof(HrzFieldContext)}.");
        }

        if (FieldContext.CurrentValue is not bool)
        {
            throw new InvalidOperationException($"{nameof(HrzCheckbox)} only supports non-nullable bool fields in v1.");
        }

        _checked = ResolveChecked(FieldContext);

        var attributes = HrzFieldControlRendering.BuildAttributes(
            FieldContext,
            AdditionalAttributes,
            allowLiveValidation: true);
        attributes["type"] = "checkbox";
        attributes["value"] = "true";

        if (_checked)
        {
            attributes["checked"] = "checked";
        }
        else
        {
            attributes.Remove("checked");
        }

        _attributes = attributes;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(FieldContext);

        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", "hidden");
        builder.AddAttribute(2, "name", FieldContext.HtmlName);
        builder.AddAttribute(3, "value", "false");
        builder.CloseElement();

        builder.OpenElement(4, "input");
        builder.AddMultipleAttributes(5, _attributes);
        builder.CloseElement();
    }

    private static bool ResolveChecked(HrzFieldContext fieldContext)
    {
        var attemptedValue = HrzFormRendering.AttemptedValueFor(fieldContext.Form.SubmitValidationState, fieldContext.Path);
        if (attemptedValue is not null && attemptedValue.Values.Count > 0)
        {
            return attemptedValue.Values.Any(IsTruthy);
        }

        return (bool)fieldContext.CurrentValue!;
    }

    private static bool IsTruthy(string? value)
    {
        return value is not null
            && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("on", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
