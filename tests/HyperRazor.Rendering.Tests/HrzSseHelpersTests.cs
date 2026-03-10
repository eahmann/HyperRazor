using System.IO;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Rendering.Tests;

public sealed class HrzSseHelpersTests
{
    [Fact]
    public void Message_CreatesUnnamedEventWithMetadata()
    {
        var item = HrzSse.Message(
            "<div>hello</div>",
            id: "evt-1",
            retryAfter: TimeSpan.FromSeconds(2));

        Assert.Equal("message", item.EventType);
        Assert.Equal("evt-1", item.EventId);
        Assert.Equal(TimeSpan.FromSeconds(2), item.ReconnectionInterval);
        Assert.Equal("<div>hello</div>", item.Data);
    }

    [Fact]
    public void Named_CreatesNamedEventWithMetadata()
    {
        var item = HrzSse.Named(
            "stale",
            "{}",
            id: "evt-2",
            retryAfter: TimeSpan.FromSeconds(5));

        Assert.Equal("stale", item.EventType);
        Assert.Equal("evt-2", item.EventId);
        Assert.Equal(TimeSpan.FromSeconds(5), item.ReconnectionInterval);
        Assert.Equal("{}", item.Data);
    }

    [Fact]
    public void Signal_CreatesBlankDataNamedEvent()
    {
        var item = HrzSse.Signal("stale", id: "evt-3");

        Assert.Equal("stale", item.EventType);
        Assert.Equal("evt-3", item.EventId);
        Assert.Equal(string.Empty, item.Data);
    }

    [Fact]
    public void Done_CreatesCanonicalCloseEvent()
    {
        var item = HrzSse.Done(id: "evt-done");

        Assert.Equal("done", item.EventType);
        Assert.Equal("evt-done", item.EventId);
        Assert.Equal(string.Empty, item.Data);
    }

    [Fact]
    public async Task WriteHeartbeatAsync_WritesKeepAliveCommentFrame()
    {
        await using var stream = new MemoryStream();

        await HrzSse.WriteHeartbeatAsync(stream);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var body = await reader.ReadToEndAsync();

        Assert.Equal(": keep-alive\n\n", body);
    }

    [Fact]
    public async Task WriteCommentAsync_WhenCommentContainsLineBreak_Throws()
    {
        await using var stream = new MemoryStream();

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            HrzSse.WriteCommentAsync(stream, "still\nalive"));

        Assert.Equal("comment", error.ParamName);
    }

    [Fact]
    public void GetLastEventId_NormalizesBlankValues()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Last-Event-ID"] = "  evt-7  ";

        Assert.Equal("evt-7", HrzSse.GetLastEventId(context.Request));

        context.Request.Headers["Last-Event-ID"] = "   ";
        Assert.Null(HrzSse.GetLastEventId(context.Request));
    }

    [Fact]
    public async Task ServerSentEvents_AppliesDefaultHeadersAndWritesEventStream()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var result = HrzResults.ServerSentEvents(GetSampleEvents());

        await result.ExecuteAsync(context);

        Assert.Contains("no-cache", context.Response.Headers.CacheControl.ToString(), StringComparison.Ordinal);
        Assert.Equal("no", context.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.StartsWith("text/event-stream", context.Response.ContentType, StringComparison.Ordinal);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("id: evt-1", body, StringComparison.Ordinal);
        Assert.Contains("data: <div>hello</div>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("event: message", body, StringComparison.Ordinal);
        Assert.Contains("event: done", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ServerSentEvents_WithHeartbeatInterval_EmitsHeartbeatComments()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var result = HrzResults.ServerSentEvents(
            GetDelayedEvents(),
            options: new HrzSseResultOptions
            {
                HeartbeatInterval = TimeSpan.FromMilliseconds(10),
                HeartbeatComment = "still-here"
            });

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains(": still-here", body, StringComparison.Ordinal);
        Assert.Contains("data: <div>late hello</div>", body, StringComparison.Ordinal);
        Assert.Contains("event: done", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ServerSentEvents_AllowsResponseCustomization()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var result = HrzResults.ServerSentEvents(
            GetSampleEvents(),
            response => response.Headers["X-Test-Stream"] = "custom");

        await result.ExecuteAsync(context);

        Assert.Equal("no", context.Response.Headers["X-Accel-Buffering"].ToString());
        Assert.Equal("custom", context.Response.Headers["X-Test-Stream"].ToString());
    }

    private static async IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<string>> GetSampleEvents()
    {
        yield return HrzSse.Message("<div>hello</div>", id: "evt-1");
        yield return HrzSse.Done();
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<System.Net.ServerSentEvents.SseItem<string>> GetDelayedEvents()
    {
        await Task.Delay(35);
        yield return HrzSse.Message("<div>late hello</div>", id: "evt-late");
        await Task.Delay(35);
        yield return HrzSse.Done();
        await Task.CompletedTask;
    }
}
