using HyperRazor.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Mvc;

public static class HrzValidationProblemDetailsExtensions
{
    public static HrzSubmitValidationState ToSubmitValidationState(
        this ValidationProblemDetails problemDetails,
        HrzValidationRootId rootId,
        IHrzFieldPathResolver fieldPathResolver,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue>? attemptedValues = null)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(fieldPathResolver);

        var summaryErrors = new List<string>();
        var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();

        foreach (var error in problemDetails.Errors)
        {
            if (string.IsNullOrWhiteSpace(error.Key))
            {
                summaryErrors.AddRange(error.Value.Where(static value => !string.IsNullOrWhiteSpace(value)));
                continue;
            }

            var path = fieldPathResolver.FromFieldName(error.Key);
            fieldErrors[path] = error.Value
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        return new HrzSubmitValidationState(
            rootId,
            summaryErrors,
            fieldErrors,
            attemptedValues ?? new Dictionary<HrzFieldPath, HrzAttemptedValue>());
    }
}
