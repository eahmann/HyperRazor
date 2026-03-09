using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Htmx;

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
                    EnsureVaryByHtmxBranching(context.Response.Headers);
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

    private static void EnsureVaryByHtmxBranching(IHeaderDictionary headers)
    {
        EnsureVaryBy(headers, HtmxHeaderNames.Request);
        EnsureVaryBy(headers, HtmxHeaderNames.RequestType);
        EnsureVaryBy(headers, HtmxHeaderNames.HistoryRestoreRequest);
    }

    private static void EnsureVaryBy(IHeaderDictionary headers, string varyHeader)
    {
        if (!headers.TryGetValue(HeaderNames.Vary, out var existingVary)
            || string.IsNullOrWhiteSpace(existingVary))
        {
            headers[HeaderNames.Vary] = varyHeader;
            return;
        }

        var values = existingVary
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Contains(varyHeader, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        headers[HeaderNames.Vary] = $"{existingVary}, {varyHeader}";
    }
}
