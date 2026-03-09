using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzValidationSummary : ComponentBase
{
    [CascadingParameter]
    private HrzFormContext? FormContext { get; set; }

    private IReadOnlyList<string> _errors = Array.Empty<string>();

    protected override void OnParametersSet()
    {
        if (FormContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzValidationSummary)} requires an ambient {nameof(HrzFormContext)}.");
        }

        _errors = FormContext.SubmitValidationState?.SummaryErrors ?? Array.Empty<string>();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(FormContext);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", FormContext.SummaryId);
        builder.AddAttribute(2, "class", _errors.Count == 0
            ? "validation-summary validation-summary--empty"
            : "validation-summary");
        builder.AddAttribute(3, "data-hrz-server-validation-summary", true);

        if (_errors.Count > 0)
        {
            builder.OpenElement(4, "ul");
            builder.AddAttribute(5, "class", "validation-errors");

            builder.OpenRegion(6);
            foreach (var error in _errors)
            {
                builder.OpenElement(0, "li");
                builder.AddContent(1, error);
                builder.CloseElement();
            }
            builder.CloseRegion();

            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
