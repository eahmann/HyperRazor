using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HyperRazor.Htmx;

public static class HyperRazorHtmxServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared HTMX configuration used by HyperRazor.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configure">Optional HTMX configuration callback.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHtmx(
        this IServiceCollection services,
        Action<HtmxConfig>? configure = null)
    {
        return services.AddHyperRazorHtmx(configure);
    }

    /// <summary>
    /// Registers the shared HTMX configuration used by HyperRazor.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configure">Optional HTMX configuration callback.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHyperRazorHtmx(
        this IServiceCollection services,
        Action<HtmxConfig>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<HtmxConfig>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.RemoveAll<HtmxConfig>();
        services.AddSingleton(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<HtmxConfig>>().Value);
        services.TryAddSingleton<IHtmxRegistrationMarker, HtmxRegistrationMarker>();

        return services;
    }
}
