using HyperRazor;
using HyperRazor.Components;
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
        services.AddHyperRazor(options =>
        {
            options.RootComponent = typeof(HrzApp<AppLayout>);
            options.UseMinimalLayoutForHtmx = true;
            options.LayoutBoundary.Enabled = true;
            options.LayoutBoundary.OnlyBoostedRequests = true;
            options.LayoutBoundary.PromotionMode = HrzLayoutBoundaryPromotionMode.ShellSwap;
            options.LayoutBoundary.LayoutFamilyHeaderName = HtmxHeaderNames.LayoutFamily;
            options.LayoutBoundary.DefaultLayoutFamily = "admin";
            options.LayoutBoundary.ShellTargetSelector = "#hrz-app-shell";
            options.LayoutBoundary.ShellSwapStyle = "outerHTML";
            options.LayoutBoundary.ShellReselectSelector = "#hrz-app-shell";
            options.LayoutBoundary.AddVaryHeader = true;
        });
        services.AddScoped<IHrzSseReplayStrategy, DemoSseReplayStrategy>();
        services.AddHtmx(htmx =>
        {
            htmx.ClientProfile = HtmxClientProfile.Htmx2Defaults;
            htmx.SelfRequestsOnly = true;
            htmx.HistoryRestoreAsHxRequest = false;
            htmx.AllowNestedOobSwaps = false;
            htmx.DefaultSwapStyle = "outerHTML";
            htmx.EnableHeadSupport = true;
            htmx.AntiforgeryMetaName = "hrz-antiforgery";
            htmx.AntiforgeryHeaderName = "RequestVerificationToken";
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
        services.AddSingleton<IInviteValidationBackend, DemoInviteValidationBackend>();
        services.AddSingleton<IHrzLiveValidationPolicyResolver, DemoValidationLivePolicyResolver>();

        return services;
    }
}
