using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Htmx;

namespace HyperRazor.Demo.Mvc.Middleware;

public sealed class DemoChromeUpdatesMiddleware
{
    private readonly RequestDelegate _next;

    public DemoChromeUpdatesMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, IHrzSwapService swapService)
    {
        var request = context.HtmxRequest();
        if (HttpMethods.IsGet(context.Request.Method)
            && request.RequestType == HtmxRequestType.Partial
            && !request.IsHistoryRestoreRequest
            && DemoChromeState.IsPageChromeRoute(context.Request.Path))
        {
            var chromeState = DemoChromeState.Create(context);

            swapService.Replace<DemoChromeToolbar>(
                AppLayout.ChromeToolbarRegion,
                new
                {
                    chromeState.RouteLabel,
                    chromeState.LayoutName,
                    chromeState.Theme
                });

            swapService.Replace<DemoChromeSidebar>(
                AppLayout.ChromeSidebarRegion,
                new
                {
                    chromeState.ActiveSection,
                    chromeState.LayoutName
                });
        }

        await _next(context);
    }
}
