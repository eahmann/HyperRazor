using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public sealed class DemoInviteLiveValidationPolicyResolver : IHrzLiveValidationPolicyResolver
{
    private static readonly HrzFieldPath DisplayNamePath = HrzFieldPaths.FromFieldName(nameof(InviteUserInput.DisplayName));
    private static readonly HrzFieldPath EmailPath = HrzFieldPaths.FromFieldName(nameof(InviteUserInput.Email));

    public Task<HrzLiveValidationPolicy> ResolveAsync<TModel>(
        TModel model,
        HrzValidationRootId rootId,
        HrzFieldPath fieldPath,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rootId);
        ArgumentNullException.ThrowIfNull(fieldPath);
        ArgumentNullException.ThrowIfNull(attemptedValues);

        cancellationToken.ThrowIfCancellationRequested();

        if (model is not InviteUserInput invite
            || rootId != UserInviteValidationRoots.MinimalProxy)
        {
            return Task.FromResult(DefaultEnabledPolicy());
        }

        var email = invite.Email?.Trim();
        var requiresTeamDisplayName = string.Equals(
            email,
            "shared-mailbox@example.com",
            StringComparison.OrdinalIgnoreCase);

        if (fieldPath.Equals(EmailPath))
        {
            return Task.FromResult(new HrzLiveValidationPolicy(
                Enabled: true,
                DependsOn: [EmailPath, DisplayNamePath],
                AffectedFields: [EmailPath, DisplayNamePath],
                ClearFields: [EmailPath, DisplayNamePath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: false));
        }

        if (fieldPath.Equals(DisplayNamePath))
        {
            return Task.FromResult(new HrzLiveValidationPolicy(
                Enabled: requiresTeamDisplayName,
                DependsOn: [EmailPath, DisplayNamePath],
                AffectedFields: [DisplayNamePath],
                ClearFields: [DisplayNamePath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: true));
        }

        return Task.FromResult(DefaultEnabledPolicy());
    }

    private static HrzLiveValidationPolicy DefaultEnabledPolicy()
    {
        return new HrzLiveValidationPolicy(
            Enabled: true,
            DependsOn: Array.Empty<HrzFieldPath>(),
            AffectedFields: Array.Empty<HrzFieldPath>(),
            ClearFields: Array.Empty<HrzFieldPath>(),
            ReplaceSummaryWhenDisabled: false,
            ImmediateRecheckWhenEnabled: false);
    }
}
