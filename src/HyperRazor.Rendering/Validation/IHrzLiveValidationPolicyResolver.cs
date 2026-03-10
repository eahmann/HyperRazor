namespace HyperRazor.Rendering;

public interface IHrzLiveValidationPolicyResolver
{
    Task<HrzLiveValidationPolicy> ResolveAsync<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default);
}
