using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor;

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
        EnsureConfigured(app.ApplicationServices);

        app.UseHyperRazorDiagnostics();
        return app.UseHyperRazorHtmxVary();
    }

    private static void EnsureConfigured(IServiceProvider services)
    {
        if (services.GetService<IHyperRazorRegistrationMarker>() is null)
        {
            throw new InvalidOperationException(
                "UseHyperRazor() requires AddHyperRazor() to be called on your IServiceCollection during startup. " +
                "AddHyperRazor() registers the HTMX defaults used by the top-level HyperRazor package. " +
                "Call services.AddHyperRazor() or builder.Services.AddHyperRazor() before building the app.");
        }
    }
}
