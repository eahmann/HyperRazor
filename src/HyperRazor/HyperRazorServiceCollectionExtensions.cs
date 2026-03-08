using HyperRazor.Components.Services;
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
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHyperRazor(
        this IServiceCollection services,
        Action<HrzOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddRazorComponents();
        services.AddOptions<HrzOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
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
        services.AddSingleton<IHrzLayoutFamilyResolver, HrzLayoutFamilyResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<IHrzSwapService, HrzSwapService>();

        return services;
    }
}
