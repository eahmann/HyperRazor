using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

internal static class HrzRegistrationRequirements
{
    private const string HyperRazorRegistration = "AddHyperRazor()";

    public static IHrzRenderService ResolveRenderService(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        EnsureConfigured(services, "Rendering a HyperRazor page or fragment");
        return services.GetRequiredService<IHrzRenderService>();
    }

    public static void EnsureConfigured(IServiceProvider services, string operation)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        if (services.GetService<IHyperRazorRegistrationMarker>() is null)
        {
            throw new InvalidOperationException(
                $"{operation} requires explicit HyperRazor registration. Call services.{HyperRazorRegistration} during startup. " +
                $"{HyperRazorRegistration} also registers the HTMX defaults used by the top-level HyperRazor package.");
        }
    }
}
