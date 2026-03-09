using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzField<TValue> : ComponentBase
{
    [Inject]
    private IHrzFieldPathResolver FieldPathResolver { get; set; } = default!;

    [Inject]
    private IHrzHtmlIdGenerator HtmlIdGenerator { get; set; } = default!;

    [CascadingParameter]
    private HrzFormContext? FormContext { get; set; }

    [Parameter, EditorRequired]
    public required Expression<Func<TValue>> For { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private HrzFieldContext _fieldContext = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(For);

        if (FormContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzField<TValue>)} requires an ambient {nameof(HrzFormContext)}.");
        }

        var path = FieldPathResolver.FromExpression(For);
        var descriptor = HrzValidationDescriptorFieldResolver.Resolve(FormContext.Descriptor, path, FieldPathResolver);

        _fieldContext = new HrzFieldContext
        {
            Form = FormContext,
            Path = path,
            Descriptor = descriptor,
            HtmlName = descriptor.HtmlName,
            HtmlId = HtmlIdGenerator.GetFieldId(FormContext.FormName, path),
            MessageId = HtmlIdGenerator.GetFieldMessageId(FormContext.FormName, path),
            CurrentValue = ReadCurrentValue()
        };
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is null)
        {
            return;
        }

        builder.OpenComponent<CascadingValue<HrzFieldContext>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<HrzFieldContext>.Value), _fieldContext);
        builder.AddAttribute(2, nameof(CascadingValue<HrzFieldContext>.IsFixed), true);
        builder.AddAttribute(3, nameof(CascadingValue<HrzFieldContext>.ChildContent), ChildContent);
        builder.CloseComponent();
    }

    private object? ReadCurrentValue()
    {
        try
        {
            return For.Compile().Invoke();
        }
        catch (NullReferenceException)
        {
            return null;
        }
    }
}
