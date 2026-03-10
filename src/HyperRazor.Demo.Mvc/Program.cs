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
app.MapPost("/validation/live", async (
    HttpContext context,
    IAntiforgery antiforgery,
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

    var livePatch = BuildInviteLiveValidationPatch(scope, formPostState.Model);
    if (livePatch is null)
    {
        return Results.NoContent();
    }

    var fragments = new List<RenderFragment>
    {
        BuildFieldSlotFragment(form, primaryField, livePatch, swapOob: false)
    };

    foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
    {
        fragments.Add(BuildFieldSlotFragment(form, affectedField, livePatch, swapOob: true));
    }

    if (livePatch.ReplaceSummary)
    {
        fragments.Add(BuildSummarySlotFragment(form, livePatch, swapOob: true));
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

static HrzLiveValidationPatch? BuildInviteLiveValidationPatch(HrzValidationScope scope, InviteUserInput input)
{
    var validatesEmail = scope.ValidateAll || scope.Fields.Contains(UserInviteValidationForm.EmailPath);
    var validatesDisplayName = scope.ValidateAll || scope.Fields.Contains(UserInviteValidationForm.DisplayNamePath);
    if (!validatesEmail && !validatesDisplayName)
    {
        return null;
    }

    var email = input.Email?.Trim();
    var displayName = input.DisplayName?.Trim();
    var requiresTeamDisplayName = string.Equals(email, "shared-mailbox@example.com", StringComparison.OrdinalIgnoreCase);
    if ((validatesEmail && string.IsNullOrWhiteSpace(email))
        || (requiresTeamDisplayName && string.IsNullOrWhiteSpace(displayName)))
    {
        return null;
    }

    var affectedFields = new List<HrzFieldPath>();
    var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
    var summaryErrors = new List<string>();

    if (validatesEmail)
    {
        affectedFields.Add(UserInviteValidationForm.EmailPath);
        affectedFields.Add(UserInviteValidationForm.DisplayNamePath);

        var emailErrors = string.Equals(email, "backend-taken@example.com", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Email already exists in the upstream directory." }
            : Array.Empty<string>();
        fieldErrors[UserInviteValidationForm.EmailPath] = emailErrors;

        if (emailErrors.Length > 0)
        {
            summaryErrors.Add("Backend would reject this invite on submit.");
        }
    }

    if (validatesDisplayName || validatesEmail)
    {
        affectedFields.Add(UserInviteValidationForm.DisplayNamePath);

        var displayNameErrors = requiresTeamDisplayName
            && !displayName!.Contains("team", StringComparison.OrdinalIgnoreCase)
            ? new[] { "Shared mailbox invites must use a team display name." }
            : Array.Empty<string>();
        fieldErrors[UserInviteValidationForm.DisplayNamePath] = displayNameErrors;

        if (displayNameErrors.Length > 0)
        {
            summaryErrors.Add("Shared mailbox invites need a team display name before the backend will accept them.");
        }
    }

    if (!fieldErrors.ContainsKey(UserInviteValidationForm.EmailPath) && validatesDisplayName)
    {
        fieldErrors[UserInviteValidationForm.EmailPath] = Array.Empty<string>();
    }

    return new HrzLiveValidationPatch(
        scope.RootId,
        affectedFields.Distinct().ToArray(),
        fieldErrors,
        ReplaceSummary: true,
        SummaryErrors: summaryErrors);
}

static RenderFragment BuildFieldSlotFragment(
    InviteValidationFormViewModel form,
    HrzFieldPath fieldPath,
    HrzLiveValidationPatch patch,
    bool swapOob)
{
    var slotId = fieldPath.Equals(UserInviteValidationForm.DisplayNamePath)
        ? $"{form.IdPrefix}-display-name-server"
        : $"{form.IdPrefix}-email-server";
    var errors = patch.FieldErrors.TryGetValue(fieldPath, out var messages)
        ? messages
        : Array.Empty<string>();

    return builder =>
    {
        builder.OpenComponent<ValidationServerFieldSlot>(0);
        builder.AddAttribute(1, nameof(ValidationServerFieldSlot.Id), slotId);
        builder.AddAttribute(2, nameof(ValidationServerFieldSlot.FieldPath), fieldPath.Value);
        builder.AddAttribute(3, nameof(ValidationServerFieldSlot.Errors), errors);
        builder.AddAttribute(4, nameof(ValidationServerFieldSlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

static RenderFragment BuildSummarySlotFragment(
    InviteValidationFormViewModel form,
    HrzLiveValidationPatch patch,
    bool swapOob)
{
    return builder =>
    {
        builder.OpenComponent<ValidationServerSummarySlot>(0);
        builder.AddAttribute(1, nameof(ValidationServerSummarySlot.Id), $"{form.IdPrefix}-server-summary");
        builder.AddAttribute(2, nameof(ValidationServerSummarySlot.Errors), patch.SummaryErrors);
        builder.AddAttribute(3, nameof(ValidationServerSummarySlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
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
