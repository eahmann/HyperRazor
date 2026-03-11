using System.Net.ServerSentEvents;

namespace HyperRazor.Rendering;

public enum HrzSseReplayDisposition
{
    Live,
    ReplayThenLive,
    Reset
}

public sealed class HrzSseReplayDecision
{
    private HrzSseReplayDecision(
        HrzSseReplayDisposition disposition,
        IAsyncEnumerable<SseItem<string>>? events)
    {
        Disposition = disposition;
        Events = events;
    }

    public HrzSseReplayDisposition Disposition { get; }

    public IAsyncEnumerable<SseItem<string>>? Events { get; }

    public static HrzSseReplayDecision Live()
    {
        return new HrzSseReplayDecision(HrzSseReplayDisposition.Live, events: null);
    }

    public static HrzSseReplayDecision Replay(IAsyncEnumerable<SseItem<string>> replayEvents)
    {
        ArgumentNullException.ThrowIfNull(replayEvents);

        return new HrzSseReplayDecision(HrzSseReplayDisposition.ReplayThenLive, replayEvents);
    }

    public static HrzSseReplayDecision Reset(IAsyncEnumerable<SseItem<string>> resetEvents)
    {
        ArgumentNullException.ThrowIfNull(resetEvents);

        return new HrzSseReplayDecision(HrzSseReplayDisposition.Reset, resetEvents);
    }
}
