namespace HyperRazor.Rendering;

public interface IHrzLiveValidationPolicyResolver
{
    Task<HrzLiveValidationPolicy> ResolveAsync(
        object model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default);
}
