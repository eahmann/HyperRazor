using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzInput : ComponentBase
{
    private static readonly HashSet<string> SupportedTypes =
        new(["text", "email", "search", "tel", "url", "password"], StringComparer.OrdinalIgnoreCase);

    [CascadingParameter]
    private HrzFieldContext? FieldContext { get; set; }

    [Parameter]
    public string Type { get; set; } = "text";

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();

    private IReadOnlyDictionary<string, object> _attributes = default!;

    protected override void OnParametersSet()
    {
        if (FieldContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzInput)} requires an ambient {nameof(HrzFieldContext)}.");
        }

        if (!SupportedTypes.Contains(Type))
        {
            throw new InvalidOperationException(
                $"{nameof(HrzInput)} does not support input type '{Type}'.");
        }

        var isPassword = string.Equals(Type, "password", StringComparison.OrdinalIgnoreCase);
        var attributes = HrzFieldControlRendering.BuildAttributes(
            FieldContext,
            AdditionalAttributes,
            allowLiveValidation: !isPassword);

        attributes["type"] = Type;

        if (isPassword)
        {
            attributes.Remove("value");
        }
        else
        {
            var currentValue = Convert.ToString(FieldContext.CurrentValue, CultureInfo.InvariantCulture);
            var value = HrzFormRendering.ValueOrAttempted(FieldContext.Form.SubmitValidationState, FieldContext.Path, currentValue);
            if (value is not null)
            {
                attributes["value"] = value;
            }
        }

        _attributes = attributes;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, _attributes);
        builder.CloseElement();
    }
}
