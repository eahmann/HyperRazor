using HyperRazor;
using HyperRazor.Components;
using HyperRazor.Demo.Components.Layouts;
using HyperRazor.Demo.Infrastructure;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSampleApp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "RequestVerificationToken";
        });

        services.AddHyperRazor(options =>
        {
            options.RootComponent = typeof(HrzApp<AppLayout>);
        });

        services.AddHtmx(htmx =>
        {
            htmx.AllowNestedOobSwaps = false;
            htmx.EnableHeadSupport = true;
            htmx.ResponseHandling =
            [
                new HtmxResponseHandlingRule
                {
                    Code = "204",
                    Swap = false
                },
                new HtmxResponseHandlingRule
                {
                    Code = "[23]..",
                    Swap = true
                },
                new HtmxResponseHandlingRule
                {
                    Code = "[45]..",
                    Swap = true,
                    Error = false
                }
            ];
        });

        services.AddSingleton<WorkspaceCatalog>();
        services.AddSingleton<PeopleDirectoryService>();
        services.AddSingleton<ProvisioningStore>();
        services.AddScoped<AppViewModelFactory>();

        return services;
    }
}
