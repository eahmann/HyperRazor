using System.Net.ServerSentEvents;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public static class HrzSse
{
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
    /// Creates a named SSE control event using HyperRazor's canonical event names.
    /// </summary>
    public static SseItem<string> Named(
        HrzSseControlEvent controlEvent,
        string data,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return Named(controlEvent.ToEventName(), data, id, retryAfter);
    }

    /// <summary>
    /// Creates a blank-data named signal event.
    /// </summary>
    public static SseItem<string> Signal(
        string eventType,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new ArgumentException("A named SSE event type is required.", nameof(eventType));
        }

        return Create(string.Empty, eventType, id, retryAfter);
    }

    /// <summary>
    /// Creates a blank-data named control event using HyperRazor's canonical event names.
    /// </summary>
    public static SseItem<string> Signal(
        HrzSseControlEvent controlEvent,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return Signal(controlEvent.ToEventName(), id, retryAfter);
    }

    /// <summary>
    /// Creates HyperRazor's canonical SSE close signal.
    /// </summary>
    public static SseItem<string> Done(
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return Signal(HrzSseControlEvent.Done, id, retryAfter);
    }

    public static SseItem<string> Unauthorized(
        string? detail = null,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return CreateControlEvent(HrzSseControlEvent.Unauthorized, detail, id, retryAfter);
    }

    public static SseItem<string> Stale(
        string? detail = null,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return CreateControlEvent(HrzSseControlEvent.Stale, detail, id, retryAfter);
    }

    public static SseItem<string> RateLimited(
        string? detail = null,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return CreateControlEvent(HrzSseControlEvent.RateLimited, detail, id, retryAfter);
    }

    public static SseItem<string> Reset(
        string? detail = null,
        string? id = null,
        TimeSpan? retryAfter = null)
    {
        return CreateControlEvent(HrzSseControlEvent.Reset, detail, id, retryAfter);
    }

    /// <summary>
    /// Creates a blank-data close event that still dispatches in compliant SSE clients.
    /// </summary>
    public static SseItem<string> Close(
        string eventType = HrzSseEventNames.Done,
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

    /// <summary>
    /// Reads the standard SSE resume header and normalizes blank values to null.
    /// </summary>
    public static string? GetLastEventId(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var value = request.Headers["Last-Event-ID"].ToString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
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

    private static SseItem<string> CreateControlEvent(
        HrzSseControlEvent controlEvent,
        string? detail,
        string? id,
        TimeSpan? retryAfter)
    {
        return string.IsNullOrEmpty(detail)
            ? Signal(controlEvent, id, retryAfter)
            : Named(controlEvent, detail, id, retryAfter);
    }

    private static bool ContainsLineBreak(string? value)
    {
        return !string.IsNullOrEmpty(value)
            && value.IndexOfAny(['\r', '\n']) >= 0;
    }
}
