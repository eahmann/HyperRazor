namespace HyperRazor.Rendering;

public static class HrzSubmitValidationStateExtensions
{
    public static HrzSubmitValidationState Merge(
        this HrzSubmitValidationState state,
        HrzSubmitValidationState other)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(other);

        if (state.RootId != other.RootId)
        {
            throw new InvalidOperationException(
                $"Cannot merge validation states with different roots ('{state.RootId.Value}' and '{other.RootId.Value}').");
        }

        var summaryErrors = state.SummaryErrors
            .Concat(other.SummaryErrors)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
        MergeFieldErrors(fieldErrors, state.FieldErrors);
        MergeFieldErrors(fieldErrors, other.FieldErrors);

        var attemptedValues = new Dictionary<HrzFieldPath, HrzAttemptedValue>(state.AttemptedValues);
        foreach (var attemptedValue in other.AttemptedValues)
        {
            attemptedValues.TryAdd(attemptedValue.Key, attemptedValue.Value);
        }

        return new HrzSubmitValidationState(state.RootId, summaryErrors, fieldErrors, attemptedValues);
    }

    private static void MergeFieldErrors(
        IDictionary<HrzFieldPath, IReadOnlyList<string>> target,
        IReadOnlyDictionary<HrzFieldPath, IReadOnlyList<string>> source)
    {
        foreach (var fieldError in source)
        {
            if (!target.TryGetValue(fieldError.Key, out var existing))
            {
                target[fieldError.Key] = fieldError.Value.Distinct(StringComparer.Ordinal).ToArray();
                continue;
            }

            target[fieldError.Key] = existing
                .Concat(fieldError.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
