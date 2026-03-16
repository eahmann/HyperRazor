using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public readonly record struct HrzSseReplayRequest(
    HttpContext HttpContext,
    HrzSseResumeContext ResumeContext,
    string? StreamName = null)
{
    public static HrzSseReplayRequest FromHttpContext(HttpContext httpContext, string? streamName = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new HrzSseReplayRequest(
            httpContext,
            HrzSseResumeContext.FromRequest(httpContext.Request),
            streamName);
    }
}
