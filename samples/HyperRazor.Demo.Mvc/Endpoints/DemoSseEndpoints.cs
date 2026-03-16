using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace HyperRazor.Demo.Mvc.Endpoints;

public static class DemoSseEndpoints
{
    public static IEndpointRouteBuilder MapDemoSseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var sse = endpoints.MapGroup("/demos/sse");
        sse.MapGet("/stream", StreamSseDemo);
        sse.MapGet("/control-events/stream", StreamSseControlEventsDemo);
        sse.MapGet("/replay/stream", StreamSseReplayDemo);
        sse.MapGet("/control-events/panels/{eventName}", GetControlEventPanelAsync);

        var notifications = endpoints.MapGroup("/demos/notifications");
        notifications.MapGet("/stream", StreamNotificationsDemo);

        return endpoints;
    }

    private static IResult StreamSseDemo(
        HttpContext context,
        IHrzSseRenderer sseRenderer,
        IHrzSwapService swapService,
        CancellationToken cancellationToken)
    {
        return HrzResults.ServerSentEvents(StreamSseDemoAsync(context, sseRenderer, swapService, cancellationToken));
    }

    private static IResult StreamSseControlEventsDemo(CancellationToken cancellationToken)
    {
        return HrzResults.ServerSentEvents(StreamSseControlEventsDemoAsync(cancellationToken));
    }

    private static IResult StreamSseReplayDemo(
        HttpContext context,
        IHrzSseRenderer sseRenderer,
        IHrzSwapService swapService,
        CancellationToken cancellationToken)
    {
        return HrzResults.ServerSentEvents(StreamSseReplayDemoAsync(context, sseRenderer, swapService, cancellationToken));
    }

    private static IResult StreamNotificationsDemo(
        HttpContext context,
        IHrzSseRenderer sseRenderer,
        IHrzSwapService swapService,
        CancellationToken cancellationToken)
    {
        return HrzResults.ServerSentEvents(StreamNotificationsDemoAsync(context, sseRenderer, swapService, cancellationToken));
    }

    private static async Task<IResult> GetControlEventPanelAsync(
        HttpContext context,
        string eventName,
        CancellationToken cancellationToken)
    {
        var panel = ResolveSseControlEventPanel(eventName);
        if (panel is null)
        {
            return TypedResults.NotFound();
        }

        return await HrzResults.Fragment<SseControlEventPanel>(
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
    }

    private static async IAsyncEnumerable<SseItem<string>> StreamSseDemoAsync(
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
            swapService.Replace<SseDemoStatusCard>(
                HyperRazor.Demo.Mvc.Components.Pages.Admin.SsePage.StreamStatusRegion,
                new
                {
                    Label = "connection",
                    Title = step.StatusTitle,
                    Detail = step.StatusDetail,
                    Tone = step.EventId == "sse-demo-3" ? "success" : "progress"
                });

            swapService.Replace<SseDemoStatusCard>(
                HyperRazor.Demo.Mvc.Components.Pages.Admin.SsePage.LastEventIdRegion,
                new
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

    private static async IAsyncEnumerable<SseItem<string>> StreamNotificationsDemoAsync(
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

            swapService.Replace<NotificationsUnreadIndicator>(
                HyperRazor.Demo.Mvc.Components.Pages.Admin.NotificationsPage.UnreadIndicatorRegion,
                new
                {
                    Count = count
                });

            swapService.Replace<NotificationsStreamStateCard>(
                HyperRazor.Demo.Mvc.Components.Pages.Admin.NotificationsPage.StreamStateRegion,
                new
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

    private static async IAsyncEnumerable<SseItem<string>> StreamSseControlEventsDemoAsync(
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

    private static async IAsyncEnumerable<SseItem<string>> StreamSseReplayDemoAsync(
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

    private static SseControlEventPanelState? ResolveSseControlEventPanel(string eventName)
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

    private static async IAsyncEnumerable<SseItem<string>> StreamSseReplayLiveTailAsync(
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
}

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
