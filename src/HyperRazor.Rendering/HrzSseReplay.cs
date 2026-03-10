using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace HyperRazor.Rendering;

public static class HrzSseReplay
{
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
