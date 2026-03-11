namespace HyperRazor.Rendering;

public enum HrzSseControlEvent
{
    Done,
    Unauthorized,
    Stale,
    RateLimited,
    Reset
}

public static class HrzSseControlEventExtensions
{
    public static string ToEventName(this HrzSseControlEvent controlEvent)
    {
        return controlEvent switch
        {
            HrzSseControlEvent.Done => HrzSseEventNames.Done,
            HrzSseControlEvent.Unauthorized => HrzSseEventNames.Unauthorized,
            HrzSseControlEvent.Stale => HrzSseEventNames.Stale,
            HrzSseControlEvent.RateLimited => HrzSseEventNames.RateLimited,
            HrzSseControlEvent.Reset => HrzSseEventNames.Reset,
            _ => throw new ArgumentOutOfRangeException(nameof(controlEvent), controlEvent, "Unknown HyperRazor SSE control event.")
        };
    }
}
