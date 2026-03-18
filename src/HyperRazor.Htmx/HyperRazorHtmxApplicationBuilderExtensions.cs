using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
        HtmxVaryHeaders.EnsureForRequestBranching(headers);
    }
}
