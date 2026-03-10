using System.Net.ServerSentEvents;
using System.Text;

namespace HyperRazor.Rendering;

public static class HrzSse
{
    private const string DefaultCloseEventType = "done";
    private const string DefaultHeartbeatComment = "keep-alive";

    /// <summary>
    /// Creates an unnamed SSE message for the default HTML swap path.
    /// </summary>
    public static SseItem<string> Message(
        string data,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return Create(data, eventType: null, id, retryAfter);
    }

    /// <summary>
    /// Creates a named SSE event for advanced control flows such as close or resume signals.
    /// </summary>
    public static SseItem<string> Named(
        string eventType,
        string data,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("A named SSE event type is required.", nameof(eventType));
        }

        return Create(data, eventType, id, retryAfter);
    }

    /// <summary>
    /// Creates a blank-data close event that still dispatches in compliant SSE clients.
    /// </summary>
    public static SseItem<string> Close(
        string eventType = DefaultCloseEventType,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("A close event type is required.", nameof(eventType));
        }

        return Create(string.Empty, eventType, id, retryAfter);
    }

    /// <summary>
    /// Writes a heartbeat comment frame to an SSE stream.
    /// </summary>
    public static Task WriteHeartbeatAsync(
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        return WriteCommentAsync(destination, DefaultHeartbeatComment, cancellationToken);
    }

    /// <summary>
    /// Writes a raw SSE comment frame to a stream.
    /// </summary>
    public static async Task WriteCommentAsync(
        Stream destination,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);

        if (ContainsLineBreak(comment))
        {
            throw new ArgumentException("SSE comments cannot contain line breaks.", nameof(comment));
        }

        var payload = string.IsNullOrEmpty(comment)
            ? ":\n\n"
            : $": {comment}\n\n";

        var bytes = Encoding.UTF8.GetBytes(payload);
        await destination.WriteAsync(bytes, cancellationToken);
    }

    private static SseItem<string> Create(
        string data,
        string? eventType,
        string? id,
        TimeSpan? retryAfter)
    {
        ArgumentNullException.ThrowIfNull(data);

        var item = new SseItem<string>(data, eventType)
        {
            EventId = id,
            ReconnectionInterval = retryAfter
        };

        return item;
    }

    private static bool ContainsLineBreak(string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value.IndexOfAny(['\r', '\n']) >= 0;
    }
}
