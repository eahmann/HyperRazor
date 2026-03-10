using HyperRazor.Components.Services;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HyperRazor;

public static class HyperRazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core HyperRazor SSR services used to render page and fragment components.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configure">Optional HyperRazor rendering and layout options.</param>
    /// <param name="configureSse">Optional global SSE defaults for heartbeat and transport headers.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHyperRazor(
        this IServiceCollection services,
        Action<HrzOptions>? configure = null,
        Action<HrzSseOptions>? configureSse = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddRazorComponents();
        services.AddOptions<HrzOptions>();
        services.AddOptions<HrzSseOptions>();
        if (configure is not null)
        {
            services.Configure<HrzOptions>(configure);
        }
        if (configureSse is not null)
        {
            services.Configure<HrzSseOptions>(configureSse);
        }
        services.AddOptions<HrzSwapOptions>()
            .Configure<IOptions<HrzOptions>>((swapOptions, hrzOptions) =>
            {
                swapOptions.AllowRawContentOnNonHtmx = hrzOptions.Value.AllowRawContentOnNonHtmx;
            });

        services.TryAddSingleton<HrzFieldPathResolver>();
        services.TryAddSingleton<IHrzFieldPathResolver>(serviceProvider =>
            serviceProvider.GetRequiredService<HrzFieldPathResolver>());
        services.TryAddSingleton<IHrzModelValidator, HrzDataAnnotationsModelValidator>();
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzComponentViewService, HrzComponentViewService>();
        services.AddScoped<IHrzSseRenderer, HrzSseRenderer>();
        services.AddSingleton<IHrzLayoutFamilyResolver, HrzLayoutFamilyResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<IHrzSwapService, HrzSwapService>();

        return services;
    }
}
