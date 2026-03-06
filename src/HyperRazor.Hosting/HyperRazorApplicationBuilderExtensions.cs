using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Builder;

namespace HyperRazor.Hosting;

public static class HyperRazorApplicationBuilderExtensions
{
    /// <summary>
    /// Enables HyperRazor's HTMX-aware middleware pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseHyperRazor(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseHyperRazorDiagnostics();
        return app.UseHyperRazorHtmxVary();
    }
}
