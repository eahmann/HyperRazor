using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Components.Validation;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public sealed class DemoValidationLivePolicyResolver : IHrzLiveValidationPolicyResolver
{
    private static readonly HrzFieldPath DisplayNamePath = HrzFieldPaths.FromFieldName(nameof(InviteUserInput.DisplayName));
    private static readonly HrzFieldPath EmailPath = HrzFieldPaths.FromFieldName(nameof(InviteUserInput.Email));
    private static readonly HrzFieldPath EnvironmentPath = HrzFieldPaths.FromFieldName(nameof(MixedValidationInput.Environment));
    private static readonly HrzFieldPath RequiresApprovalPath = HrzFieldPaths.FromFieldName(nameof(MixedValidationInput.RequiresApproval));
    private static readonly HrzFieldPath SeatCountPath = HrzFieldPaths.FromFieldName(nameof(MixedValidationInput.SeatCount));

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

        if (model is InviteUserInput invite
            && rootId == UserInviteValidationRoots.MinimalProxy)
        {
            return Task.FromResult(ResolveInvitePolicy(invite, fieldPath));
        }

        if (model is MixedValidationInput mixed
            && rootId == UserInviteValidationRoots.MixedAuthoring)
        {
            return Task.FromResult(ResolveMixedPolicy(mixed, fieldPath));
        }

        return Task.FromResult(DefaultEnabledPolicy());
    }

    private static HrzLiveValidationPolicy ResolveInvitePolicy(InviteUserInput invite, HrzFieldPath fieldPath)
    {
        var email = invite.Email?.Trim();
        var requiresTeamDisplayName = string.Equals(
            email,
            "shared-mailbox@example.com",
            StringComparison.OrdinalIgnoreCase);

        if (fieldPath.Equals(EmailPath))
        {
            return new HrzLiveValidationPolicy(
                Enabled: true,
                DependsOn: [EmailPath, DisplayNamePath],
                AffectedFields: [EmailPath, DisplayNamePath],
                ClearFields: [EmailPath, DisplayNamePath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: false);
        }

        if (fieldPath.Equals(DisplayNamePath))
        {
            return new HrzLiveValidationPolicy(
                Enabled: requiresTeamDisplayName,
                DependsOn: [EmailPath, DisplayNamePath],
                AffectedFields: [DisplayNamePath],
                ClearFields: [DisplayNamePath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: true);
        }

        return DefaultEnabledPolicy();
    }

    private static HrzLiveValidationPolicy ResolveMixedPolicy(MixedValidationInput mixed, HrzFieldPath fieldPath)
    {
        var isProduction = string.Equals(mixed.Environment, "production", StringComparison.OrdinalIgnoreCase);

        if (fieldPath.Equals(EnvironmentPath))
        {
            return new HrzLiveValidationPolicy(
                Enabled: true,
                DependsOn: [EnvironmentPath, RequiresApprovalPath, SeatCountPath],
                AffectedFields: [EnvironmentPath, RequiresApprovalPath, SeatCountPath],
                ClearFields: [SeatCountPath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: false);
        }

        if (fieldPath.Equals(RequiresApprovalPath))
        {
            return new HrzLiveValidationPolicy(
                Enabled: true,
                DependsOn: [EnvironmentPath, RequiresApprovalPath, SeatCountPath],
                AffectedFields: [RequiresApprovalPath, SeatCountPath],
                ClearFields: [SeatCountPath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: false);
        }

        if (fieldPath.Equals(SeatCountPath))
        {
            return new HrzLiveValidationPolicy(
                Enabled: isProduction,
                DependsOn: [EnvironmentPath, RequiresApprovalPath, SeatCountPath],
                AffectedFields: [SeatCountPath],
                ClearFields: [SeatCountPath],
                ReplaceSummaryWhenDisabled: true,
                ImmediateRecheckWhenEnabled: true);
        }

        return DefaultEnabledPolicy();
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
