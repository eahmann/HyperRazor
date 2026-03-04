using HyperRazor.Htmx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Htmx.AspNetCore;

public static class HyperRazorHtmxApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHtmxVary(this IApplicationBuilder app)
    {
        return app.UseHyperRazorHtmxVary();
    }

    public static IApplicationBuilder UseHyperRazorHtmxVary(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                if (IsHtmlResponse(context.Response))
                {
                    EnsureVaryByHtmxRequest(context.Response.Headers);
                }

                return Task.CompletedTask;
            });

            await next();
        });
    }

    private static bool IsHtmlResponse(HttpResponse response)
    {
        var contentType = response.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureVaryByHtmxRequest(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(HeaderNames.Vary, out var existingVary)
            || string.IsNullOrWhiteSpace(existingVary))
        {
            headers[HeaderNames.Vary] = HtmxHeaderNames.Request;
            return;
        }

        var values = existingVary
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Contains(HtmxHeaderNames.Request, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        headers[HeaderNames.Vary] = $"{existingVary}, {HtmxHeaderNames.Request}";
    }
}
