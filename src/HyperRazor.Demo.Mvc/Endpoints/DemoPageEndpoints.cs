using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Demo.Mvc.Endpoints;

public static class DemoPageEndpoints
{
    public static IEndpointRouteBuilder MapDemoPages(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // AdminLayout routes stay on Minimal APIs so the demo shows page parity in a real app area.
        MapPage<DashboardPage>(endpoints, "/");
        MapPage<UsersPage>(endpoints, "/users");
        MapPage<ValidationPage>(endpoints, "/validation");
        MapPage<SsePage>(endpoints, "/demos/sse");
        MapPage<SseControlEventsPage>(endpoints, "/demos/sse/control-events");
        MapPage<SseReplayPage>(endpoints, "/demos/sse/replay");
        MapPage<NotificationsPage>(endpoints, "/demos/notifications");
        MapPage<BrandingSettingsPage>(endpoints, "/settings/branding");

        return endpoints;
    }

    private static void MapPage<TPage>(IEndpointRouteBuilder endpoints, string pattern)
        where TPage : IComponent
    {
        endpoints.MapGet(pattern, (HttpContext context, CancellationToken cancellationToken) =>
            HrzResults.Page<TPage>(context, cancellationToken: cancellationToken));
    }
}
