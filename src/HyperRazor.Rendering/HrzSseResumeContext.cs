using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public readonly record struct HrzSseResumeContext(string? LastEventId)
{
    public bool HasLastEventId => !string.IsNullOrEmpty(LastEventId);

    public static HrzSseResumeContext FromRequest(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new HrzSseResumeContext(HrzSse.GetLastEventId(request));
    }
}
