using HyperRazor.Components.Services;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;

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

        if (configure is null)
        {
            services.AddOptions<HrxOptions>();
        }
        else
        {
            services.Configure(configure);
        }

        services.AddScoped<IHrxHtmlRendererAdapter, HrxHtmlRendererAdapter>();
        services.AddScoped<IHrxComponentViewService, HrxComponentViewService>();
        services.AddScoped<IHrxSwapService, HrxSwapService>();

        return services;
    }
}
