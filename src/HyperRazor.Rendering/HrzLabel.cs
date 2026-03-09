using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzLabel : ComponentBase
{
    [Inject]
    private IHrzFieldPathResolver FieldPathResolver { get; set; } = default!;

    [Inject]
    private IHrzHtmlIdGenerator HtmlIdGenerator { get; set; } = default!;

    [CascadingParameter]
    private HrzFormContext? FormContext { get; set; }

    [CascadingParameter]
    private HrzFieldContext? FieldContext { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public LambdaExpression? For { get; set; }

    private string _htmlId = string.Empty;
    private string _text = string.Empty;

    protected override void OnParametersSet()
    {
        if (FieldContext is not null && For is null)
        {
            _htmlId = FieldContext.HtmlId;
            _text = ResolveText(FieldContext.Descriptor.DisplayName, FieldContext.Path);
            return;
        }

        if (FormContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzLabel)} requires an ambient {nameof(HrzFormContext)}.");
        }

        if (For is null)
        {
            throw new InvalidOperationException($"{nameof(HrzLabel)} requires {nameof(For)} when used outside {nameof(HrzField<object>)}.");
        }

        var path = HrzLambdaFieldPathResolver.Resolve(For, FieldPathResolver);
        var descriptor = HrzValidationDescriptorFieldResolver.Resolve(FormContext.Descriptor, path, FieldPathResolver);

        _htmlId = HtmlIdGenerator.GetFieldId(FormContext.FormName, path);
        _text = ResolveText(descriptor.DisplayName, path);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "label");
        builder.AddAttribute(1, "for", _htmlId);
        builder.AddContent(2, _text);
        builder.CloseElement();
    }

    private string ResolveText(string? displayName, HrzFieldPath path)
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            return Text;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        var segment = path.Value.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? path.Value;
        var bracketIndex = segment.IndexOf('[');
        return bracketIndex >= 0 ? segment[..bracketIndex] : segment;
    }
}
