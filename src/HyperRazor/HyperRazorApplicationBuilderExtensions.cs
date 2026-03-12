using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        List<string>? missingRegistrations = null;

        if (services.GetService<IOptions<HrzOptions>>() is null)
        {
            missingRegistrations = ["AddHyperRazor()"];
        }

        if (services.GetService<IHtmxRegistrationMarker>() is null)
        {
            missingRegistrations ??= [];
            missingRegistrations.Add("AddHtmx()");
        }

        if (missingRegistrations is null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"UseHyperRazor() requires explicit HyperRazor registration. Missing required registration(s): {string.Join(", ", missingRegistrations)}. Call builder.Services.AddHyperRazor() and builder.Services.AddHtmx() during startup.");
    }
}
