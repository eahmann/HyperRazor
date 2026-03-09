using HyperRazor.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public sealed class HrzForm<TModel> : ComponentBase
{
    [Inject]
    private IHrzValidationDescriptorProvider ValidationDescriptorProvider { get; set; } = default!;

    [Inject]
    private IHrzHtmlIdGenerator HtmlIdGenerator { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Parameter, EditorRequired]
    public required TModel Model { get; set; }

    [Parameter, EditorRequired]
    public required string Action { get; set; }

    [Parameter, EditorRequired]
    public required string FormName { get; set; }

    [Parameter]
    public bool Enhance { get; set; } = true;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?> AdditionalAttributes { get; set; } =
        new Dictionary<string, object?>();

    private HrzFormContext _formContext = default!;
    private IReadOnlyDictionary<string, object> _formAttributes = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Model);
        ArgumentException.ThrowIfNullOrWhiteSpace(Action);
        ArgumentException.ThrowIfNullOrWhiteSpace(FormName);

        var formId = ResolveFormId();
        var rootId = new HrzValidationRootId(FormName);
        var modelType = Model.GetType();

        _formContext = new HrzFormContext
        {
            Model = Model,
            ModelType = modelType,
            FormName = FormName,
            RootId = rootId,
            FormId = formId,
            SummaryId = HtmlIdGenerator.GetSummaryId(FormName),
            Enhance = Enhance,
            Descriptor = ValidationDescriptorProvider.GetDescriptor(modelType),
            SubmitValidationState = HttpContextAccessor.HttpContext?.GetSubmitValidationState(rootId)
        };

        _formAttributes = BuildFormAttributes(formId);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<HrzFormContext>>(0);
        builder.AddAttribute(1, nameof(CascadingValue<HrzFormContext>.Value), _formContext);
        builder.AddAttribute(2, nameof(CascadingValue<HrzFormContext>.IsFixed), true);
        builder.AddAttribute(3, nameof(CascadingValue<HrzFormContext>.ChildContent), (RenderFragment)BuildFormContent);
        builder.CloseComponent();
    }

    private void BuildFormContent(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "form");
        builder.AddMultipleAttributes(1, _formAttributes);

        builder.OpenComponent<HrzAntiforgeryInput>(2);
        builder.CloseComponent();

        builder.OpenElement(3, "input");
        builder.AddAttribute(4, "type", "hidden");
        builder.AddAttribute(5, "name", HrzValidationFormFields.Root);
        builder.AddAttribute(6, "value", _formContext.RootId.Value);
        builder.CloseElement();

        builder.AddContent(7, ChildContent);
        builder.CloseElement();
    }

    private IReadOnlyDictionary<string, object> BuildFormAttributes(string formId)
    {
        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (attribute.Value is not null)
                {
                    attributes[attribute.Key] = attribute.Value;
                }
            }
        }

        attributes["id"] = formId;
        attributes["method"] = "post";
        attributes["action"] = Action;

        if (Enhance)
        {
            attributes.TryAdd("hx-post", Action);
            attributes.TryAdd("hx-target", $"#{formId}");
            attributes.TryAdd("hx-swap", "outerHTML");
        }

        return attributes;
    }

    private string ResolveFormId()
    {
        if (AdditionalAttributes is not null
            && TryGetAttributeValue(AdditionalAttributes, "id", out var idValue)
            && idValue is string formId
            && !string.IsNullOrWhiteSpace(formId))
        {
            return formId;
        }

        return HtmlIdGenerator.GetFormId(FormName);
    }

    private static bool TryGetAttributeValue(
        IReadOnlyDictionary<string, object?> attributes,
        string name,
        out object? value)
    {
        if (attributes.TryGetValue(name, out value))
        {
            return true;
        }

        foreach (var entry in attributes)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}
