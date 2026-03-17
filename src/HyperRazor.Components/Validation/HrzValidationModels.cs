namespace HyperRazor.Components.Validation;

public sealed record HrzSubmitValidationState(
    HrzValidationRootId RootId,
    IReadOnlyList<string> SummaryErrors,
    IReadOnlyDictionary<HrzFieldPath, IReadOnlyList<string>> FieldErrors,
    IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> AttemptedValues)
{
    public bool IsValid =>
        SummaryErrors.Count == 0
        && FieldErrors.Values.All(messages => messages.Count == 0);
}

public sealed record HrzLiveValidationPatch(
    HrzValidationRootId RootId,
    IReadOnlyList<HrzFieldPath> AffectedFields,
    IReadOnlyDictionary<HrzFieldPath, IReadOnlyList<string>> FieldErrors,
    bool ReplaceSummary,
    IReadOnlyList<string> SummaryErrors);

public sealed record HrzLiveValidationPolicy(
    bool Enabled,
    IReadOnlyList<HrzFieldPath> DependsOn,
    IReadOnlyList<HrzFieldPath> AffectedFields,
    IReadOnlyList<HrzFieldPath> ClearFields,
    bool ReplaceSummaryWhenDisabled,
    bool ImmediateRecheckWhenEnabled);

public sealed record HrzLiveValidationRequest(
    HrzValidationRootId RootId,
    bool ValidateAll,
    IReadOnlyList<HrzFieldPath> Fields);

public sealed record HrzFormPostState<TModel>(
    TModel Model,
    HrzSubmitValidationState ValidationState);
