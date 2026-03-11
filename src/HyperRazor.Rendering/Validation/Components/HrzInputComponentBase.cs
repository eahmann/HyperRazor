using Microsoft.AspNetCore.Components;

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
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["id"] = ResolvedFieldContext.InputId,
            ["name"] = ResolvedFieldContext.Name,
            ["aria-invalid"] = ResolvedFieldContext.HasErrors ? "true" : "false",
            ["aria-describedby"] = ResolvedFieldContext.AriaDescribedBy,
            ["data-hrz-field-path"] = ResolvedFieldContext.FieldPath.Value,
            ["data-hrz-server-slot-id"] = ResolvedFieldContext.ServerSlotId
        };

        if (ResolvedFieldContext.HasLiveValidation)
        {
            attributes["data-hrz-live-policy-id"] = ResolvedFieldContext.LivePolicyId;
            attributes["data-hrz-summary-slot-id"] = ResolvedFieldContext.Form.SummaryId;
            attributes["hx-disinherit"] = "hx-disabled-elt";
            attributes["hx-post"] = ResolvedFieldContext.LiveValidationPath!;
            attributes["hx-trigger"] = ResolvedFieldContext.LiveTrigger!;
            attributes["hx-target"] = $"#{ResolvedFieldContext.ServerSlotId}";
            attributes["hx-swap"] = "outerHTML";
            attributes["hx-include"] = ResolvedFieldContext.LiveInclude!;
            attributes["hx-sync"] = ResolvedFieldContext.LiveSync!;
            attributes["hx-vals"] = ResolvedFieldContext.LiveValidationValuesJson!;
        }

        foreach (var (key, value) in ResolvedFieldContext.ClientValidationAttributes)
        {
            attributes[key] = value;
        }

        if (ResolvedFieldContext.EnableClientValidation)
        {
            attributes["data-hrz-client-slot-id"] = ResolvedFieldContext.ClientSlotId;
        }

        MergeAttributes(attributes, elementAttributes);
        MergeAttributes(attributes, AdditionalAttributes);
        MergeClassValue(attributes, elementAttributes, AdditionalAttributes);

        return attributes;
    }

    private static void MergeAttributes(
        IDictionary<string, object> target,
        IReadOnlyDictionary<string, object?>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var (key, value) in source)
        {
            if (string.Equals(key, "class", StringComparison.OrdinalIgnoreCase) || value is null)
            {
                continue;
            }

            if (value is bool booleanValue)
            {
                if (booleanValue)
                {
                    target[key] = booleanValue;
                }

                continue;
            }

            target[key] = value;
        }
    }

    private void MergeClassValue(
        IDictionary<string, object> attributes,
        IReadOnlyDictionary<string, object?>? elementAttributes,
        IReadOnlyDictionary<string, object?>? additionalAttributes)
    {
        var mergedClass = string.Join(
            " ",
            new[]
            {
                Class,
                GetClassValue(elementAttributes),
                GetClassValue(additionalAttributes)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(mergedClass))
        {
            attributes["class"] = mergedClass;
        }
    }

    private static string? GetClassValue(IReadOnlyDictionary<string, object?>? attributes)
    {
        if (attributes is null)
        {
            return null;
        }

        return attributes.TryGetValue("class", out var value)
            ? value?.ToString()
            : null;
    }
}
