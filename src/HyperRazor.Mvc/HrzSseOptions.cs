namespace HyperRazor.Mvc;

public sealed class HrzSseOptions
{
    /// <summary>
    /// Optional global heartbeat cadence for string-based SSE streams.
    /// </summary>
    public TimeSpan? HeartbeatInterval { get; set; }

    /// <summary>
    /// Comment payload to emit for heartbeat frames. Empty comments still produce valid SSE comment frames.
    /// </summary>
    public string HeartbeatComment { get; set; } = "keep-alive";

    /// <summary>
    /// Whether HyperRazor should emit <c>X-Accel-Buffering: no</c> on SSE responses by default.
    /// </summary>
    public bool DisableProxyBuffering { get; set; } = true;
}
