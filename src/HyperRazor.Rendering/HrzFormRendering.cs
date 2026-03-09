using Microsoft.Extensions.Primitives;

namespace HyperRazor.Rendering;

public static class HrzFormRendering
{
    public static string? ValueOrAttempted(
        HrzSubmitValidationState? state,
        HrzFieldPath path,
        string? currentValue)
    {
        ArgumentNullException.ThrowIfNull(path);

        return AttemptedValueFor(state, path)?.Values.FirstOrDefault() ?? currentValue;
    }

    public static StringValues ValuesOrAttempted(
        HrzSubmitValidationState? state,
        HrzFieldPath path,
        StringValues currentValues)
    {
        ArgumentNullException.ThrowIfNull(path);

        return AttemptedValueFor(state, path)?.Values ?? currentValues;
    }

    public static HrzAttemptedValue? AttemptedValueFor(HrzSubmitValidationState? state, HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (state is null)
        {
            return null;
        }

        return state.AttemptedValues.TryGetValue(path, out var attemptedValue)
            ? attemptedValue
            : null;
    }

    public static IReadOnlyList<string> ErrorsFor(HrzSubmitValidationState? state, HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (state is null)
        {
            return Array.Empty<string>();
        }

        return state.FieldErrors.TryGetValue(path, out var errors)
            ? errors
            : Array.Empty<string>();
    }

    public static bool HasErrors(HrzSubmitValidationState? state, HrzFieldPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ErrorsFor(state, path).Count > 0;
    }
}
