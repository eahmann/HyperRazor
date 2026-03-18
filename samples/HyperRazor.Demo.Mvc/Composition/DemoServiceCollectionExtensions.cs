using HyperRazor;
using HyperRazor.Components;
using HyperRazor.Components.Validation;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Composition;

public static class DemoServiceCollectionExtensions
{
    public static IServiceCollection AddDemoMvc(this IServiceCollection services)
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
        services.AddHyperRazor(
            options =>
            {
                options.RootComponent = typeof(HrzApp<AppLayout>);
            },
            configureHtmx: htmx =>
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
        services.AddScoped<IHrzSseReplayStrategy, DemoSseReplayStrategy>();
        services.AddSingleton<IInviteValidationBackend, DemoInviteValidationBackend>();
        services.AddSingleton<IHrzLiveValidationPolicyResolver, DemoValidationLivePolicyResolver>();

        return services;
    }
}
