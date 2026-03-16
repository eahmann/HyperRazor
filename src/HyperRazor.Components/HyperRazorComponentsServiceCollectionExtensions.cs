using HyperRazor.Components.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HyperRazor.Components;

public static class HyperRazorComponentsServiceCollectionExtensions
{
    public static IServiceCollection AddHyperRazorComponentServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<HrzFieldDescriptorFactory>();
        services.TryAddSingleton<HrzFieldValueProjector>();
        services.TryAddSingleton<HrzClientValidationMetadataFactory>();
        services.TryAddSingleton<HrzLiveMetadataFactory>();
        services.TryAddSingleton<IHrzValidationViewFactory, HrzValidationViewFactory>();
        services.TryAddScoped<IHrzForms>(serviceProvider =>
            new HrzForms(
                serviceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>(),
                serviceProvider.GetRequiredService<IHrzValidationViewFactory>()));

        return services;
    }
}
