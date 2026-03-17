using HyperRazor.Components.Services;
using HyperRazor.Components.Validation;
using HyperRazor.Components;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HyperRazor;

public static class HyperRazorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core HyperRazor SSR services used to render page and fragment components.
    /// </summary>
    /// <param name="services">The service collection being configured.</param>
    /// <param name="configure">Optional HyperRazor rendering and layout options.</param>
#if NET10_0_OR_GREATER
    /// <param name="configureSse">Optional global SSE defaults for heartbeat and transport headers.</param>
#endif
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHyperRazor(
        this IServiceCollection services,
        Action<HrzOptions>? configure = null
#if NET10_0_OR_GREATER
        ,
        Action<HrzSseOptions>? configureSse = null
#endif
        )
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddRazorComponents();
        services.AddHyperRazorComponentServices();
        services.AddOptions<HrzOptions>();
#if NET10_0_OR_GREATER
        services.AddOptions<HrzSseOptions>();
#endif
        if (configure is not null)
        {
            services.Configure<HrzOptions>(configure);
        }
#if NET10_0_OR_GREATER
        if (configureSse is not null)
        {
            services.Configure<HrzSseOptions>(configureSse);
        }
#endif
        services.TryAddSingleton<IHyperRazorRegistrationMarker, HyperRazorRegistrationMarker>();

        services.TryAddSingleton<HrzFieldPathResolver>();
        services.TryAddSingleton<IHrzFieldPathResolver>(serviceProvider =>
            serviceProvider.GetRequiredService<HrzFieldPathResolver>());
        services.TryAddSingleton<IHrzModelValidator, HrzDataAnnotationsModelValidator>();
        services.TryAddSingleton<IHrzLiveValidationPolicyResolver, HrzDefaultLiveValidationPolicyResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHrzClientValidationMetadataProvider, HrzDataAnnotationsClientValidationMetadataProvider>());
        services.TryAddScoped<IHrzFormPostBinder, HrzFormPostBinder>();
        services.TryAddScoped<IHrzLiveValidationRequestBinder, HrzLiveValidationRequestBinder>();
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzRenderService, HrzRenderService>();
#if NET10_0_OR_GREATER
        services.AddScoped<IHrzSseRenderer, HrzSseRenderer>();
        services.TryAddScoped<IHrzSseReplayStrategy, HrzDefaultSseReplayStrategy>();
#endif
        services.AddSingleton<IHrzLayoutTypeResolver, HrzLayoutTypeResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<HrzSwapService>();
        services.AddScoped<IHrzSwapService>(serviceProvider => serviceProvider.GetRequiredService<HrzSwapService>());
        services.AddScoped<IHrzSwapBuffer>(serviceProvider => (IHrzSwapBuffer)serviceProvider.GetRequiredService<HrzSwapService>());

        return services;
    }
}
