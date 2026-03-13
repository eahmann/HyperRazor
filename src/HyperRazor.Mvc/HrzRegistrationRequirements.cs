using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Mvc;

internal static class HrzRegistrationRequirements
{
    private const string HyperRazorRegistration = "AddHyperRazor()";
    private const string HtmxRegistration = "AddHtmx()";

    public static IHrzComponentViewService ResolveViewService(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        EnsureConfigured(services, "Rendering a HyperRazor page or partial");
        return services.GetRequiredService<IHrzComponentViewService>();
    }

    public static void EnsureConfigured(IServiceProvider services, string operation)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);

        List<string>? missingRegistrations = null;

        if (services.GetService<IOptions<HrzOptions>>() is null)
        {
            missingRegistrations = [HyperRazorRegistration];
        }

        if (services.GetService<IHtmxRegistrationMarker>() is null)
        {
            missingRegistrations ??= [];
            missingRegistrations.Add(HtmxRegistration);
        }

        if (missingRegistrations is null)
        {
            return;
        }

        string registrationGuidance = missingRegistrations.Count == 1
            ? $"Call services.{missingRegistrations[0]} during startup."
            : $"Call services.{missingRegistrations[0]} and services.{missingRegistrations[1]} during startup.";

        throw new InvalidOperationException(
            $"{operation} requires explicit HyperRazor registration. Missing required registration(s): {string.Join(", ", missingRegistrations)}. {registrationGuidance}");
    }
}
