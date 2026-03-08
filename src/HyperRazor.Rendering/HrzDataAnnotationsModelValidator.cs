using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Rendering;

public sealed class HrzDataAnnotationsModelValidator : IHrzModelValidator
{
    private readonly IHrzFieldPathResolver _fieldPathResolver;

    public HrzDataAnnotationsModelValidator(IHrzFieldPathResolver fieldPathResolver)
    {
        _fieldPathResolver = fieldPathResolver ?? throw new ArgumentNullException(nameof(fieldPathResolver));
    }

    public HrzSubmitValidationState Validate<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue>? attemptedValues = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(rootId);

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);
        if (model is IValidatableObject validatableObject)
        {
            validationResults.AddRange(validatableObject.Validate(validationContext));
        }

        var summaryErrors = new List<string>();
        var fieldErrors = new Dictionary<HrzFieldPath, List<string>>();

        foreach (var validationResult in validationResults)
        {
            if (validationResult == ValidationResult.Success || string.IsNullOrWhiteSpace(validationResult.ErrorMessage))
            {
                continue;
            }

            var memberNames = validationResult.MemberNames
                .Where(static memberName => !string.IsNullOrWhiteSpace(memberName))
                .ToArray();

            if (memberNames.Length == 0)
            {
                if (!summaryErrors.Contains(validationResult.ErrorMessage, StringComparer.Ordinal))
                {
                    summaryErrors.Add(validationResult.ErrorMessage);
                }

                continue;
            }

            foreach (var memberName in memberNames)
            {
                var fieldPath = _fieldPathResolver.FromFieldName(memberName);
                if (!fieldErrors.TryGetValue(fieldPath, out var errors))
                {
                    errors = new List<string>();
                    fieldErrors[fieldPath] = errors;
                }

                if (!errors.Contains(validationResult.ErrorMessage, StringComparer.Ordinal))
                {
                    errors.Add(validationResult.ErrorMessage);
                }
            }
        }

        return new HrzSubmitValidationState(
            rootId,
            summaryErrors,
            fieldErrors.ToDictionary(static pair => pair.Key, static pair => (IReadOnlyList<string>)pair.Value),
            attemptedValues ?? new Dictionary<HrzFieldPath, HrzAttemptedValue>());
    }
}
