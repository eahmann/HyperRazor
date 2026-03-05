using HyperRazor.Components.Services;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Hosting;

public static class HyperRazorServiceCollectionExtensions
{
    public static IServiceCollection AddHyperRazor(
        this IServiceCollection services,
        Action<HrxOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddRazorComponents();
        services.AddOptions<HrxOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }
        services.AddOptions<HrxSwapOptions>()
            .Configure<IOptions<HrxOptions>>((swapOptions, hrxOptions) =>
            {
                swapOptions.AllowRawContentOnNonHtmx = hrxOptions.Value.AllowRawContentOnNonHtmx;
            });

        services.AddScoped<IHrxHtmlRendererAdapter, HrxHtmlRendererAdapter>();
        services.AddScoped<IHrxComponentViewService, HrxComponentViewService>();
        services.AddSingleton<IHrxLayoutFamilyResolver, HrxLayoutFamilyResolver>();
        services.AddScoped<IHrxHeadService, HrxHeadService>();
        services.AddScoped<IHrxSwapService, HrxSwapService>();

        return services;
    }
}
