using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Endpoints;

public static class DemoValidationEndpoints
{
    public static IEndpointRouteBuilder MapDemoValidationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var validation = endpoints.MapGroup("/validation");
        validation.MapPost("/minimal/local", HandleMinimalLocalAsync);
        validation.MapPost("/minimal/proxy", HandleMinimalProxyAsync);
        validation.MapPost("/mixed", HandleMixedSubmitAsync);
        validation.MapPost("/mixed/live", HandleMixedLiveAsync);
        validation.MapPost("/live", HandleInviteLiveAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleMinimalLocalAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);

        var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
            UserInviteValidationRoots.MinimalLocal,
            cancellationToken);

        if (!formPostState.ValidationState.IsValid)
        {
            context.SetSubmitValidationState(formPostState.ValidationState);
            DemoValidationFeedback.TriggerInvalid(context, formPostState.ValidationState);
            DemoInspectorUpdates.Queue(
                context,
                action: "validation-minimal-local-invalid",
                details: $"Minimal API local validation failed with {DemoValidationFeedback.CountErrors(formPostState.ValidationState)} error(s).");

            return await UserInviteValidationResponses.RenderValidationAsync(
                context,
                nameof(ValidationPage.MinimalInviteForm),
                UserInviteValidationDefinitions.MinimalLocal(formPostState.Model),
                cancellationToken);
        }

        var count = Random.Shared.Next(100, 200);
        DemoValidationFeedback.TriggerValid(context, formPostState.Model, count);
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-local-valid",
            details: $"Minimal API local validation accepted {formPostState.Model.DisplayName} (#{count}).");

        if (context.HtmxRequest().IsHtmx)
        {
            return await HrzResults.Fragment<UserInviteValidationForm>(
                context,
                new
                {
                    Form = UserInviteValidationDefinitions.MinimalLocal(formPostState.Model, success: true, count: count)
                },
                cancellationToken: cancellationToken);
        }

        return Results.Redirect("/validation");
    }

    private static async Task<IResult> HandleMinimalProxyAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IInviteValidationBackend inviteValidationBackend,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);

        var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
            UserInviteValidationRoots.MinimalProxy,
            cancellationToken);

        if (!formPostState.ValidationState.IsValid)
        {
            context.SetSubmitValidationState(formPostState.ValidationState);
            DemoValidationFeedback.TriggerInvalid(context, formPostState.ValidationState);
            DemoInspectorUpdates.Queue(
                context,
                action: "validation-minimal-proxy-invalid",
                details: $"Minimal API proxy validation failed locally with {DemoValidationFeedback.CountErrors(formPostState.ValidationState)} error(s) before the backend call.");

            return await UserInviteValidationResponses.RenderValidationAsync(
                context,
                nameof(ValidationPage.MinimalProxyInviteForm),
                UserInviteValidationDefinitions.MinimalProxy(formPostState.Model),
                cancellationToken);
        }

        var backendResult = await inviteValidationBackend.SubmitAsync(formPostState.Model, cancellationToken);
        if (!backendResult.IsSuccess)
        {
            var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
            var validationState = backendResult.ProblemDetails!.ToSubmitValidationState(
                UserInviteValidationRoots.MinimalProxy,
                resolver,
                formPostState.ValidationState.AttemptedValues);
            context.SetSubmitValidationState(validationState);
            DemoValidationFeedback.TriggerInvalid(context, validationState);
            DemoInspectorUpdates.Queue(
                context,
                action: "validation-minimal-proxy-backend-invalid",
                details: "Minimal API proxy mapped backend validation JSON back into the server-rendered form fragment.");

            return await UserInviteValidationResponses.RenderValidationAsync(
                context,
                nameof(ValidationPage.MinimalProxyInviteForm),
                UserInviteValidationDefinitions.MinimalProxy(formPostState.Model),
                cancellationToken);
        }

        DemoValidationFeedback.TriggerValid(context, formPostState.Model, backendResult.Count);
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-proxy-valid",
            details: $"Minimal API proxy validated successfully and the backend accepted {formPostState.Model.DisplayName} (#{backendResult.Count}).");

        if (context.HtmxRequest().IsHtmx)
        {
            return await HrzResults.Fragment<UserInviteValidationForm>(
                context,
                new
                {
                    Form = UserInviteValidationDefinitions.MinimalProxy(
                        formPostState.Model,
                        success: true,
                        count: backendResult.Count)
                },
                cancellationToken: cancellationToken);
        }

        return Results.Redirect("/validation");
    }

    private static async Task<IResult> HandleMixedSubmitAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IHrzModelValidator modelValidator,
        IHrzLiveValidationPolicyResolver livePolicyResolver,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);

        var formPostState = await context.BindFormAsync<MixedValidationInput>(
            UserInviteValidationRoots.MixedAuthoring,
            cancellationToken);
        var validationState = await DemoValidationSupport.BuildMixedSubmitValidationStateAsync(
            modelValidator,
            livePolicyResolver,
            formPostState,
            cancellationToken);

        if (!validationState.IsValid)
        {
            context.SetSubmitValidationState(validationState);
            DemoValidationFeedback.TriggerInvalid(context, validationState);
            DemoInspectorUpdates.Queue(
                context,
                action: "validation-mixed-invalid",
                details: $"Mixed authoring validation failed with {DemoValidationFeedback.CountErrors(validationState)} error(s).");

            return await MixedValidationResponses.RenderValidationAsync(
                context,
                nameof(ValidationPage.MixedAuthoringForm),
                MixedValidationDefinitions.Authoring(formPostState.Model),
                cancellationToken);
        }

        context.HtmxResponse().Trigger("form:valid", new
        {
            environment = formPostState.Model.Environment,
            seatCount = formPostState.Model.SeatCount,
            requiresApproval = formPostState.Model.RequiresApproval
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-mixed-valid",
            details: $"Mixed authoring validation accepted a {formPostState.Model.Environment} rollout for {formPostState.Model.SeatCount} seats.");

        if (context.HtmxRequest().IsHtmx)
        {
            return await HrzResults.Fragment<MixedValidationAuthoringForm>(
                context,
                new
                {
                    Form = MixedValidationDefinitions.Authoring(formPostState.Model, success: true)
                },
                cancellationToken: cancellationToken);
        }

        return Results.Redirect("/validation");
    }

    private static async Task<IResult> HandleMixedLiveAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IHrzLiveValidationPolicyResolver livePolicyResolver,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);

        var scope = await context.BindLiveValidationScopeAsync(cancellationToken);
        if (scope is null || scope.Fields.Count == 0)
        {
            return Results.NoContent();
        }

        var formPostState = await context.BindFormAsync<MixedValidationInput>(scope.RootId, cancellationToken);
        if (!MixedValidationDefinitions.TryResolve(scope.RootId, formPostState.Model, out var form))
        {
            return Results.NoContent();
        }

        var primaryField = DemoValidationSupport.ResolveMixedPrimaryField(scope);
        if (primaryField is null)
        {
            return Results.NoContent();
        }

        var primaryPolicy = await livePolicyResolver.ResolveAsync(
            formPostState.Model,
            scope.RootId,
            primaryField,
            formPostState.ValidationState.AttemptedValues,
            cancellationToken);
        var resolvedPolicies = await DemoValidationSupport.ResolveLivePoliciesAsync(
            livePolicyResolver,
            formPostState.Model,
            scope.RootId,
            primaryField,
            primaryPolicy,
            formPostState.ValidationState.AttemptedValues,
            cancellationToken);

        if (!primaryPolicy.Enabled)
        {
            var disabledFragments = new List<RenderFragment>
            {
                DemoValidationSupport.BuildMixedFieldSlotFragment(form, primaryField, Array.Empty<string>(), swapOob: false)
            };

            foreach (var resolvedPolicy in resolvedPolicies)
            {
                disabledFragments.Add(DemoValidationSupport.BuildMixedLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
            }

            foreach (var clearField in primaryPolicy.ClearFields
                         .Distinct()
                         .Where(field => !field.Equals(primaryField)))
            {
                disabledFragments.Add(DemoValidationSupport.BuildMixedFieldSlotFragment(form, clearField, Array.Empty<string>(), swapOob: true));
            }

            if (primaryPolicy.ReplaceSummaryWhenDisabled)
            {
                disabledFragments.Add(DemoValidationSupport.BuildMixedSummarySlotFragment(form, Array.Empty<string>(), swapOob: true));
            }

            DemoInspectorUpdates.Queue(
                context,
                action: "validation-mixed-live-policy-disabled",
                details: $"Mixed live policy blocked {primaryField.Value} and cleared {string.Join(", ", primaryPolicy.ClearFields.Select(static field => field.Value))}.");

            return await HrzResults.Fragment(context, cancellationToken, disabledFragments.ToArray());
        }

        var livePatch = DemoValidationSupport.BuildMixedLiveValidationPatch(scope, primaryField, formPostState.Model, resolvedPolicies);
        var fragments = new List<RenderFragment>
        {
            DemoValidationSupport.BuildMixedFieldSlotFragment(form, primaryField, DemoValidationSupport.GetFieldErrors(livePatch, primaryField), swapOob: false)
        };

        foreach (var resolvedPolicy in resolvedPolicies)
        {
            fragments.Add(DemoValidationSupport.BuildMixedLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
        }

        foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
        {
            fragments.Add(DemoValidationSupport.BuildMixedFieldSlotFragment(
                form,
                affectedField,
                DemoValidationSupport.GetFieldErrors(livePatch, affectedField),
                swapOob: true));
        }

        if (livePatch.ReplaceSummary)
        {
            fragments.Add(DemoValidationSupport.BuildMixedSummarySlotFragment(form, livePatch.SummaryErrors, swapOob: true));
        }

        DemoInspectorUpdates.Queue(
            context,
            action: "validation-mixed-live",
            details: $"Mixed live validation updated {string.Join(", ", livePatch.AffectedFields.Select(static field => field.Value))}.");

        return await HrzResults.Fragment(context, cancellationToken, fragments.ToArray());
    }

    private static async Task<IResult> HandleInviteLiveAsync(
        HttpContext context,
        IAntiforgery antiforgery,
        IHrzLiveValidationPolicyResolver livePolicyResolver,
        CancellationToken cancellationToken)
    {
        await antiforgery.ValidateRequestAsync(context);

        var scope = await context.BindLiveValidationScopeAsync(cancellationToken);
        if (scope is null || scope.Fields.Count == 0)
        {
            return Results.NoContent();
        }

        var formPostState = await context.BindFormAsync<InviteUserInput>(scope.RootId, cancellationToken);
        if (!UserInviteValidationDefinitions.TryResolve(scope.RootId, formPostState.Model, out var form))
        {
            return Results.NoContent();
        }

        var primaryField = DemoValidationSupport.ResolveInvitePrimaryField(scope);
        if (primaryField is null)
        {
            return Results.NoContent();
        }

        var primaryPolicy = await livePolicyResolver.ResolveAsync(
            formPostState.Model,
            scope.RootId,
            primaryField,
            formPostState.ValidationState.AttemptedValues,
            cancellationToken);
        var resolvedPolicies = await DemoValidationSupport.ResolveLivePoliciesAsync(
            livePolicyResolver,
            formPostState.Model,
            scope.RootId,
            primaryField,
            primaryPolicy,
            formPostState.ValidationState.AttemptedValues,
            cancellationToken);

        if (!primaryPolicy.Enabled)
        {
            var disabledFragments = new List<RenderFragment>
            {
                DemoValidationSupport.BuildFieldSlotFragment(form, primaryField, Array.Empty<string>(), swapOob: false)
            };

            foreach (var resolvedPolicy in resolvedPolicies)
            {
                disabledFragments.Add(DemoValidationSupport.BuildLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
            }

            foreach (var clearField in primaryPolicy.ClearFields
                         .Distinct()
                         .Where(field => !field.Equals(primaryField)))
            {
                disabledFragments.Add(DemoValidationSupport.BuildFieldSlotFragment(form, clearField, Array.Empty<string>(), swapOob: true));
            }

            if (primaryPolicy.ReplaceSummaryWhenDisabled)
            {
                disabledFragments.Add(DemoValidationSupport.BuildSummarySlotFragment(form, Array.Empty<string>(), swapOob: true));
            }

            DemoInspectorUpdates.Queue(
                context,
                action: "validation-live-policy-disabled",
                details: $"Live policy blocked {primaryField.Value} and cleared {string.Join(", ", primaryPolicy.ClearFields.Select(static field => field.Value))}.");

            return await HrzResults.Fragment(context, cancellationToken, disabledFragments.ToArray());
        }

        var livePatch = DemoValidationSupport.BuildInviteLiveValidationPatch(scope, primaryField, formPostState.Model, resolvedPolicies);
        var fragments = new List<RenderFragment>
        {
            DemoValidationSupport.BuildFieldSlotFragment(form, primaryField, DemoValidationSupport.GetFieldErrors(livePatch, primaryField), swapOob: false)
        };

        foreach (var resolvedPolicy in resolvedPolicies)
        {
            fragments.Add(DemoValidationSupport.BuildLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
        }

        foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
        {
            fragments.Add(DemoValidationSupport.BuildFieldSlotFragment(
                form,
                affectedField,
                DemoValidationSupport.GetFieldErrors(livePatch, affectedField),
                swapOob: true));
        }

        if (livePatch.ReplaceSummary)
        {
            fragments.Add(DemoValidationSupport.BuildSummarySlotFragment(form, livePatch.SummaryErrors, swapOob: true));
        }

        DemoInspectorUpdates.Queue(
            context,
            action: "validation-live",
            details: $"Live validation updated {string.Join(", ", livePatch.AffectedFields.Select(static field => field.Value))}.");

        return await HrzResults.Fragment(context, cancellationToken, fragments.ToArray());
    }
}
