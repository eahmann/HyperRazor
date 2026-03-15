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
        services.TryAddSingleton<IHyperRazorRegistrationMarker, HyperRazorRegistrationMarker>();

        services.TryAddSingleton<HrzFieldPathResolver>();
        services.TryAddSingleton<IHrzFieldPathResolver>(serviceProvider =>
            serviceProvider.GetRequiredService<HrzFieldPathResolver>());
        services.TryAddSingleton<IHrzModelValidator, HrzDataAnnotationsModelValidator>();
        services.TryAddSingleton<IHrzLiveValidationPolicyResolver, HrzDefaultLiveValidationPolicyResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHrzClientValidationMetadataProvider, HrzDataAnnotationsClientValidationMetadataProvider>());
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzRenderService, HrzRenderService>();
        services.AddScoped<IHrzSseRenderer, HrzSseRenderer>();
        services.TryAddScoped<IHrzSseReplayStrategy, HrzDefaultSseReplayStrategy>();
        services.AddSingleton<IHrzLayoutTypeResolver, HrzLayoutTypeResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<HrzSwapService>();
        services.AddScoped<IHrzSwapService>(serviceProvider => serviceProvider.GetRequiredService<HrzSwapService>());
        services.AddScoped<IHrzSwapBuffer>(serviceProvider => (IHrzSwapBuffer)serviceProvider.GetRequiredService<HrzSwapService>());

        return services;
    }
}
