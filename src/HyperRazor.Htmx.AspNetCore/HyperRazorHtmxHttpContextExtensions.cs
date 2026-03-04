using HyperRazor.Htmx;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Htmx.AspNetCore;

public static class HyperRazorHtmxHttpContextExtensions
{
    public static (HtmxRequest Request, HtmxResponseWriter Response) Htmx(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return (context.HtmxRequest(), context.HtmxResponse());
    }

    public static HtmxRequest HtmxRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Request.Headers)
        {
            headers[header.Key] = header.Value.ToString();
        }

        return HyperRazor.Htmx.HtmxRequest.FromHeaders(headers);
    }

    public static HtmxResponseWriter HtmxResponse(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new HtmxResponseWriter(context.Response.Headers);
    }
}
