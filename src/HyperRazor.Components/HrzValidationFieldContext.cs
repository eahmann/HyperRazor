using System.Linq.Expressions;
using HyperRazor.Components.Validation;

namespace HyperRazor.Components;

internal sealed class HrzValidationFieldContext
{
    public required HrzValidationFormContext Form { get; init; }

    public required HrzFieldView View { get; init; }

    public HrzFieldPath FieldPath => View.FieldPath;

    public string Name => View.Name;

    public string InputId => View.InputId;

    public string ClientSlotId => View.ClientSlotId;

    public string ServerSlotId => View.ServerSlotId;

    public string LivePolicyId => View.LivePolicyId;

    public Type ValueType => View.ValueType;

    public object? CurrentValue => View.CurrentValueUntyped;

    public HrzAttemptedValue? AttemptedValue => View.AttemptedValue;

    public string? Value => View.Value;

    public IReadOnlyList<string> Values => View.Values;

    public bool IsChecked => View.IsChecked;

    public IReadOnlyList<string> Errors => View.Errors;

    public bool HasErrors => View.HasErrors;

    public string LabelText => View.LabelText;

    public bool EnableClientValidation => View.EnableClientValidation;

    public IReadOnlyDictionary<string, string> ClientValidationAttributes => View.ClientValidationAttributes;

    public string AriaDescribedBy => View.AriaDescribedBy;

    public bool HasLiveValidation => View.HasLiveValidation;

    public string? LiveValidationPath => View.LiveValidationPath;

    public string? LiveTrigger => View.LiveTrigger;

    public string? LiveInclude => View.LiveInclude;

    public string? LiveSync => View.LiveSync;

    public string? LiveValidationValuesJson => View.LiveValidationValuesJson;

    public static HrzValidationFieldContext Create<TValue>(
        HrzValidationFormContext formContext,
        Expression<Func<TValue>> accessor,
        Func<TValue> compiledAccessor,
        string? explicitLabel,
        bool? enableClientValidationOverride,
        bool? liveOverride,
        string? liveValidationPathOverride,
        string? liveTriggerOverride,
        string? liveIncludeOverride,
        string? liveSyncOverride,
        HrzFieldLiveOptions? liveOptions = null)
    {
        ArgumentNullException.ThrowIfNull(formContext);
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(compiledAccessor);

        var resolvedLiveOptions = MergeLiveOptions(
            liveOptions,
            liveOverride,
            liveValidationPathOverride,
            liveTriggerOverride,
            liveIncludeOverride,
            liveSyncOverride);

        var fieldView = formContext.Field(
            accessor,
            compiledAccessor,
            explicitLabel,
            enableClientValidationOverride,
            resolvedLiveOptions);

        return FromView(formContext, fieldView);
    }

    public static HrzValidationFieldContext FromView(
        HrzValidationFormContext formContext,
        HrzFieldView fieldView)
    {
        ArgumentNullException.ThrowIfNull(formContext);
        ArgumentNullException.ThrowIfNull(fieldView);

        return new HrzValidationFieldContext
        {
            Form = formContext,
            View = fieldView
        };
    }

    private static HrzFieldLiveOptions? MergeLiveOptions(
        HrzFieldLiveOptions? liveOptions,
        bool? liveOverride,
        string? liveValidationPathOverride,
        string? liveTriggerOverride,
        string? liveIncludeOverride,
        string? liveSyncOverride)
    {
        if (liveOptions is null
            && liveOverride is null
            && liveValidationPathOverride is null
            && liveTriggerOverride is null
            && liveIncludeOverride is null
            && liveSyncOverride is null)
        {
            return null;
        }

        return new HrzFieldLiveOptions
        {
            Enabled = liveOverride ?? liveOptions?.Enabled,
            Path = liveValidationPathOverride ?? liveOptions?.Path,
            Trigger = liveTriggerOverride ?? liveOptions?.Trigger,
            Include = liveIncludeOverride ?? liveOptions?.Include,
            Sync = liveSyncOverride ?? liveOptions?.Sync
        };
    }
}
