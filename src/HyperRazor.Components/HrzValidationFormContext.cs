using HyperRazor.Components.Validation;

namespace HyperRazor.Components;

internal sealed class HrzValidationFormContext
{
    private readonly Dictionary<string, HrzValidationFieldContext> _registeredLiveFields = new(StringComparer.Ordinal);

    public object Model { get; set; } = default!;

    public HrzValidationRootId RootId { get; set; } = default!;

    public string IdPrefix { get; set; } = string.Empty;

    public HrzSubmitValidationState? ValidationState { get; set; }

    public bool EnableClientValidation { get; set; }

    public IReadOnlyList<IHrzClientValidationMetadataProvider> ClientValidationMetadataProviders { get; set; } =
        Array.Empty<IHrzClientValidationMetadataProvider>();

    public string? LiveValidationPath { get; set; }

    public string LiveTrigger { get; set; } = "input changed delay:400ms, blur";

    public string LiveInclude { get; set; } = "closest form";

    public string LiveSync { get; set; } = "closest form:abort";

    public Action? RegistrationChanged { get; set; }

    public string SummaryId => $"{IdPrefix}-server-summary";

    public string FormId => $"{IdPrefix}-form";

    public string LivePolicyRegionId => $"{IdPrefix}-live-policies";

    public IReadOnlyList<HrzValidationFieldContext> RegisteredLiveFields =>
        _registeredLiveFields.Values
            .OrderBy(static fieldContext => fieldContext.FieldPath.Value, StringComparer.Ordinal)
            .ToArray();

    public void RegisterField(HrzValidationFieldContext fieldContext)
    {
        ArgumentNullException.ThrowIfNull(fieldContext);

        if (!fieldContext.HasLiveValidation)
        {
            UnregisterField(fieldContext.FieldPath);
            return;
        }

        var key = fieldContext.FieldPath.Value;
        if (_registeredLiveFields.TryGetValue(key, out var existing)
            && existing.LivePolicyId == fieldContext.LivePolicyId
            && existing.LiveValidationPath == fieldContext.LiveValidationPath
            && existing.LiveTrigger == fieldContext.LiveTrigger
            && existing.LiveInclude == fieldContext.LiveInclude
            && existing.LiveSync == fieldContext.LiveSync)
        {
            _registeredLiveFields[key] = fieldContext;
            return;
        }

        _registeredLiveFields[key] = fieldContext;
        RegistrationChanged?.Invoke();
    }

    public void UnregisterField(HrzFieldPath fieldPath)
    {
        ArgumentNullException.ThrowIfNull(fieldPath);

        if (_registeredLiveFields.Remove(fieldPath.Value))
        {
            RegistrationChanged?.Invoke();
        }
    }
}
