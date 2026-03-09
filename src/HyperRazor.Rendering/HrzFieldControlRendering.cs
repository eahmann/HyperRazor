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
        attributes["aria-invalid"] = HrzFormRendering.HasErrors(fieldContext.Form.SubmitValidationState, fieldContext.Path);
        attributes.TryAdd("aria-describedby", fieldContext.MessageId);

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
            attributes.TryAdd("hx-include", "closest form");
            attributes.TryAdd(
                "hx-vals",
                JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [HrzValidationFormFields.Root] = fieldContext.Form.RootId.Value,
                    [HrzValidationFormFields.Fields] = fieldContext.Path.Value
                }));
            attributes["data-hrz-client-slot-id"] = GetClientSlotId(fieldContext.MessageId);
            attributes["data-hrz-server-slot-id"] = GetServerSlotId(fieldContext.MessageId);
            attributes["data-hrz-summary-slot-id"] = fieldContext.Form.SummaryId;
        }

        return attributes;
    }
}
