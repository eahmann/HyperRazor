using System.Net;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Mvc.Tests;

[Collection("DemoMvcWebAppFactoryCollection")]
public class DemoMvcSseTests : DemoMvcIntegrationTestBase
{
    public DemoMvcSseTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SseRoute_ReturnsExplicitSseMarkup()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/sse");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>SSE Live Feed</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-ext=\"sse\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-connect=\"/demos/sse/stream\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-close=\"done\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-swap=\"message\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/sse/control-events\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse/control-events\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/sse/replay\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse/replay\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-target=\"#hrz-main-layout\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-push-url=\"true\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseControlEventsRoute_ReturnsNamedEventHarnessMarkup()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/sse/control-events");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>SSE Control Events</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-ext=\"sse\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-connect=\"/demos/sse/control-events/stream\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-close=\"done\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"sse:stale\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"sse:rate-limited\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"sse:reset\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"sse:unauthorized\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse/control-events/panels/stale\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/sse\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/sse/replay\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse/replay\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-target=\"#hrz-main-layout\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-push-url=\"true\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseReplayRoute_ReturnsReplayHarnessMarkup()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/sse/replay");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>SSE Replay</h2>", body, StringComparison.Ordinal);
        Assert.Contains("sse-connect=\"/demos/sse/replay/stream\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-close=\"done\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-swap=\"message\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"sse-replay-feed\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"sse-replay-resume\"", body, StringComparison.Ordinal);
        Assert.Contains("Browser DevTools may log one expected", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/demos/sse/control-events\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseStream_ReturnsIncrementalHtmlWithOobAndDoneEvent()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/sse/stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var firstEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-demo-1", firstEvent, StringComparison.Ordinal);
        Assert.Contains("Connection established", firstEvent, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", firstEvent, StringComparison.Ordinal);
        Assert.Contains("No resume header was supplied on this connection.", firstEvent, StringComparison.Ordinal);

        var secondEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-demo-2", secondEvent, StringComparison.Ordinal);
        Assert.Contains("Out-of-band update applied", secondEvent, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", secondEvent, StringComparison.Ordinal);
        Assert.Contains("Secondary target updated", secondEvent, StringComparison.Ordinal);

        var thirdEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-demo-3", thirdEvent, StringComparison.Ordinal);
        Assert.Contains("Closed cleanly", thirdEvent, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", thirdEvent, StringComparison.Ordinal);

        var doneEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("event: done", doneEvent, StringComparison.Ordinal);
        Assert.Contains("data: ", doneEvent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseReplayStream_FirstConnection_EndsAfterTwoSeedFramesWithoutDone()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/sse/replay/stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var firstEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: replay-demo-01", firstEvent, StringComparison.Ordinal);
        Assert.Contains("Live stream opened", firstEvent, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", firstEvent, StringComparison.Ordinal);
        Assert.Contains("Awaiting reconnect", firstEvent, StringComparison.Ordinal);

        var secondEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: replay-demo-02", secondEvent, StringComparison.Ordinal);
        Assert.Contains("Intentional disconnect", secondEvent, StringComparison.Ordinal);
        Assert.Contains("Disconnect after replay-demo-02", secondEvent, StringComparison.Ordinal);

        using var tailCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var tailLine = await reader.ReadLineAsync(tailCts.Token);
        Assert.Null(tailLine);
    }

    [Fact]
    public async Task SseReplayStream_WithLastEventId_ReplaysBufferedFramesThenCloses()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/sse/replay/stream");
        request.Headers.Add("Last-Event-ID", "replay-demo-02");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var replayThree = await ReadEventBlockAsync(reader);
        Assert.Contains("id: replay-demo-03", replayThree, StringComparison.Ordinal);
        Assert.Contains("Buffered event recovered", replayThree, StringComparison.Ordinal);
        Assert.Contains("Resumed after replay-demo-02", replayThree, StringComparison.Ordinal);

        var replayFour = await ReadEventBlockAsync(reader);
        Assert.Contains("id: replay-demo-04", replayFour, StringComparison.Ordinal);
        Assert.Contains("Replay buffer drained", replayFour, StringComparison.Ordinal);

        var liveFive = await ReadEventBlockAsync(reader);
        Assert.Contains("id: replay-demo-05", liveFive, StringComparison.Ordinal);
        Assert.Contains("Live streaming resumed", liveFive, StringComparison.Ordinal);
        Assert.Contains("Live stream resumed", liveFive, StringComparison.Ordinal);

        var doneEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("event: done", doneEvent, StringComparison.Ordinal);
        Assert.Contains("data: ", doneEvent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseControlEventsStream_ReturnsNamedEventsInExpectedOrder()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/sse/control-events/stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var staleEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-control-stale", staleEvent, StringComparison.Ordinal);
        Assert.Contains("event: stale", staleEvent, StringComparison.Ordinal);
        Assert.Contains("data: Replay window expired. Fetch a fresh snapshot before resuming.", staleEvent, StringComparison.Ordinal);

        var rateLimitedEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-control-rate-limited", rateLimitedEvent, StringComparison.Ordinal);
        Assert.Contains("event: rate-limited", rateLimitedEvent, StringComparison.Ordinal);
        Assert.Contains("retry: 6000", rateLimitedEvent, StringComparison.Ordinal);

        var resetEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-control-reset", resetEvent, StringComparison.Ordinal);
        Assert.Contains("event: reset", resetEvent, StringComparison.Ordinal);

        var unauthorizedEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("id: sse-control-unauthorized", unauthorizedEvent, StringComparison.Ordinal);
        Assert.Contains("event: unauthorized", unauthorizedEvent, StringComparison.Ordinal);
        Assert.Contains("data: Session credentials expired. Reauthenticate before reconnecting.", unauthorizedEvent, StringComparison.Ordinal);

        var doneEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("event: done", doneEvent, StringComparison.Ordinal);
        Assert.Contains("data: ", doneEvent, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("stale", "sse-control-stale", "Stale signal received")]
    [InlineData("rate-limited", "sse-control-rate-limited", "Rate-limited signal received")]
    [InlineData("reset", "sse-control-reset", "Reset signal received")]
    [InlineData("unauthorized", "sse-control-unauthorized", "Unauthorized signal received")]
    public async Task SseControlEventPanelRoute_ReturnsEventSpecificFragment(
        string eventName,
        string elementId,
        string expectedTitle)
    {
        using var client = CreateClient();

        var response = await client.GetAsync($"/demos/sse/control-events/panels/{eventName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"id=\"{elementId}\"", body, StringComparison.Ordinal);
        Assert.Contains($"data-control-event=\"{eventName}\"", body, StringComparison.Ordinal);
        Assert.Contains(expectedTitle, body, StringComparison.Ordinal);
        Assert.Contains($"hx-trigger=\"sse:{eventName}\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SseStream_WithLastEventId_RendersResumeDetails()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/sse/stream");
        request.Headers.Add("Last-Event-ID", "sse-demo-2");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var firstEvent = await ReadEventBlockAsync(reader);

        Assert.Contains("Reconnect requested from event sse-demo-2.", firstEvent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotificationsRoute_ReturnsExplicitSseMarkup()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/notifications");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Notifications Center</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-ext=\"sse\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-connect=\"/demos/notifications/stream\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-close=\"done\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-swap=\"message\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"notifications-unread-badge\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotificationsStream_ReturnsTenIncrementalHtmlWithOobAndDoneEvent()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/notifications/stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var firstEvent = await ReadEventBlockAsync(reader);

        var events = new List<string>
        {
            firstEvent
        };

        for (var index = 1; index < 10; index++)
        {
            events.Add(await ReadEventBlockAsync(reader));
        }

        Assert.Contains("id: notif-01", firstEvent, StringComparison.Ordinal);
        Assert.Contains("New comment on deployment review", firstEvent, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", firstEvent, StringComparison.Ordinal);
        Assert.Contains("id=\"notifications-unread-indicator\"", firstEvent, StringComparison.Ordinal);
        Assert.Contains("notifications-unread-badge\">1</span>", firstEvent, StringComparison.Ordinal);
        Assert.Contains("Connected from the beginning of the demo stream.", firstEvent, StringComparison.Ordinal);

        Assert.All(events, static item => Assert.Contains("hx-swap-oob=\"outerHTML\"", item, StringComparison.Ordinal));
        Assert.Contains("id: notif-09", events[8], StringComparison.Ordinal);
        Assert.Contains("P1 incident declared for EU auth", events[8], StringComparison.Ordinal);
        Assert.Contains("notification-card--urgent", events[8], StringComparison.Ordinal);

        var lastEvent = events[9];
        Assert.Contains("id: notif-10", lastEvent, StringComparison.Ordinal);
        Assert.Contains("EU auth incident resolved", lastEvent, StringComparison.Ordinal);
        Assert.Contains("notifications-unread-badge\">10</span>", lastEvent, StringComparison.Ordinal);
        Assert.Contains("10 / 10", lastEvent, StringComparison.Ordinal);
        Assert.Contains("notifications-state-card--success", lastEvent, StringComparison.Ordinal);

        var doneEvent = await ReadEventBlockAsync(reader);
        Assert.Contains("event: done", doneEvent, StringComparison.Ordinal);
        Assert.Contains("data: ", doneEvent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotificationsStream_WithLastEventId_RendersResumeDetails()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/notifications/stream");
        request.Headers.Add("Last-Event-ID", "notif-04");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        var firstEvent = await ReadEventBlockAsync(reader);

        Assert.Contains("Client requested resume after notif-04.", firstEvent, StringComparison.Ordinal);
    }
}
