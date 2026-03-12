using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Mvc;

namespace HyperRazor.Demo.Mvc.Endpoints;

public static class DemoPageEndpoints
{
    public static IEndpointRouteBuilder MapDemoPages(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // AdminLayout routes stay on Minimal APIs so the demo shows page parity in a real app area.
        endpoints.MapPage<DashboardPage>("/");
        endpoints.MapPage<UsersPage>("/users");
        endpoints.MapPage<ValidationPage>("/validation");
        endpoints.MapPage<SsePage>("/demos/sse");
        endpoints.MapPage<SseControlEventsPage>("/demos/sse/control-events");
        endpoints.MapPage<SseReplayPage>("/demos/sse/replay");
        endpoints.MapPage<NotificationsPage>("/demos/notifications");
        endpoints.MapPage<BrandingSettingsPage>("/settings/branding");

        return endpoints;
    }
}
