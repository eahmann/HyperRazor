using HyperRazor.Components;
using HyperRazor.Components.Validation;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Models;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Demo.Mvc.Endpoints;

internal static class DemoValidationSupport
{
    public static HrzFieldPath? ResolveInvitePrimaryField(HrzValidationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.ValidateAll
            ? UserInviteValidationForm.EmailPath
            : scope.Fields.FirstOrDefault(static field =>
                field.Equals(UserInviteValidationForm.EmailPath)
                || field.Equals(UserInviteValidationForm.DisplayNamePath));
    }

    public static HrzFieldPath? ResolveMixedPrimaryField(HrzValidationScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return scope.ValidateAll
            ? MixedValidationAuthoringForm.SeatCountPath
            : scope.Fields.FirstOrDefault(static field =>
                field.Equals(MixedValidationAuthoringForm.EnvironmentPath)
                || field.Equals(MixedValidationAuthoringForm.RequiresApprovalPath)
                || field.Equals(MixedValidationAuthoringForm.SeatCountPath));
    }

    public static async Task<IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy>> ResolveLivePoliciesAsync(
        IHrzLiveValidationPolicyResolver livePolicyResolver,
        object model,
        HrzValidationRootId rootId,
        HrzFieldPath primaryField,
        HrzLiveValidationPolicy primaryPolicy,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(livePolicyResolver);
        ArgumentNullException.ThrowIfNull(model);

        var policies = new Dictionary<HrzFieldPath, HrzLiveValidationPolicy>
        {
            [primaryField] = primaryPolicy
        };

        foreach (var affectedField in primaryPolicy.AffectedFields.Where(field => !field.Equals(primaryField)).Distinct())
        {
            policies[affectedField] = await livePolicyResolver.ResolveAsync(
                model,
                rootId,
                affectedField,
                attemptedValues,
                cancellationToken);
        }

        return policies;
    }

    public static async Task<HrzSubmitValidationState> BuildMixedSubmitValidationStateAsync(
        IHrzModelValidator modelValidator,
        IHrzLiveValidationPolicyResolver livePolicyResolver,
        HrzFormPostState<MixedValidationInput> formPostState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(modelValidator);
        ArgumentNullException.ThrowIfNull(livePolicyResolver);
        ArgumentNullException.ThrowIfNull(formPostState);

        var rootId = UserInviteValidationRoots.MixedAuthoring;
        var validationState = formPostState.ValidationState.Merge(
            modelValidator.Validate(
                formPostState.Model,
                rootId,
                formPostState.ValidationState.AttemptedValues));
        var primaryField = MixedValidationAuthoringForm.SeatCountPath;
        var primaryPolicy = await livePolicyResolver.ResolveAsync(
            formPostState.Model,
            rootId,
            primaryField,
            validationState.AttemptedValues,
            cancellationToken);
        var resolvedPolicies = await ResolveLivePoliciesAsync(
            livePolicyResolver,
            formPostState.Model,
            rootId,
            primaryField,
            primaryPolicy,
            validationState.AttemptedValues,
            cancellationToken);
        var livePatch = BuildMixedLiveValidationPatch(
            new HrzValidationScope(rootId, ValidateAll: true, Fields: [primaryField]),
            primaryField,
            formPostState.Model,
            resolvedPolicies);

        return validationState.Merge(ToSubmitValidationState(livePatch, validationState.AttemptedValues));
    }

    public static HrzLiveValidationPatch BuildInviteLiveValidationPatch(
        HrzValidationScope scope,
        HrzFieldPath primaryField,
        InviteUserInput input,
        IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy> resolvedPolicies)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(resolvedPolicies);

        var primaryPolicy = resolvedPolicies[primaryField];
        var affectedFields = primaryPolicy.AffectedFields
            .Append(primaryField)
            .Distinct()
            .ToArray();
        var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
        var summaryErrors = new List<string>();
        var email = input.Email?.Trim();
        var displayName = input.DisplayName?.Trim();
        var requiresTeamDisplayName = string.Equals(email, "shared-mailbox@example.com", StringComparison.OrdinalIgnoreCase);

        foreach (var field in affectedFields)
        {
            if (field.Equals(UserInviteValidationForm.EmailPath))
            {
                var emailErrors = resolvedPolicies[field].Enabled
                    && !string.IsNullOrWhiteSpace(email)
                    && string.Equals(email, "backend-taken@example.com", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "Email already exists in the upstream directory." }
                    : Array.Empty<string>();
                fieldErrors[field] = emailErrors;

                if (emailErrors.Length > 0)
                {
                    summaryErrors.Add("Backend would reject this invite on submit.");
                }
            }

            if (field.Equals(UserInviteValidationForm.DisplayNamePath))
            {
                var displayNameErrors = resolvedPolicies[field].Enabled
                    && requiresTeamDisplayName
                    && IsEligibleForDisplayNameServerValidation(displayName)
                    && !displayName!.Contains("team", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "Shared mailbox invites must use a team display name." }
                    : Array.Empty<string>();
                fieldErrors[field] = displayNameErrors;

                if (displayNameErrors.Length > 0)
                {
                    summaryErrors.Add("Shared mailbox invites need a team display name before the backend will accept them.");
                }
            }
        }

        return new HrzLiveValidationPatch(
            scope.RootId,
            affectedFields,
            fieldErrors,
            ReplaceSummary: true,
            SummaryErrors: summaryErrors);
    }

    public static HrzLiveValidationPatch BuildMixedLiveValidationPatch(
        HrzValidationScope scope,
        HrzFieldPath primaryField,
        MixedValidationInput input,
        IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy> resolvedPolicies)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(resolvedPolicies);

        var primaryPolicy = resolvedPolicies[primaryField];
        var affectedFields = primaryPolicy.AffectedFields
            .Append(primaryField)
            .Distinct()
            .ToArray();
        var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
        var summaryErrors = new List<string>();
        var isProduction = string.Equals(input.Environment, "production", StringComparison.OrdinalIgnoreCase);
        var requiresApproval = input.RequiresApproval;

        foreach (var field in affectedFields)
        {
            if (field.Equals(MixedValidationAuthoringForm.EnvironmentPath)
                || field.Equals(MixedValidationAuthoringForm.RequiresApprovalPath))
            {
                fieldErrors[field] = Array.Empty<string>();
            }

            if (field.Equals(MixedValidationAuthoringForm.SeatCountPath))
            {
                var seatCountErrors = resolvedPolicies[field].Enabled
                    && isProduction
                    && input.SeatCount > 10
                    && !requiresApproval
                    ? new[] { "Production rollouts above 10 seats require approval." }
                    : Array.Empty<string>();
                fieldErrors[field] = seatCountErrors;

                if (seatCountErrors.Length > 0)
                {
                    summaryErrors.Add("Approval is required before a production rollout can exceed 10 seats.");
                }
            }
        }

        return new HrzLiveValidationPatch(
            scope.RootId,
            affectedFields,
            fieldErrors,
            ReplaceSummary: true,
            SummaryErrors: summaryErrors);
    }

    public static IReadOnlyList<string> GetFieldErrors(HrzLiveValidationPatch patch, HrzFieldPath fieldPath)
    {
        ArgumentNullException.ThrowIfNull(patch);

        return patch.FieldErrors.TryGetValue(fieldPath, out var messages)
            ? messages
            : Array.Empty<string>();
    }

    public static HrzSubmitValidationState ToSubmitValidationState(
        HrzLiveValidationPatch patch,
        IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues)
    {
        ArgumentNullException.ThrowIfNull(patch);

        return new HrzSubmitValidationState(
            patch.RootId,
            patch.SummaryErrors,
            patch.FieldErrors,
            attemptedValues);
    }

    public static RenderFragment BuildFieldSlotFragment(
        InviteValidationFormViewModel form,
        HrzFieldPath fieldPath,
        IReadOnlyList<string> errors,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        var slotId = fieldPath.Equals(UserInviteValidationForm.DisplayNamePath)
            ? UserInviteValidationForm.GetDisplayNameServerId(form.IdPrefix)
            : UserInviteValidationForm.GetEmailServerId(form.IdPrefix);

        return builder =>
        {
            builder.OpenComponent<HrzValidationServerFieldSlot>(0);
            builder.AddAttribute(1, nameof(HrzValidationServerFieldSlot.Id), slotId);
            builder.AddAttribute(2, nameof(HrzValidationServerFieldSlot.FieldPath), fieldPath.Value);
            builder.AddAttribute(3, nameof(HrzValidationServerFieldSlot.Errors), errors);
            builder.AddAttribute(4, nameof(HrzValidationServerFieldSlot.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    public static RenderFragment BuildSummarySlotFragment(
        InviteValidationFormViewModel form,
        IReadOnlyList<string> errors,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        return builder =>
        {
            builder.OpenComponent<HrzValidationServerSummarySlot>(0);
            builder.AddAttribute(1, nameof(HrzValidationServerSummarySlot.Id), UserInviteValidationForm.GetSummaryId(form.IdPrefix));
            builder.AddAttribute(2, nameof(HrzValidationServerSummarySlot.Errors), errors);
            builder.AddAttribute(3, nameof(HrzValidationServerSummarySlot.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    public static RenderFragment BuildLivePolicyCarrierFragment(
        InviteValidationFormViewModel form,
        HrzFieldPath fieldPath,
        HrzLiveValidationPolicy policy,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        var carrierId = fieldPath.Equals(UserInviteValidationForm.DisplayNamePath)
            ? UserInviteValidationForm.GetDisplayNameLivePolicyId(form.IdPrefix)
            : UserInviteValidationForm.GetEmailLivePolicyId(form.IdPrefix);

        return builder =>
        {
            builder.OpenComponent<HrzValidationLivePolicyCarrier>(0);
            builder.AddAttribute(1, nameof(HrzValidationLivePolicyCarrier.Id), carrierId);
            builder.AddAttribute(2, nameof(HrzValidationLivePolicyCarrier.Policy), policy);
            builder.AddAttribute(3, nameof(HrzValidationLivePolicyCarrier.SummarySlotId), UserInviteValidationForm.GetSummaryId(form.IdPrefix));
            builder.AddAttribute(4, nameof(HrzValidationLivePolicyCarrier.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    public static RenderFragment BuildMixedFieldSlotFragment(
        MixedValidationFormViewModel form,
        HrzFieldPath fieldPath,
        IReadOnlyList<string> errors,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        var slotId = fieldPath.Equals(MixedValidationAuthoringForm.EnvironmentPath)
            ? MixedValidationAuthoringForm.GetEnvironmentServerId(form.IdPrefix)
            : fieldPath.Equals(MixedValidationAuthoringForm.RequiresApprovalPath)
                ? MixedValidationAuthoringForm.GetRequiresApprovalServerId(form.IdPrefix)
                : MixedValidationAuthoringForm.GetSeatCountServerId(form.IdPrefix);

        return builder =>
        {
            builder.OpenComponent<HrzValidationServerFieldSlot>(0);
            builder.AddAttribute(1, nameof(HrzValidationServerFieldSlot.Id), slotId);
            builder.AddAttribute(2, nameof(HrzValidationServerFieldSlot.FieldPath), fieldPath.Value);
            builder.AddAttribute(3, nameof(HrzValidationServerFieldSlot.Errors), errors);
            builder.AddAttribute(4, nameof(HrzValidationServerFieldSlot.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    public static RenderFragment BuildMixedSummarySlotFragment(
        MixedValidationFormViewModel form,
        IReadOnlyList<string> errors,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        return builder =>
        {
            builder.OpenComponent<HrzValidationServerSummarySlot>(0);
            builder.AddAttribute(1, nameof(HrzValidationServerSummarySlot.Id), MixedValidationAuthoringForm.GetSummaryId(form.IdPrefix));
            builder.AddAttribute(2, nameof(HrzValidationServerSummarySlot.Errors), errors);
            builder.AddAttribute(3, nameof(HrzValidationServerSummarySlot.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    public static RenderFragment BuildMixedLivePolicyCarrierFragment(
        MixedValidationFormViewModel form,
        HrzFieldPath fieldPath,
        HrzLiveValidationPolicy policy,
        bool swapOob)
    {
        ArgumentNullException.ThrowIfNull(form);

        var carrierId = fieldPath.Equals(MixedValidationAuthoringForm.EnvironmentPath)
            ? MixedValidationAuthoringForm.GetEnvironmentLivePolicyId(form.IdPrefix)
            : fieldPath.Equals(MixedValidationAuthoringForm.RequiresApprovalPath)
                ? MixedValidationAuthoringForm.GetRequiresApprovalLivePolicyId(form.IdPrefix)
                : MixedValidationAuthoringForm.GetSeatCountLivePolicyId(form.IdPrefix);

        return builder =>
        {
            builder.OpenComponent<HrzValidationLivePolicyCarrier>(0);
            builder.AddAttribute(1, nameof(HrzValidationLivePolicyCarrier.Id), carrierId);
            builder.AddAttribute(2, nameof(HrzValidationLivePolicyCarrier.Policy), policy);
            builder.AddAttribute(3, nameof(HrzValidationLivePolicyCarrier.SummarySlotId), MixedValidationAuthoringForm.GetSummaryId(form.IdPrefix));
            builder.AddAttribute(4, nameof(HrzValidationLivePolicyCarrier.SwapOob), swapOob);
            builder.CloseComponent();
        };
    }

    private static bool IsEligibleForDisplayNameServerValidation(string? displayName)
    {
        return !string.IsNullOrWhiteSpace(displayName)
            && displayName.Trim().Length >= 3;
    }
}
