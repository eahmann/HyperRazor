using System.Text.Json;

namespace HyperRazor.Rendering;

internal static class HrzFieldControlRendering
{
    public static string GetClientSlotId(string messageId) => $"{messageId}--client";

    public static string GetServerSlotId(string messageId) => $"{messageId}--server";

    public static Dictionary<string, object> BuildAttributes(
        HrzFieldContext fieldContext,
        IReadOnlyDictionary<string, object?> additionalAttributes,
        bool allowLiveValidation)
    {
        ArgumentNullException.ThrowIfNull(fieldContext);
        ArgumentNullException.ThrowIfNull(additionalAttributes);

        var attributes = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in additionalAttributes)
        {
            if (attribute.Value is not null)
            {
                attributes[attribute.Key] = attribute.Value;
            }
        }

        attributes.TryAdd("id", fieldContext.HtmlId);
        attributes["name"] = fieldContext.HtmlName;
        attributes["aria-invalid"] = HrzFormRendering.HasErrors(fieldContext.Form.SubmitValidationState, fieldContext.Path)
            ? "true"
            : "false";
        attributes.TryAdd("aria-describedby", fieldContext.MessageId);
        attributes["data-hrz-client-slot-id"] = GetClientSlotId(fieldContext.MessageId);
        attributes["data-hrz-server-slot-id"] = GetServerSlotId(fieldContext.MessageId);

        if (fieldContext.Descriptor.LocalRules.Count > 0)
        {
            attributes["data-val"] = "true";

            foreach (var rule in fieldContext.Descriptor.LocalRules)
            {
                attributes[$"data-val-{rule.Key}"] = rule.Value;
            }
        }

        if (allowLiveValidation && fieldContext.Descriptor.LiveRule is { } liveRule)
        {
            attributes.TryAdd("hx-post", liveRule.Endpoint);
            attributes.TryAdd("hx-trigger", liveRule.Trigger);
            attributes.TryAdd("hx-target", $"#{GetServerSlotId(fieldContext.MessageId)}");
            attributes.TryAdd("hx-swap", "outerHTML");

            var includeSelector = BuildLiveValidationIncludeSelector(fieldContext, liveRule);
            if (!string.IsNullOrWhiteSpace(includeSelector))
            {
                attributes.TryAdd("hx-include", includeSelector);
            }

            attributes.TryAdd(
                "hx-vals",
                JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [HrzValidationFormFields.Root] = fieldContext.Form.RootId.Value,
                    [HrzValidationFormFields.Fields] = fieldContext.Path.Value
                }));
            attributes["data-hrz-summary-slot-id"] = fieldContext.Form.SummaryId;
        }

        return attributes;
    }

    private static string? BuildLiveValidationIncludeSelector(
        HrzFieldContext fieldContext,
        HrzLiveRuleDescriptor liveRule)
    {
        if (liveRule.AdditionalFields.Count == 0)
        {
            return null;
        }

        var selectors = new List<string>(liveRule.AdditionalFields.Count);

        foreach (var dependencyPath in liveRule.AdditionalFields)
        {
            if (!fieldContext.Form.FieldIds.TryGetValue(dependencyPath, out var dependencyId))
            {
                continue;
            }

            selectors.Add($"#{EscapeSelectorValue(dependencyId)}");
        }

        return selectors.Count == 0
            ? null
            : string.Join(", ", selectors.Distinct(StringComparer.Ordinal));
    }

    private static string EscapeSelectorValue(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
