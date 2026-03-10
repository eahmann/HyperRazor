namespace HyperRazor.Mvc;

public sealed class HrzSseResultOptions
{
    public TimeSpan? HeartbeatInterval { get; set; }

    public string? HeartbeatComment { get; set; }

    public bool? DisableProxyBuffering { get; set; }
}
