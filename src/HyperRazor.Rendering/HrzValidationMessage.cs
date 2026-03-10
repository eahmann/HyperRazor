using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HyperRazor.Rendering;

public sealed class HrzValidationMessage : ComponentBase
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
    public LambdaExpression? For { get; set; }

    [Parameter]
    public bool? LiveValidationActive { get; set; }

    private HrzFormContext _resolvedForm = default!;
    private HrzFieldPath _resolvedPath = default!;
    private string _resolvedHtmlName = string.Empty;
    private string _resolvedHtmlId = string.Empty;
    private string _messageId = string.Empty;
    private IReadOnlyList<string> _errors = Array.Empty<string>();
    private HrzFieldDescriptor _descriptor = default!;

    protected override void OnParametersSet()
    {
        (_resolvedForm, _resolvedPath, _descriptor, _resolvedHtmlName, _resolvedHtmlId, _messageId) = ResolveScope();
        _errors = HrzFormRendering.ErrorsFor(_resolvedForm.SubmitValidationState, _resolvedPath);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var clientSlotId = HrzFieldControlRendering.GetClientSlotId(_messageId);
        var serverSlotId = HrzFieldControlRendering.GetServerSlotId(_messageId);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", _messageId);
        builder.AddAttribute(2, "data-hrz-validation-for", _resolvedPath.Value);

        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "id", clientSlotId);
        builder.AddAttribute(5, "class", "validation-slot validation-slot--client");
        builder.AddAttribute(6, "data-hrz-client-validation-for", _resolvedPath.Value);
        builder.AddAttribute(7, "data-valmsg-for", _resolvedHtmlName);
        builder.CloseElement();

        var liveValidationActive = ResolveLiveValidationActive();
        if (liveValidationActive is not null)
        {
            builder.OpenElement(8, "div");
            builder.AddAttribute(9, "id", HrzFieldControlRendering.GetLiveStateSlotId(_messageId));
            builder.AddAttribute(10, "hidden", "hidden");
            builder.AddAttribute(11, "data-hrz-live-state-for", _resolvedHtmlId);
            builder.AddAttribute(12, "data-hrz-live-active", liveValidationActive.Value ? "true" : "false");
            builder.CloseElement();
        }

        builder.OpenElement(13, "div");
        builder.AddAttribute(14, "id", serverSlotId);
        builder.AddAttribute(15, "class", "validation-slot validation-slot--server");
        builder.AddAttribute(16, "data-hrz-server-validation-for", _resolvedPath.Value);

        builder.OpenRegion(17);
        foreach (var error in _errors)
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "validation-message");
            builder.AddContent(2, error);
            builder.CloseElement();
        }
        builder.CloseRegion();

        builder.CloseElement();
        builder.CloseElement();
    }

    private (HrzFormContext Form, HrzFieldPath Path, HrzFieldDescriptor Descriptor, string HtmlName, string HtmlId, string MessageId) ResolveScope()
    {
        if (FieldContext is not null && For is null)
        {
            return (
                FieldContext.Form,
                FieldContext.Path,
                FieldContext.Descriptor,
                FieldContext.HtmlName,
                FieldContext.HtmlId,
                FieldContext.MessageId);
        }

        if (FormContext is null)
        {
            throw new InvalidOperationException($"{nameof(HrzValidationMessage)} requires an ambient {nameof(HrzFormContext)}.");
        }

        if (For is null)
        {
            throw new InvalidOperationException($"{nameof(HrzValidationMessage)} requires {nameof(For)} when used outside {nameof(HrzField<object>)}.");
        }

        var path = HrzLambdaFieldPathResolver.Resolve(For, FieldPathResolver);
        var field = FormContext.Descriptor.Fields[path];
        return (
            FormContext,
            path,
            field,
            field.HtmlName,
            HtmlIdGenerator.GetFieldId(FormContext.FormName, path),
            HtmlIdGenerator.GetFieldMessageId(FormContext.FormName, path));
    }

    private bool? ResolveLiveValidationActive()
    {
        if (LiveValidationActive.HasValue)
        {
            return LiveValidationActive.Value;
        }

        return _descriptor.LiveRule?.StartsActive;
    }
}
