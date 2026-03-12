namespace HyperRazor.Rendering;

/// <summary>
/// Default live-policy resolver that preserves the current always-armed behavior
/// until an application opts into a field-specific live-policy contract.
/// </summary>
public sealed class HrzDefaultLiveValidationPolicyResolver : IHrzLiveValidationPolicyResolver
{
    public Task<HrzLiveValidationPolicy> ResolveAsync(
        object model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(fieldPath);
        ArgumentNullException.ThrowIfNull(attemptedValues);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new HrzLiveValidationPolicy(
            Enabled: true,
            DependsOn: Array.Empty<HrzFieldPath>(),
            AffectedFields: Array.Empty<HrzFieldPath>(),
            ClearFields: Array.Empty<HrzFieldPath>(),
            ReplaceSummaryWhenDisabled: false,
            ImmediateRecheckWhenEnabled: false));
    }
}
