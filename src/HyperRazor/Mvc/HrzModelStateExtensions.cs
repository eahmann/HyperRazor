using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Mvc;

public static class HrzModelStateExtensions
{
    public static HrzSubmitValidationState ToSubmitValidationState(
        this ModelStateDictionary modelState,
        HrzValidationRootId rootId,
        IHrzFieldPathResolver fieldPathResolver,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue>? fallbackAttemptedValues = null)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(fieldPathResolver);

        var summaryErrors = new List<string>();
        var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
        var attemptedValues = fallbackAttemptedValues is null
            ? new Dictionary<HrzFieldPath, HrzAttemptedValue>()
            : new Dictionary<HrzFieldPath, HrzAttemptedValue>(fallbackAttemptedValues);

        foreach (var entry in modelState)
        {
            var key = entry.Key;
            var state = entry.Value;
            if (state is null)
            {
                continue;
            }

            var errors = state.Errors
                .Select(static error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.Exception?.Message
                    : error.ErrorMessage)
                .Where(static message => !string.IsNullOrWhiteSpace(message))
                .Cast<string>()
                .ToArray();

            if (string.IsNullOrWhiteSpace(key))
            {
                summaryErrors.AddRange(errors);
                continue;
            }

            var path = fieldPathResolver.FromFieldName(key);
            if (errors.Length > 0)
            {
                fieldErrors[path] = errors;
            }

            var attemptedValue = CreateAttemptedValue(state, attemptedValues.TryGetValue(path, out var existing) ? existing : null);
            if (attemptedValue is not null)
            {
                attemptedValues[path] = attemptedValue;
            }
        }

        return new HrzSubmitValidationState(rootId, summaryErrors, fieldErrors, attemptedValues);
    }

    private static HrzAttemptedValue? CreateAttemptedValue(ModelStateEntry state, HrzAttemptedValue? fallback)
    {
        var stringValues = state.RawValue switch
        {
            StringValues values => values,
            string value => new StringValues(value),
            string[] values => new StringValues(values),
            IEnumerable<string> values => new StringValues(values.ToArray()),
            not null when state.AttemptedValue is not null => new StringValues(state.AttemptedValue),
            _ => fallback?.Values ?? StringValues.Empty
        };

        if (stringValues.Count == 0 && (fallback?.Files.Count ?? 0) == 0)
        {
            return null;
        }

        return new HrzAttemptedValue(stringValues, fallback?.Files ?? Array.Empty<HrzAttemptedFile>());
    }
}
