using HyperRazor.Htmx;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Htmx.AspNetCore;

public static class HyperRazorHtmxServiceCollectionExtensions
{
    public static IServiceCollection AddHtmx(
        this IServiceCollection services,
        Action<HtmxConfig>? configure = null)
    {
        return services.AddHyperRazorHtmx(configure);
    }

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
