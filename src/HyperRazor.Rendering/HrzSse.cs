using System.Net.ServerSentEvents;

namespace HyperRazor.Rendering;

public static class HrzSse
{
    public static SseItem<string> Close(
        string eventType = "done",
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("A close event type is required.", nameof(eventType));
        }

        return new SseItem<string>(string.Empty, eventType)
        {
            EventId = id,
            ReconnectionInterval = retryAfter
        };
    }
}
