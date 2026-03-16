namespace HyperRazor.Components.Validation;

public abstract class HrzFieldView
{
    internal HrzFieldView(
        HrzFormView form,
        HrzFieldDescriptor descriptor,
        HrzFieldValueProjection projection,
        IReadOnlyDictionary<string, string> clientValidationAttributes,
        HrzResolvedLiveValidation live)
    {
        Form = form;
        FieldPath = descriptor.FieldPath;
        Name = descriptor.Name;
        InputId = descriptor.InputId;
        ClientSlotId = descriptor.ClientSlotId;
        ServerSlotId = descriptor.ServerSlotId;
        LivePolicyId = descriptor.LivePolicyId;
        ValueType = descriptor.ValueType;
        LabelText = descriptor.LabelText;
        CurrentValueUntyped = projection.CurrentValue;
        AttemptedValue = projection.AttemptedValue;
        Value = projection.Value;
        Values = projection.Values;
        IsChecked = projection.IsChecked;
        Errors = projection.Errors;
        HasErrors = projection.HasErrors;
        EnableClientValidation = descriptor.EnableClientValidation;
        ClientValidationAttributes = clientValidationAttributes;
        AriaDescribedBy = BuildAriaDescribedBy(descriptor.EnableClientValidation);
        HasLiveValidation = live.Enabled;
        LiveValidationPath = live.Path;
        LiveTrigger = live.Trigger;
        LiveInclude = live.Include;
        LiveSync = live.Sync;
        LiveValidationValuesJson = live.ValuesJson;
    }

    public HrzFormView Form { get; }

    public HrzFieldPath FieldPath { get; }

    public string Name { get; }

    public string InputId { get; }

    public string ClientSlotId { get; }

    public string ServerSlotId { get; }

    public string LivePolicyId { get; }

    public Type ValueType { get; }

    public object? CurrentValueUntyped { get; }

    public HrzAttemptedValue? AttemptedValue { get; }

    public string? Value { get; }

    public IReadOnlyList<string> Values { get; }

    public bool IsChecked { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool HasErrors { get; }

    public string LabelText { get; }

    public bool EnableClientValidation { get; }

    public IReadOnlyDictionary<string, string> ClientValidationAttributes { get; }

    public string AriaDescribedBy { get; }

    public bool HasLiveValidation { get; }

    public string? LiveValidationPath { get; }

    public string? LiveTrigger { get; }

    public string? LiveInclude { get; }

    public string? LiveSync { get; }

    public string? LiveValidationValuesJson { get; }

    public IReadOnlyDictionary<string, object> AsTextInput(
        string type = "text",
        string? placeholder = null,
        string? autocomplete = null,
        string? inputMode = null,
        bool includeClientValidationSlot = true) =>
        BuildControlAttributes(new Dictionary<string, object?>
        {
            ["type"] = type,
            ["value"] = Value ?? string.Empty,
            ["placeholder"] = placeholder,
            ["autocomplete"] = autocomplete,
            ["inputmode"] = inputMode
        }, includeClientValidationSlot: includeClientValidationSlot);

    public IReadOnlyDictionary<string, object> AsTextArea(
        int? rows = null,
        string? placeholder = null,
        string? autocomplete = null,
        bool includeClientValidationSlot = true) =>
        BuildControlAttributes(new Dictionary<string, object?>
        {
            ["rows"] = rows?.ToString(),
            ["placeholder"] = placeholder,
            ["autocomplete"] = autocomplete
        }, includeClientValidationSlot: includeClientValidationSlot);

    public IReadOnlyDictionary<string, object> AsNumberInput(
        string? min = null,
        string? max = null,
        string? step = null,
        string? placeholder = null,
        string? inputMode = null,
        bool includeClientValidationSlot = true) =>
        BuildControlAttributes(new Dictionary<string, object?>
        {
            ["type"] = "number",
            ["value"] = Value ?? string.Empty,
            ["min"] = min,
            ["max"] = max,
            ["step"] = step,
            ["placeholder"] = placeholder,
            ["inputmode"] = inputMode
        }, includeClientValidationSlot: includeClientValidationSlot);

    public IReadOnlyDictionary<string, object> AsCheckbox(
        bool includeClientValidationSlot = true) =>
        BuildControlAttributes(new Dictionary<string, object?>
        {
            ["type"] = "checkbox",
            ["value"] = "true"
        }, includeClientValidationSlot: includeClientValidationSlot);

    public IReadOnlyDictionary<string, object> AsSelect(
        bool multiple = false,
        bool includeClientValidationSlot = true) =>
        BuildControlAttributes(new Dictionary<string, object?>
        {
            ["multiple"] = multiple
        }, includeClientValidationSlot: includeClientValidationSlot);

    internal IReadOnlyDictionary<string, object> BuildControlAttributes(
        IReadOnlyDictionary<string, object?>? elementAttributes = null,
        string? cssClass = null,
        IReadOnlyDictionary<string, object?>? additionalAttributes = null,
        bool includeClientValidationSlot = true)
    {
        var includeClientSlot = includeClientValidationSlot && EnableClientValidation;
        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["id"] = InputId,
            ["name"] = Name,
            ["aria-invalid"] = HasErrors ? "true" : "false",
            ["aria-describedby"] = BuildAriaDescribedBy(includeClientSlot),
            ["data-hrz-field-path"] = FieldPath.Value,
            ["data-hrz-server-slot-id"] = ServerSlotId
        };

        if (HasLiveValidation)
        {
            attributes["data-hrz-live-policy-id"] = LivePolicyId;
            attributes["data-hrz-summary-slot-id"] = Form.SummaryId;
            attributes["hx-disinherit"] = "*";
            attributes["hx-post"] = LiveValidationPath!;
            attributes["hx-trigger"] = LiveTrigger!;
            attributes["hx-target"] = $"#{ServerSlotId}";
            attributes["hx-swap"] = "outerHTML";
            attributes["hx-include"] = LiveInclude!;
            attributes["hx-sync"] = LiveSync!;
            attributes["hx-vals"] = LiveValidationValuesJson!;
        }

        foreach (var (key, value) in ClientValidationAttributes)
        {
            attributes[key] = value;
        }

        if (includeClientSlot)
        {
            attributes["data-hrz-client-slot-id"] = ClientSlotId;
        }

        MergeAttributes(attributes, elementAttributes);
        MergeAttributes(attributes, additionalAttributes);
        MergeClassValue(attributes, cssClass, elementAttributes, additionalAttributes);

        return attributes;
    }

    private string BuildAriaDescribedBy(bool includeClientSlot)
    {
        return includeClientSlot
            ? $"{ClientSlotId} {ServerSlotId}"
            : ServerSlotId;
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

    private static void MergeClassValue(
        IDictionary<string, object> attributes,
        string? cssClass,
        IReadOnlyDictionary<string, object?>? elementAttributes,
        IReadOnlyDictionary<string, object?>? additionalAttributes)
    {
        var mergedClass = string.Join(
            " ",
            new[]
            {
                cssClass,
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

public sealed class HrzFieldView<TValue> : HrzFieldView
{
    internal HrzFieldView(
        HrzFormView form,
        HrzFieldDescriptor descriptor,
        HrzFieldValueProjection projection,
        IReadOnlyDictionary<string, string> clientValidationAttributes,
        HrzResolvedLiveValidation live,
        TValue? currentValue)
        : base(form, descriptor, projection, clientValidationAttributes, live)
    {
        CurrentValue = currentValue;
    }

    public TValue? CurrentValue { get; }
}
