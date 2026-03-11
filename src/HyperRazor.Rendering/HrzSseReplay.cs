using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Rendering;

public static class HrzSseReplay
{
    public static ValueTask<HrzSseReplayDecision> DecideAsync(
        HttpContext httpContext,
        string? streamName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var strategy = httpContext.RequestServices.GetRequiredService<IHrzSseReplayStrategy>();
        var request = HrzSseReplayRequest.FromHttpContext(httpContext, streamName);
        return strategy.DecideAsync(request, cancellationToken);
    }

    public static async IAsyncEnumerable<SseItem<string>> Compose(
        HttpContext httpContext,
        IAsyncEnumerable<SseItem<string>> liveEvents,
        string? streamName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var decision = await DecideAsync(httpContext, streamName, cancellationToken);

        await foreach (var item in Compose(decision, liveEvents, cancellationToken).WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public static async IAsyncEnumerable<SseItem<string>> Compose(
        HrzSseReplayDecision decision,
        IAsyncEnumerable<SseItem<string>> liveEvents,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(liveEvents);

        switch (decision.Disposition)
        {
            case HrzSseReplayDisposition.Live:
                await foreach (var item in liveEvents.WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;

            case HrzSseReplayDisposition.ReplayThenLive:
                await foreach (var item in RequireEvents(decision).WithCancellation(cancellationToken))
                {
                    yield return item;
                }

                await foreach (var item in liveEvents.WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;

            case HrzSseReplayDisposition.Reset:
                await foreach (var item in RequireEvents(decision).WithCancellation(cancellationToken))
                {
                    yield return item;
                }
                yield break;

            default:
                throw new ArgumentOutOfRangeException(nameof(decision), decision.Disposition, "Unknown HyperRazor SSE replay disposition.");
        }
    }

    private static IAsyncEnumerable<SseItem<string>> RequireEvents(HrzSseReplayDecision decision)
    {
        return decision.Events
            ?? throw new InvalidOperationException($"Replay disposition '{decision.Disposition}' requires a buffered event source.");
    }
}
