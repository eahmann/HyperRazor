using HyperRazor.Components;
using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddHyperRazor(options =>
{
    options.RootComponent = typeof(HrzApp<AppLayout>);
    options.UseMinimalLayoutForHtmx = true;
    options.LayoutBoundary.Enabled = true;
    options.LayoutBoundary.OnlyBoostedRequests = true;
    options.LayoutBoundary.PromotionMode = HrzLayoutBoundaryPromotionMode.ShellSwap;
    options.LayoutBoundary.LayoutFamilyHeaderName = HtmxHeaderNames.LayoutFamily;
    options.LayoutBoundary.DefaultLayoutFamily = "admin";
    options.LayoutBoundary.ShellTargetSelector = "#hrz-app-shell";
    options.LayoutBoundary.ShellSwapStyle = "outerHTML";
    options.LayoutBoundary.ShellReselectSelector = "#hrz-app-shell";
    options.LayoutBoundary.AddVaryHeader = true;
});
builder.Services.AddScoped<IHrzSseReplayStrategy, DemoSseReplayStrategy>();
builder.Services.AddHtmx(htmx =>
{
    htmx.ClientProfile = HtmxClientProfile.Htmx2Defaults;
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false;
    htmx.AllowNestedOobSwaps = false;
    htmx.DefaultSwapStyle = "outerHTML";
    htmx.EnableHeadSupport = true;
    htmx.AntiforgeryMetaName = "hrz-antiforgery";
    htmx.AntiforgeryHeaderName = "RequestVerificationToken";
    htmx.ResponseHandling =
    [
        new HtmxResponseHandlingRule
        {
            Code = "204",
            Swap = false
        },
        new HtmxResponseHandlingRule
        {
            Code = "[23]..",
            Swap = true
        },
        new HtmxResponseHandlingRule
        {
            Code = "[45]..",
            Swap = true,
            Error = false
        }
    ];
});
builder.Services.AddSingleton<IInviteValidationBackend, DemoInviteValidationBackend>();
builder.Services.AddSingleton<IHrzLiveValidationPolicyResolver, DemoValidationLivePolicyResolver>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseHyperRazor();
app.Use(async (context, next) =>
{
    var request = context.HtmxRequest();
    if (HttpMethods.IsGet(context.Request.Method)
        && request.RequestType == HtmxRequestType.Partial
        && !request.IsHistoryRestoreRequest
        && DemoChromeState.IsPageChromeRoute(context.Request.Path))
    {
        var chromeState = DemoChromeState.Create(context);
        var swapService = context.RequestServices.GetRequiredService<IHrzSwapService>();

        swapService.QueueComponent<DemoChromeToolbar>(
            targetId: "app-chrome-toolbar",
            parameters: new
            {
                chromeState.RouteLabel,
                chromeState.LayoutFamily,
                chromeState.Theme
            },
            swapStyle: SwapStyle.OuterHtml);

        swapService.QueueComponent<DemoChromeSidebar>(
            targetId: "app-chrome-sidebar",
            parameters: new
            {
                chromeState.ActiveSection,
                chromeState.LayoutFamily
            },
            swapStyle: SwapStyle.OuterHtml);
    }

    await next();
});

// AdminLayout routes are intentionally served via Minimal API so the demo shows parity in a real app area.
app.MapGet("/", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<DashboardPage>(context, cancellationToken: cancellationToken));
app.MapGet("/users", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<UsersPage>(context, cancellationToken: cancellationToken));
app.MapGet("/validation", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<ValidationPage>(context, cancellationToken: cancellationToken));
app.MapGet("/demos/sse", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<SsePage>(context, cancellationToken: cancellationToken));
app.MapGet("/demos/sse/control-events", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<SseControlEventsPage>(context, cancellationToken: cancellationToken));
app.MapGet("/demos/sse/replay", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<SseReplayPage>(context, cancellationToken: cancellationToken));
app.MapGet("/demos/notifications", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<NotificationsPage>(context, cancellationToken: cancellationToken));
app.MapGet("/settings/branding", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<BrandingSettingsPage>(context, cancellationToken: cancellationToken));
app.MapGet("/demos/sse/stream", (
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    CancellationToken cancellationToken) =>
    HrzResults.ServerSentEvents(StreamSseDemoAsync(context, sseRenderer, swapService, cancellationToken)));
app.MapGet("/demos/sse/control-events/stream", (CancellationToken cancellationToken) =>
    HrzResults.ServerSentEvents(StreamSseControlEventsDemoAsync(cancellationToken)));
app.MapGet("/demos/sse/replay/stream", (
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    CancellationToken cancellationToken) =>
    HrzResults.ServerSentEvents(StreamSseReplayDemoAsync(context, sseRenderer, swapService, cancellationToken)));
app.MapGet("/demos/sse/control-events/panels/{eventName}", async Task<IResult> (
    HttpContext context,
    string eventName,
    CancellationToken cancellationToken) =>
{
    var panel = ResolveSseControlEventPanel(eventName);
    if (panel is null)
    {
        return TypedResults.NotFound();
    }

    return await HrzResults.Partial<SseControlEventPanel>(
        context,
        new
        {
            panel.Id,
            panel.EventName,
            panel.Title,
            panel.Detail,
            panel.Tone
        },
        cancellationToken: cancellationToken);
});
app.MapGet("/demos/notifications/stream", (
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    CancellationToken cancellationToken) =>
    HrzResults.ServerSentEvents(StreamNotificationsDemoAsync(context, sseRenderer, swapService, cancellationToken)));
app.MapPost("/validation/minimal/local", async (
    HttpContext context,
    IAntiforgery antiforgery,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
        UserInviteValidationRoots.MinimalLocal,
        cancellationToken);

    if (!formPostState.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(formPostState.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(formPostState.ValidationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-local-invalid",
            details: $"Minimal API local validation failed with {CountErrors(formPostState.ValidationState)} error(s).");

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalInviteForm),
            UserInviteValidationDefinitions.MinimalLocal(formPostState.Model),
            cancellationToken);
    }

    var count = Random.Shared.Next(100, 200);
    context.HtmxResponse().Trigger("form:valid", new
    {
        name = formPostState.Model.DisplayName,
        email = formPostState.Model.Email,
        count
    });
    DemoInspectorUpdates.Queue(
        context,
        action: "validation-minimal-local-valid",
        details: $"Minimal API local validation accepted {formPostState.Model.DisplayName} (#{count}).");

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
            context,
            new
            {
                Form = UserInviteValidationDefinitions.MinimalLocal(formPostState.Model, success: true, count: count)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});
app.MapPost("/validation/minimal/proxy", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IInviteValidationBackend inviteValidationBackend,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
        UserInviteValidationRoots.MinimalProxy,
        cancellationToken);

    if (!formPostState.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(formPostState.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(formPostState.ValidationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-proxy-invalid",
            details: $"Minimal API proxy validation failed locally with {CountErrors(formPostState.ValidationState)} error(s) before the backend call.");

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
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(validationState)
        });
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

    context.HtmxResponse().Trigger("form:valid", new
    {
        name = formPostState.Model.DisplayName,
        email = formPostState.Model.Email,
        count = backendResult.Count
    });
    DemoInspectorUpdates.Queue(
        context,
        action: "validation-minimal-proxy-valid",
        details: $"Minimal API proxy validated successfully and the backend accepted {formPostState.Model.DisplayName} (#{backendResult.Count}).");

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
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
});
app.MapPost("/validation/mixed", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IHrzModelValidator modelValidator,
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var formPostState = await context.BindFormAsync<MixedValidationInput>(
        UserInviteValidationRoots.MixedAuthoring,
        cancellationToken);
    var validationState = await BuildMixedSubmitValidationStateAsync(
        modelValidator,
        livePolicyResolver,
        formPostState,
        cancellationToken);

    if (!validationState.IsValid)
    {
        context.SetSubmitValidationState(validationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(validationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-mixed-invalid",
            details: $"Mixed authoring validation failed with {CountErrors(validationState)} error(s).");

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
        return await HrzResults.Partial<MixedValidationAuthoringForm>(
            context,
            new
            {
                Form = MixedValidationDefinitions.Authoring(formPostState.Model, success: true)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});
app.MapPost("/validation/mixed/live", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    CancellationToken cancellationToken) =>
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

    var primaryField = ResolveMixedPrimaryField(scope);
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
    var resolvedPolicies = await ResolveMixedLivePoliciesAsync(
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
            BuildMixedFieldSlotFragment(form, primaryField, Array.Empty<string>(), swapOob: false)
        };

        foreach (var resolvedPolicy in resolvedPolicies)
        {
            disabledFragments.Add(BuildMixedLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
        }

        foreach (var clearField in primaryPolicy.ClearFields
                     .Distinct()
                     .Where(field => !field.Equals(primaryField)))
        {
            disabledFragments.Add(BuildMixedFieldSlotFragment(form, clearField, Array.Empty<string>(), swapOob: true));
        }

        if (primaryPolicy.ReplaceSummaryWhenDisabled)
        {
            disabledFragments.Add(BuildMixedSummarySlotFragment(form, Array.Empty<string>(), swapOob: true));
        }

        DemoInspectorUpdates.Queue(
            context,
            action: "validation-mixed-live-policy-disabled",
            details: $"Mixed live policy blocked {primaryField.Value} and cleared {string.Join(", ", primaryPolicy.ClearFields.Select(static field => field.Value))}.");

        return await HrzResults.Partial(context, cancellationToken, disabledFragments.ToArray());
    }

    var livePatch = BuildMixedLiveValidationPatch(scope, primaryField, formPostState.Model, resolvedPolicies);
    var fragments = new List<RenderFragment>
    {
        BuildMixedFieldSlotFragment(form, primaryField, GetFieldErrors(livePatch, primaryField), swapOob: false)
    };

    foreach (var resolvedPolicy in resolvedPolicies)
    {
        fragments.Add(BuildMixedLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
    }

    foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
    {
        fragments.Add(BuildMixedFieldSlotFragment(form, affectedField, GetFieldErrors(livePatch, affectedField), swapOob: true));
    }

    if (livePatch.ReplaceSummary)
    {
        fragments.Add(BuildMixedSummarySlotFragment(form, livePatch.SummaryErrors, swapOob: true));
    }

    DemoInspectorUpdates.Queue(
        context,
        action: "validation-mixed-live",
        details: $"Mixed live validation updated {string.Join(", ", livePatch.AffectedFields.Select(static field => field.Value))}.");

    return await HrzResults.Partial(context, cancellationToken, fragments.ToArray());
});
app.MapPost("/validation/live", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    CancellationToken cancellationToken) =>
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

    var primaryField = ResolvePrimaryField(scope);
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
    var resolvedPolicies = await ResolveLivePoliciesAsync(
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
            BuildFieldSlotFragment(form, primaryField, Array.Empty<string>(), swapOob: false)
        };

        foreach (var resolvedPolicy in resolvedPolicies)
        {
            disabledFragments.Add(BuildLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
        }

        foreach (var clearField in primaryPolicy.ClearFields
                     .Distinct()
                     .Where(field => !field.Equals(primaryField)))
        {
            disabledFragments.Add(BuildFieldSlotFragment(form, clearField, Array.Empty<string>(), swapOob: true));
        }

        if (primaryPolicy.ReplaceSummaryWhenDisabled)
        {
            disabledFragments.Add(BuildSummarySlotFragment(form, Array.Empty<string>(), swapOob: true));
        }

        DemoInspectorUpdates.Queue(
            context,
            action: "validation-live-policy-disabled",
            details: $"Live policy blocked {primaryField.Value} and cleared {string.Join(", ", primaryPolicy.ClearFields.Select(static field => field.Value))}.");

        return await HrzResults.Partial(context, cancellationToken, disabledFragments.ToArray());
    }

    var livePatch = BuildInviteLiveValidationPatch(scope, primaryField, formPostState.Model, resolvedPolicies);
    var fragments = new List<RenderFragment>
    {
        BuildFieldSlotFragment(form, primaryField, GetFieldErrors(livePatch, primaryField), swapOob: false)
    };

    foreach (var resolvedPolicy in resolvedPolicies)
    {
        fragments.Add(BuildLivePolicyCarrierFragment(form, resolvedPolicy.Key, resolvedPolicy.Value, swapOob: true));
    }

    foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
    {
        fragments.Add(BuildFieldSlotFragment(form, affectedField, GetFieldErrors(livePatch, affectedField), swapOob: true));
    }

    if (livePatch.ReplaceSummary)
    {
        fragments.Add(BuildSummarySlotFragment(form, livePatch.SummaryErrors, swapOob: true));
    }

    DemoInspectorUpdates.Queue(
        context,
        action: "validation-live",
        details: $"Live validation updated {string.Join(", ", livePatch.AffectedFields.Select(static field => field.Value))}.");

    return await HrzResults.Partial(context, cancellationToken, fragments.ToArray());
});

app.MapControllers();

static int CountErrors(HrzSubmitValidationState validationState)
{
    return validationState.SummaryErrors.Count
        + validationState.FieldErrors.Sum(static pair => pair.Value.Count);
}

static HrzFieldPath? ResolvePrimaryField(HrzValidationScope scope)
{
    return scope.ValidateAll
        ? UserInviteValidationForm.EmailPath
        : scope.Fields.FirstOrDefault(static field =>
            field.Equals(UserInviteValidationForm.EmailPath)
            || field.Equals(UserInviteValidationForm.DisplayNamePath));
}

static async IAsyncEnumerable<SseItem<string>> StreamSseDemoAsync(
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var frameDelay = TimeSpan.FromSeconds(1.25);
    var resumeHeader = HrzSse.GetLastEventId(context.Request);
    var resumeTitle = string.IsNullOrWhiteSpace(resumeHeader) ? "Fresh Stream" : "Reconnect Requested";
    var resumeDetail = string.IsNullOrWhiteSpace(resumeHeader)
        ? "No resume header was supplied on this connection."
        : $"Reconnect requested from event {resumeHeader}.";

    var steps = new[]
    {
        new SseDemoStep(
            EventId: "sse-demo-1",
            Title: "Connection established",
            Body: "The first HTML fragment arrived over SSE without a follow-up polling request.",
            Badge: "message",
            StatusTitle: "Stream connected",
            StatusDetail: "The server opened the stream and rendered the first fragment immediately."),
        new SseDemoStep(
            EventId: "sse-demo-2",
            Title: "Out-of-band update applied",
            Body: "This message appends a second card while also replacing the sidecar through HyperRazor's OOB queue.",
            Badge: "message",
            StatusTitle: "Secondary target updated",
            StatusDetail: "The sidecar changed from the same SSE message instead of a separate request."),
        new SseDemoStep(
            EventId: "sse-demo-3",
            Title: "Graceful shutdown prepared",
            Body: "One final HTML frame renders before the connection closes with a blank-data done event.",
            Badge: "message",
            StatusTitle: "Closed cleanly",
            StatusDetail: "The next SSE frame is event: done with a blank data line, so HTMX should stop reconnecting.")
    };

    foreach (var step in steps)
    {
        swapService.QueueComponent<SseDemoStatusCard>(
            targetId: "sse-stream-status",
            parameters: new
            {
                Label = "connection",
                Title = step.StatusTitle,
                Detail = step.StatusDetail,
                Tone = step.EventId == "sse-demo-3" ? "success" : "progress"
            });

        swapService.QueueComponent<SseDemoStatusCard>(
            targetId: "sse-last-event-id",
            parameters: new
            {
                Label = "last-event-id",
                Title = resumeTitle,
                Detail = resumeDetail,
                Tone = "resume"
            });

        yield return await sseRenderer.RenderComponent<SseDemoFeedItem>(
            new
            {
                step.EventId,
                step.Title,
                step.Body,
                step.Badge
            },
            id: step.EventId,
            cancellationToken: cancellationToken);

        if (step != steps[^1])
        {
            await Task.Delay(frameDelay, cancellationToken);
        }
    }

    yield return HrzSse.Done();
}

static async IAsyncEnumerable<SseItem<string>> StreamNotificationsDemoAsync(
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var frameDelay = TimeSpan.FromMilliseconds(700);
    var resumeHeader = HrzSse.GetLastEventId(context.Request);
    var resumeDetail = string.IsNullOrWhiteSpace(resumeHeader)
        ? "Connected from the beginning of the demo stream."
        : $"Client requested resume after {resumeHeader}.";

    var notifications = new[]
    {
        new NotificationDemoEntry("notif-01", "deployments", "New comment on deployment review", "Platform requested one more smoke check before the noon rollout window.", "note 01", "notice"),
        new NotificationDemoEntry("notif-02", "access", "Access request escalated", "Finance export access was escalated to an on-call approver after the SLA threshold.", "note 02", "warning"),
        new NotificationDemoEntry("notif-03", "invites", "New contractor invite accepted", "A vendor identity accepted the invite and is waiting for follow-up provisioning.", "note 03", "notice"),
        new NotificationDemoEntry("notif-04", "billing", "Billing sync completed", "The overnight reconciliation job finished and posted the final delta set.", "note 04", "notice"),
        new NotificationDemoEntry("notif-05", "support", "Support queue nearing SLA", "The west region support queue is within 12 minutes of its first response target.", "note 05", "warning"),
        new NotificationDemoEntry("notif-06", "audit", "Audit export ready", "Compliance generated the weekly audit package and staged it for review.", "note 06", "notice"),
        new NotificationDemoEntry("notif-07", "security", "SSO certificate expires soon", "The shared SAML certificate now has seven days remaining before renewal is required.", "note 07", "warning"),
        new NotificationDemoEntry("notif-08", "sync", "Nightly directory sync failed", "The background directory sync stopped after the upstream API returned repeated 503 responses.", "note 08", "warning"),
        new NotificationDemoEntry("notif-09", "incidents", "P1 incident declared for EU auth", "Authentication failures crossed the paging threshold and an incident bridge is now active.", "note 09", "urgent"),
        new NotificationDemoEntry("notif-10", "incidents", "EU auth incident resolved", "The rollback completed, error rates normalized, and the bridge moved into recovery review.", "note 10", "recovery")
    };

    for (var index = 0; index < notifications.Length; index++)
    {
        var notification = notifications[index];
        var count = index + 1;

        swapService.QueueComponent<NotificationsUnreadIndicator>(
            targetId: "notifications-unread-indicator",
            parameters: new
            {
                Count = count
            });

        swapService.QueueComponent<NotificationsStreamStateCard>(
            targetId: "notifications-stream-state",
            parameters: new
            {
                EventId = notification.EventId,
                Position = $"{count} / {notifications.Length}",
                Detail = resumeDetail,
                Tone = count == notifications.Length ? "success" : "progress"
            });

        yield return await sseRenderer.RenderComponent<NotificationsDemoItem>(
            new
            {
                notification.EventId,
                notification.Category,
                notification.Title,
                notification.Body,
                notification.Stamp,
                notification.Tone
            },
            id: notification.EventId,
            cancellationToken: cancellationToken);

        if (count < notifications.Length)
        {
            await Task.Delay(frameDelay, cancellationToken);
        }
    }

    yield return HrzSse.Done();
}

static async IAsyncEnumerable<SseItem<string>> StreamSseControlEventsDemoAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var frameDelay = TimeSpan.FromMilliseconds(650);

    yield return HrzSse.Stale(
        "Replay window expired. Fetch a fresh snapshot before resuming.",
        id: "sse-control-stale");
    await Task.Delay(frameDelay, cancellationToken);

    yield return HrzSse.RateLimited(
        "The server asked the client to back off before reconnecting.",
        id: "sse-control-rate-limited",
        retryAfter: TimeSpan.FromSeconds(6));
    await Task.Delay(frameDelay, cancellationToken);

    yield return HrzSse.Reset(
        "Server state changed. Rebuild the affected UI from a clean snapshot.",
        id: "sse-control-reset");
    await Task.Delay(frameDelay, cancellationToken);

    yield return HrzSse.Unauthorized(
        "Session credentials expired. Reauthenticate before reconnecting.",
        id: "sse-control-unauthorized");
    await Task.Delay(frameDelay, cancellationToken);

    yield return HrzSse.Done();
}

static async IAsyncEnumerable<SseItem<string>> StreamSseReplayDemoAsync(
    HttpContext context,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    var frameDelay = TimeSpan.FromMilliseconds(850);
    var resumeContext = HrzSseResumeContext.FromRequest(context.Request);

    if (!resumeContext.HasLastEventId)
    {
        foreach (var entry in DemoSseReplayScenario.InitialEntries)
        {
            yield return await DemoSseReplayScenario.RenderEntryAsync(
                entry,
                resumeContext,
                sseRenderer,
                swapService,
                cancellationToken);

            if (!string.Equals(entry.EventId, DemoSseReplayScenario.DisconnectAfterEventId, StringComparison.Ordinal))
            {
                await Task.Delay(frameDelay, cancellationToken);
            }
        }

        yield break;
    }

    await foreach (var item in HrzSseReplay.Compose(
        context,
        StreamSseReplayLiveTailAsync(resumeContext, sseRenderer, swapService, frameDelay, cancellationToken),
        DemoSseReplayScenario.StreamName,
        cancellationToken))
    {
        yield return item;
    }
}

static HrzFieldPath? ResolveMixedPrimaryField(HrzValidationScope scope)
static HrzLiveValidationPatch? BuildInviteLiveValidationPatch(HrzValidationScope scope, InviteUserInput input)
{
    return scope.ValidateAll
        ? MixedValidationAuthoringForm.SeatCountPath
        : scope.Fields.FirstOrDefault(static field =>
            field.Equals(MixedValidationAuthoringForm.EnvironmentPath)
            || field.Equals(MixedValidationAuthoringForm.RequiresApprovalPath)
            || field.Equals(MixedValidationAuthoringForm.SeatCountPath));
}

static async Task<IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy>> ResolveLivePoliciesAsync(
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    InviteUserInput input,
    HrzValidationRootId rootId,
    HrzFieldPath primaryField,
    HrzLiveValidationPolicy primaryPolicy,
    IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
    CancellationToken cancellationToken)
{
    var policies = new Dictionary<HrzFieldPath, HrzLiveValidationPolicy>
    {
        [primaryField] = primaryPolicy
    };

    foreach (var affectedField in primaryPolicy.AffectedFields.Where(field => !field.Equals(primaryField)).Distinct())
    {
        policies[affectedField] = await livePolicyResolver.ResolveAsync(
            input,
            rootId,
            affectedField,
            attemptedValues,
            cancellationToken);
    }

    return policies;
}

static async Task<IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy>> ResolveMixedLivePoliciesAsync(
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    MixedValidationInput input,
    HrzValidationRootId rootId,
    HrzFieldPath primaryField,
    HrzLiveValidationPolicy primaryPolicy,
    IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues,
    CancellationToken cancellationToken)
{
    var policies = new Dictionary<HrzFieldPath, HrzLiveValidationPolicy>
    {
        [primaryField] = primaryPolicy
    };

    foreach (var affectedField in primaryPolicy.AffectedFields.Where(field => !field.Equals(primaryField)).Distinct())
    {
        policies[affectedField] = await livePolicyResolver.ResolveAsync(
            input,
            rootId,
            affectedField,
            attemptedValues,
            cancellationToken);
    }

    return policies;
}

static async Task<HrzSubmitValidationState> BuildMixedSubmitValidationStateAsync(
    IHrzModelValidator modelValidator,
    IHrzLiveValidationPolicyResolver livePolicyResolver,
    HrzFormPostState<MixedValidationInput> formPostState,
    CancellationToken cancellationToken)
{
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
    var resolvedPolicies = await ResolveMixedLivePoliciesAsync(
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

static HrzLiveValidationPatch BuildInviteLiveValidationPatch(
    HrzValidationScope scope,
    HrzFieldPath primaryField,
    InviteUserInput input,
    IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy> resolvedPolicies)
{
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

static HrzLiveValidationPatch BuildMixedLiveValidationPatch(
    HrzValidationScope scope,
    HrzFieldPath primaryField,
    MixedValidationInput input,
    IReadOnlyDictionary<HrzFieldPath, HrzLiveValidationPolicy> resolvedPolicies)
{
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

static bool IsEligibleForDisplayNameServerValidation(string? displayName)
{
    return !string.IsNullOrWhiteSpace(displayName)
        && displayName.Trim().Length >= 3;
}

static IReadOnlyList<string> GetFieldErrors(HrzLiveValidationPatch patch, HrzFieldPath fieldPath)
{
    return patch.FieldErrors.TryGetValue(fieldPath, out var messages)
        ? messages
        : Array.Empty<string>();
}

static HrzSubmitValidationState ToSubmitValidationState(
    HrzLiveValidationPatch patch,
    IReadOnlyDictionary<HrzFieldPath, HrzAttemptedValue> attemptedValues)
{
    return new HrzSubmitValidationState(
        patch.RootId,
        patch.SummaryErrors,
        patch.FieldErrors,
        attemptedValues);
}

static RenderFragment BuildFieldSlotFragment(
    InviteValidationFormViewModel form,
    HrzFieldPath fieldPath,
    IReadOnlyList<string> errors,
    bool swapOob)
{
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

static RenderFragment BuildSummarySlotFragment(
    InviteValidationFormViewModel form,
    IReadOnlyList<string> errors,
    bool swapOob)
{
    return builder =>
    {
        builder.OpenComponent<HrzValidationServerSummarySlot>(0);
        builder.AddAttribute(1, nameof(HrzValidationServerSummarySlot.Id), UserInviteValidationForm.GetSummaryId(form.IdPrefix));
        builder.AddAttribute(2, nameof(HrzValidationServerSummarySlot.Errors), errors);
        builder.AddAttribute(3, nameof(HrzValidationServerSummarySlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

static RenderFragment BuildLivePolicyCarrierFragment(
    InviteValidationFormViewModel form,
    HrzFieldPath fieldPath,
    HrzLiveValidationPolicy policy,
    bool swapOob)
{
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

static RenderFragment BuildMixedFieldSlotFragment(
    MixedValidationFormViewModel form,
    HrzFieldPath fieldPath,
    IReadOnlyList<string> errors,
    bool swapOob)
{
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

static RenderFragment BuildMixedSummarySlotFragment(
    MixedValidationFormViewModel form,
    IReadOnlyList<string> errors,
    bool swapOob)
{
    return builder =>
    {
        builder.OpenComponent<HrzValidationServerSummarySlot>(0);
        builder.AddAttribute(1, nameof(HrzValidationServerSummarySlot.Id), MixedValidationAuthoringForm.GetSummaryId(form.IdPrefix));
        builder.AddAttribute(2, nameof(HrzValidationServerSummarySlot.Errors), errors);
        builder.AddAttribute(3, nameof(HrzValidationServerSummarySlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

static RenderFragment BuildMixedLivePolicyCarrierFragment(
    MixedValidationFormViewModel form,
    HrzFieldPath fieldPath,
    HrzLiveValidationPolicy policy,
    bool swapOob)
{
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

static SseControlEventPanelState? ResolveSseControlEventPanel(string eventName)
{
    return eventName switch
    {
        HrzSseEventNames.Stale => new SseControlEventPanelState(
            Id: "sse-control-stale",
            EventName: HrzSseEventNames.Stale,
            Title: "Stale signal received",
            Detail: "HTMX dispatched sse:stale and re-rendered this card through a normal fragment request.",
            Tone: "warning"),
        HrzSseEventNames.RateLimited => new SseControlEventPanelState(
            Id: "sse-control-rate-limited",
            EventName: HrzSseEventNames.RateLimited,
            Title: "Rate-limited signal received",
            Detail: "The server requested a slower reconnect cadence before this stream should be retried.",
            Tone: "resume"),
        HrzSseEventNames.Reset => new SseControlEventPanelState(
            Id: "sse-control-reset",
            EventName: HrzSseEventNames.Reset,
            Title: "Reset signal received",
            Detail: "This is where an app would rebuild the affected live region from a fresh server snapshot.",
            Tone: "progress"),
        HrzSseEventNames.Unauthorized => new SseControlEventPanelState(
            Id: "sse-control-unauthorized",
            EventName: HrzSseEventNames.Unauthorized,
            Title: "Unauthorized signal received",
            Detail: "This is where an app would route into reauthentication or a session-expired prompt.",
            Tone: "warning"),
        _ => null
    };
}

static async IAsyncEnumerable<SseItem<string>> StreamSseReplayLiveTailAsync(
    HrzSseResumeContext resumeContext,
    IHrzSseRenderer sseRenderer,
    IHrzSwapService swapService,
    TimeSpan frameDelay,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await Task.Delay(frameDelay, cancellationToken);

    yield return await DemoSseReplayScenario.RenderEntryAsync(
        DemoSseReplayScenario.LiveResumeEntry,
        resumeContext,
        sseRenderer,
        swapService,
        cancellationToken);

    yield return HrzSse.Done();
}

app.Run();

public partial class Program;

internal sealed record NotificationDemoEntry(
    string EventId,
    string Category,
    string Title,
    string Body,
    string Stamp,
    string Tone);

internal sealed record SseDemoStep(
    string EventId,
    string Title,
    string Body,
    string Badge,
    string StatusTitle,
    string StatusDetail);

internal sealed record SseControlEventPanelState(
    string Id,
    string EventName,
    string Title,
    string Detail,
    string Tone);
