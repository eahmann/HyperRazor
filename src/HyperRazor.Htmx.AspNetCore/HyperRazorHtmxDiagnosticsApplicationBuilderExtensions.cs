using HyperRazor.Htmx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Htmx.AspNetCore;

public static class HyperRazorHtmxDiagnosticsApplicationBuilderExtensions
{
    public static IApplicationBuilder UseHyperRazorDiagnostics(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var environment = app.ApplicationServices.GetService<IHostEnvironment>();
        var config = app.ApplicationServices.GetService<HtmxConfig>();
        var enabled = (config?.EnableDiagnosticsInDevelopment ?? true)
            && (environment?.IsDevelopment() ?? false);

        if (!enabled)
        {
            return app;
        }

        var loggerFactory = app.ApplicationServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("HyperRazor.Htmx.Diagnostics");

        return app.Use(async (context, next) =>
        {
            var request = context.HtmxRequest();
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["hrx.is_htmx"] = request.IsHtmx,
                ["hrx.version"] = request.Version.ToString(),
                ["hrx.request_type"] = request.RequestType.ToString(),
                ["hrx.source"] = request.SourceElement,
                ["hrx.target"] = request.Target,
                ["hrx.current_url"] = request.CurrentUrl?.ToString()
            });

            await next();

            logger.LogInformation(
                "HTMX request processed: {Method} {Path} => {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        });
    }
}
