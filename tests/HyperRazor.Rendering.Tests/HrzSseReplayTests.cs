using System.IO;
using System.Net.ServerSentEvents;
using HyperRazor;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Rendering.Tests;

public sealed class HrzSseReplayTests
{
    [Fact]
    public void ResumeContext_FromRequest_ReadsLastEventId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Last-Event-ID"] = "  evt-42  ";

        var resumeContext = HrzSseResumeContext.FromRequest(context.Request);

        Assert.True(resumeContext.HasLastEventId);
        Assert.Equal("evt-42", resumeContext.LastEventId);
    }

    [Fact]
    public void ReplayRequest_FromHttpContext_ReadsResumeContextAndStreamName()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Last-Event-ID"] = "evt-77";

        var request = HrzSseReplayRequest.FromHttpContext(context, "notifications");

        Assert.Same(context, request.HttpContext);
        Assert.Equal("notifications", request.StreamName);
        Assert.Equal("evt-77", request.ResumeContext.LastEventId);
    }

    [Fact]
    public async Task Compose_WhenLiveDecision_StreamsLiveOnly()
    {
        var ids = await ReadEventIdsAsync(HrzSseReplay.Compose(
            HrzSseReplayDecision.Live(),
            GetEvents("live-1", "live-2")));

        Assert.Equal(["live-1", "live-2"], ids);
    }

    [Fact]
    public async Task Compose_WhenReplayDecision_ReplaysThenContinuesLive()
    {
        var ids = await ReadEventIdsAsync(HrzSseReplay.Compose(
            HrzSseReplayDecision.Replay(GetEvents("replay-1", "replay-2")),
            GetEvents("live-1", "live-2")));

        Assert.Equal(["replay-1", "replay-2", "live-1", "live-2"], ids);
    }

    [Fact]
    public async Task Compose_WhenResetDecision_StreamsResetOnly()
    {
        var ids = await ReadEventIdsAsync(HrzSseReplay.Compose(
            HrzSseReplayDecision.Reset(GetEvents("reset-1")),
            GetEvents("live-1", "live-2")));

        Assert.Equal(["reset-1"], ids);
    }

    [Fact]
    public async Task DecideAsync_UsesRegisteredReplayStrategy()
    {
        var strategy = new TestReplayStrategy(_ => HrzSseReplayDecision.Replay(GetEvents("replay-1")));
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddHyperRazor()
            .AddScoped<IHrzSseReplayStrategy>(_ => strategy)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Headers["Last-Event-ID"] = "evt-9";

        var decision = await HrzSseReplay.DecideAsync(context, "notifications");

        Assert.Equal(HrzSseReplayDisposition.ReplayThenLive, decision.Disposition);
        Assert.NotNull(strategy.LastRequest);
        Assert.Equal("notifications", strategy.LastRequest.Value.StreamName);
        Assert.Equal("evt-9", strategy.LastRequest.Value.ResumeContext.LastEventId);
    }

    [Fact]
    public async Task Compose_WithHttpContext_UsesRegisteredReplayStrategy()
    {
        var strategy = new TestReplayStrategy(request =>
            request.ResumeContext.HasLastEventId
                ? HrzSseReplayDecision.Replay(GetEvents("replay-1"))
                : HrzSseReplayDecision.Live());
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddHyperRazor()
            .AddScoped<IHrzSseReplayStrategy>(_ => strategy)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Headers["Last-Event-ID"] = "evt-11";

        var ids = await ReadEventIdsAsync(HrzSseReplay.Compose(
            context,
            GetEvents("live-1", "live-2"),
            "notifications"));

        Assert.Equal(["replay-1", "live-1", "live-2"], ids);
        Assert.Equal("notifications", strategy.LastRequest?.StreamName);
    }

    [Fact]
    public void AddHyperRazor_RegistersDefaultReplayStrategy()
    {
        using var provider = new ServiceCollection()
            .AddLogging()
            .AddHyperRazor()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        var strategy = scope.ServiceProvider.GetRequiredService<IHrzSseReplayStrategy>();

        Assert.IsType<HrzDefaultSseReplayStrategy>(strategy);
    }

    [Fact]
    public async Task NoReconnect_Returns204NoContent()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        await HrzResults.NoReconnect().ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal(0, context.Response.Body.Length);
    }

    private static async Task<List<string?>> ReadEventIdsAsync(IAsyncEnumerable<SseItem<string>> source)
    {
        var ids = new List<string?>();

        await foreach (var item in source)
        {
            ids.Add(item.EventId);
        }

        return ids;
    }

    private static async IAsyncEnumerable<SseItem<string>> GetEvents(params string[] ids)
    {
        foreach (var id in ids)
        {
            yield return HrzSse.Message($"<div>{id}</div>", id: id);
        }

        await Task.CompletedTask;
    }

    private sealed class TestReplayStrategy : IHrzSseReplayStrategy
    {
        private readonly Func<HrzSseReplayRequest, HrzSseReplayDecision> _resolve;

        public TestReplayStrategy(Func<HrzSseReplayRequest, HrzSseReplayDecision> resolve)
        {
            _resolve = resolve;
        }

        public HrzSseReplayRequest? LastRequest { get; private set; }

        public ValueTask<HrzSseReplayDecision> DecideAsync(
            HrzSseReplayRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return ValueTask.FromResult(_resolve(request));
        }
    }
}
