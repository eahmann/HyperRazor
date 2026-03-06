using Microsoft.Extensions.DependencyInjection;

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

        var config = new HtmxConfig();
        configure?.Invoke(config);

        services.AddSingleton(config);
        return services;
    }
}
