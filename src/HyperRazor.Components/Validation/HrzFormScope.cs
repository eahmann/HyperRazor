using System.Linq.Expressions;

namespace HyperRazor.Components.Validation;

public abstract class HrzFormScope
{
    private readonly Dictionary<string, HrzFieldScope> _registeredLiveFields = new(StringComparer.Ordinal);
    private readonly IHrzValidationScopeFactory _scopeFactory;

    internal HrzFormScope(
        object model,
        HrzValidationRootId rootId,
        string idPrefix,
        HrzSubmitValidationState? validationState,
        bool enableClientValidation,
        HrzLiveValidationOptions? live,
        IHrzValidationScopeFactory scopeFactory)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(scopeFactory);

        if (validationState is not null && validationState.RootId != rootId)
        {
            throw new InvalidOperationException(
                $"The provided validation state root '{validationState.RootId.Value}' does not match '{rootId.Value}'.");
        }

        Model = model;
        RootId = rootId;
        IdPrefix = string.IsNullOrWhiteSpace(idPrefix) ? rootId.Value : idPrefix;
        ValidationState = validationState;
        EnableClientValidation = enableClientValidation;
        Live = live;
        _scopeFactory = scopeFactory;
    }

    public object Model { get; }

    public HrzValidationRootId RootId { get; }

    public string IdPrefix { get; }

    public HrzSubmitValidationState? ValidationState { get; }

    public bool EnableClientValidation { get; }

    public HrzLiveValidationOptions? Live { get; }

    public string FormId => $"{IdPrefix}-form";

    public string SummaryId => $"{IdPrefix}-server-summary";

    public string LivePolicyRegionId => $"{IdPrefix}-live-policies";

    internal IReadOnlyList<HrzFieldScope> RegisteredLiveFields =>
        _registeredLiveFields.Values
            .OrderBy(static fieldView => fieldView.FieldPath.Value, StringComparer.Ordinal)
            .ToArray();

    internal event Action? RegistrationChanged;

    public HrzFieldScope<TValue> Field<TValue>(
        Expression<Func<TValue>> accessor,
        string? label = null,
        bool? enableClientValidation = null,
        HrzFieldLiveOptions? live = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return Field(
            accessor,
            accessor.Compile(),
            label,
            enableClientValidation,
            live);
    }

    public IReadOnlyDictionary<string, object> FormAttributes(
        string action,
        string? hxPost = null,
        string? hxTarget = null,
        string? hxSwap = "outerHTML focus-scroll:true",
        string? disabledElementSelector = "find button",
        string? cssClass = null,
        IReadOnlyDictionary<string, object?>? additionalAttributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        var attributes = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["id"] = FormId,
            ["action"] = action,
            ["data-hrz-validation-root"] = RootId.Value,
            ["data-hrz-validation-region"] = true,
            ["novalidate"] = true
        };

        if (!string.IsNullOrWhiteSpace(disabledElementSelector))
        {
            attributes["data-hrz-disabled-elt"] = disabledElementSelector;
        }

        if (!string.IsNullOrWhiteSpace(hxPost))
        {
            attributes["hx-post"] = hxPost;
        }

        if (!string.IsNullOrWhiteSpace(hxTarget))
        {
            attributes["hx-target"] = hxTarget;
        }

        if (!string.IsNullOrWhiteSpace(hxSwap))
        {
            attributes["hx-swap"] = hxSwap;
        }

        MergeAttributes(attributes, additionalAttributes);
        MergeClassValue(attributes, cssClass, additionalAttributes);

        return attributes;
    }

    internal HrzFieldScope<TValue> Field<TValue>(
        Expression<Func<TValue>> accessor,
        Func<TValue> compiledAccessor,
        string? label,
        bool? enableClientValidation,
        HrzFieldLiveOptions? live)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(compiledAccessor);

        var field = _scopeFactory.CreateFieldScope(this, accessor, compiledAccessor, label, enableClientValidation, live);
        if (field.HasLiveValidation)
        {
            RegisterField(field);
        }
        else
        {
            UnregisterField(field.FieldPath);
        }

        return field;
    }

    internal void UnregisterField(HrzFieldPath fieldPath)
    {
        ArgumentNullException.ThrowIfNull(fieldPath);

        if (_registeredLiveFields.Remove(fieldPath.Value))
        {
            RegistrationChanged?.Invoke();
        }
    }

    private void RegisterField(HrzFieldScope field)
    {
        var key = field.FieldPath.Value;
        if (_registeredLiveFields.TryGetValue(key, out var existing)
            && existing.LivePolicyId == field.LivePolicyId
            && existing.LiveValidationPath == field.LiveValidationPath
            && existing.LiveTrigger == field.LiveTrigger
            && existing.LiveInclude == field.LiveInclude
            && existing.LiveSync == field.LiveSync)
        {
            _registeredLiveFields[key] = field;
            return;
        }

        _registeredLiveFields[key] = field;
        RegistrationChanged?.Invoke();
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
        IReadOnlyDictionary<string, object?>? additionalAttributes)
    {
        var mergedClass = string.Join(
            " ",
            new[]
            {
                cssClass,
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

public sealed class HrzFormScope<TModel> : HrzFormScope
{
    internal HrzFormScope(
        TModel model,
        HrzValidationRootId rootId,
        string idPrefix,
        HrzSubmitValidationState? validationState,
        bool enableClientValidation,
        HrzLiveValidationOptions? live,
        IHrzValidationScopeFactory scopeFactory)
        : base(model!, rootId, idPrefix, validationState, enableClientValidation, live, scopeFactory)
    {
        Model = model;
    }

    public new TModel Model { get; }
}
