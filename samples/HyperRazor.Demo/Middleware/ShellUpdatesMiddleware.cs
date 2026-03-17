using HyperRazor.Components.Services;
using HyperRazor.Demo.Components;
using HyperRazor.Demo.Components.Layouts;
using HyperRazor.Htmx;

namespace HyperRazor.Demo.Middleware;

public sealed class ShellUpdatesMiddleware
{
    private readonly RequestDelegate _next;

    public ShellUpdatesMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, IHrzSwapService swapService)
    {
        var request = context.HtmxRequest();
        if (HttpMethods.IsGet(context.Request.Method)
            && request.RequestType == HtmxRequestType.Partial
            && !request.IsHistoryRestoreRequest
            && IsConsoleRoute(context.Request.Path))
        {
            var workspaceKey = context.Request.Query["workspace"].ToString().Trim();
            if (string.IsNullOrWhiteSpace(workspaceKey))
            {
                workspaceKey = "atlas";
            }

            swapService.Replace<NavLinks>(
                AppLayout.NavRegion,
                new
                {
                    WorkspaceKey = workspaceKey,
                    ActivePath = context.Request.Path.Value ?? "/users"
                });
        }

        await _next(context);
    }

    private static bool IsConsoleRoute(PathString path)
    {
        return string.Equals(path.Value, "/users", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path.Value, "/settings/branding", StringComparison.OrdinalIgnoreCase);
    }
}
