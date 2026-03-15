using System.Net.ServerSentEvents;
using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Rendering;

namespace HyperRazor.Demo.Mvc.Infrastructure;

internal static class DemoSseReplayScenario
{
    public const string StreamName = "demo-replay";
    public const string DisconnectAfterEventId = "replay-demo-02";

    private static readonly DemoSseReplayEntry[] Entries =
    [
        new(
            EventId: "replay-demo-01",
            Title: "Live stream opened",
            Body: "The first live HTML frame arrived on the initial connection.",
            Badge: "live",
            ConnectionTitle: "Fresh connection",
            ConnectionDetail: "The server is sending live frames before the demo forces a reconnect.",
            ConnectionTone: "progress"),
        new(
            EventId: "replay-demo-02",
            Title: "Intentional disconnect",
            Body: "The server ends this first response without a done event so the browser reconnects with Last-Event-ID.",
            Badge: "live",
            ConnectionTitle: "Disconnect after replay-demo-02",
            ConnectionDetail: "The response ends here. EventSource should reconnect and advertise the last confirmed event ID.",
            ConnectionTone: "warning"),
        new(
            EventId: "replay-demo-03",
            Title: "Buffered event recovered",
            Body: "This frame was buffered while the connection was away and replayed after the browser resumed from replay-demo-02.",
            Badge: "replay",
            ConnectionTitle: "Replay in progress",
            ConnectionDetail: "The strategy is draining buffered frames that the client missed during the disconnect.",
            ConnectionTone: "resume"),
        new(
            EventId: "replay-demo-04",
            Title: "Replay buffer drained",
            Body: "The buffered gap is closed after this event. The next frame comes from the live stream again.",
            Badge: "replay",
            ConnectionTitle: "Replay buffer drained",
            ConnectionDetail: "HyperRazor finished replaying the buffered frames and is about to hand off to live streaming.",
            ConnectionTone: "resume"),
        new(
            EventId: "replay-demo-05",
            Title: "Live streaming resumed",
            Body: "After replay, the server emitted one fresh live frame and then closed cleanly with done.",
            Badge: "live",
            ConnectionTitle: "Live stream resumed",
            ConnectionDetail: "Replay is complete and the stream is back on its live path.",
            ConnectionTone: "success")
    ];

    public static IReadOnlyList<DemoSseReplayEntry> InitialEntries => Entries[..2];

    public static IReadOnlyList<DemoSseReplayEntry> ReplayEntries => Entries[2..4];

    public static DemoSseReplayEntry LiveResumeEntry => Entries[4];

    public static bool TryGetReplayEntries(string? lastEventId, out IReadOnlyList<DemoSseReplayEntry> entries)
    {
        if (string.Equals(lastEventId, DisconnectAfterEventId, StringComparison.Ordinal))
        {
            entries = ReplayEntries;
            return true;
        }

        entries = Array.Empty<DemoSseReplayEntry>();
        return false;
    }

    public static Task<SseItem<string>> RenderEntryAsync(
        DemoSseReplayEntry entry,
        HrzSseResumeContext resumeContext,
        IHrzSseRenderer sseRenderer,
        IHrzSwapService swapService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sseRenderer);
        ArgumentNullException.ThrowIfNull(swapService);

        swapService.Replace<SseDemoStatusCard>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.SseReplayPage.ConnectionRegion,
            new
            {
                Label = "connection",
                Title = entry.ConnectionTitle,
                Detail = entry.ConnectionDetail,
                Tone = entry.ConnectionTone
            });

        swapService.Replace<SseDemoStatusCard>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.SseReplayPage.ResumeRegion,
            new
            {
                Label = "last-event-id",
                Title = resumeContext.HasLastEventId
                    ? $"Resumed after {resumeContext.LastEventId}"
                    : "Awaiting reconnect",
                Detail = resumeContext.HasLastEventId
                    ? "The browser reconnected with Last-Event-ID and the replay strategy filled the missed gap."
                    : $"The first response ends after {DisconnectAfterEventId} so EventSource can reconnect with the last confirmed event ID.",
                Tone = resumeContext.HasLastEventId ? "resume" : "neutral"
            });

        return sseRenderer.RenderComponent<SseDemoFeedItem>(
            new
            {
                entry.EventId,
                entry.Title,
                entry.Body,
                entry.Badge
            },
            id: entry.EventId,
            cancellationToken: cancellationToken);
    }
}

internal sealed record DemoSseReplayEntry(
    string EventId,
    string Title,
    string Body,
    string Badge,
    string ConnectionTitle,
    string ConnectionDetail,
    string ConnectionTone);
