using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Components;

public sealed class HrzRegion : ComponentBase
{
    [Parameter, EditorRequired]
    public string Name { get; set; } = default!;

    [Parameter]
    public string TagName { get; set; } = "div";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? AdditionalAttributes { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException($"{nameof(Name)} must be provided.");
        }

        if (string.IsNullOrWhiteSpace(TagName))
        {
            throw new InvalidOperationException($"{nameof(TagName)} must be provided.");
        }

        builder.OpenElement(0, TagName);
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", Name);
        builder.AddAttribute(3, "data-hrz-region", Name);
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}
