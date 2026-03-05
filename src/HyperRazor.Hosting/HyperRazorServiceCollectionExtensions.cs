using HyperRazor.Components.Services;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Hosting;

public static class HyperRazorServiceCollectionExtensions
{
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

        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzComponentViewService, HrzComponentViewService>();
        services.AddSingleton<IHrzLayoutFamilyResolver, HrzLayoutFamilyResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<IHrzSwapService, HrzSwapService>();

        return services;
    }
}
