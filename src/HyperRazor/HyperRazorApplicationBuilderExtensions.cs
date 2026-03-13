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
        List<string>? missingRegistrations = null;

        if (services.GetService<IHrzComponentViewService>() is null)
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

        var callToAction = missingRegistrations.Count switch
        {
            1 => missingRegistrations[0],
            2 => $"{missingRegistrations[0]} and {missingRegistrations[1]}",
            _ => string.Join(" and ", missingRegistrations),
        };

        throw new InvalidOperationException(
            $"UseHyperRazor() requires explicit HyperRazor registration. Missing required registration(s): {string.Join(", ", missingRegistrations)}. Call {callToAction} on your IServiceCollection during startup (for example, services.AddHyperRazor(); services.AddHtmx(); or builder.Services.AddHyperRazor(); builder.Services.AddHtmx()).");
    }
}
