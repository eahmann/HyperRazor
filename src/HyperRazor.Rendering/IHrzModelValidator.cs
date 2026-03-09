namespace HyperRazor.Rendering;

public interface IHrzModelValidator
{
    HrzSubmitValidationState Validate<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue>? attemptedValues = null);
}
