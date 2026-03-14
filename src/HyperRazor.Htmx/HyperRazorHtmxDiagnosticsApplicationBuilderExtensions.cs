using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Htmx;

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
                ["hrz.is_htmx"] = request.IsHtmx,
                ["hrz.version"] = request.Version.ToString(),
                ["hrz.request_type"] = request.RequestType.ToString(),
                ["hrz.source"] = request.SourceElement,
                ["hrz.target"] = request.Target,
                ["hrz.current_url"] = request.CurrentUrl?.ToString()
            });

            await next();

            if (context.Items.TryGetValue(typeof(HtmxPageNavigationDiagnostics), out var diagnosticsValue)
                && diagnosticsValue is HtmxPageNavigationDiagnostics navigationDiagnostics)
            {
                logger.LogInformation(
                    "HTMX request processed: {Method} {Path} => {StatusCode}; currentUrl={CurrentUrl}; sourceLayout={SourceLayout}; targetLayout={TargetLayout}; mode={Mode}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    Display(navigationDiagnostics.CurrentUrl),
                    Display(navigationDiagnostics.SourceLayout),
                    Display(navigationDiagnostics.TargetLayout),
                    navigationDiagnostics.Mode);
                return;
            }

            logger.LogInformation(
                "HTMX request processed: {Method} {Path} => {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode);
        });
    }

    private static string Display(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(none)" : value;
    }
}
