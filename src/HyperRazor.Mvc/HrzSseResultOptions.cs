namespace HyperRazor.Mvc;

public sealed class HrzSseResultOptions
{
    public TimeSpan? HeartbeatInterval { get; set; }

    public string HeartbeatComment { get; set; } = "keep-alive";

    public bool DisableProxyBuffering { get; set; } = true;
}
