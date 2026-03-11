namespace HyperRazor.Mvc;

public sealed class HrzSseResultOptions
{
    public TimeSpan? HeartbeatInterval { get; set; }

    public string? HeartbeatComment { get; set; }

    public bool? DisableProxyBuffering { get; set; }

    /// <summary>
    /// When <see langword="true"/>, disables heartbeat comments for this result even if a global
    /// <see cref="HrzSseOptions.HeartbeatInterval"/> default is configured.
    /// </summary>
    public bool DisableHeartbeat { get; set; }
}
