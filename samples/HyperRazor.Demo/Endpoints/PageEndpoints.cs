using HyperRazor.Demo.Components.Pages;
using HyperRazor.Demo.Infrastructure;
using HyperRazor.Mvc;

namespace HyperRazor.Demo.Endpoints;

public static class PageEndpoints
{
    public static IEndpointRouteBuilder MapAppPages(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/", () => Results.Redirect("/portal"));

        endpoints.MapGet(
            "/portal",
            (HttpContext context, AppViewModelFactory models, string? workspace, CancellationToken cancellationToken) =>
                HrzResults.Page<PortalPage>(
                    context,
                    new
                    {
                        Model = models.CreatePortalPage(workspace)
                    },
                    cancellationToken: cancellationToken));

        endpoints.MapGet(
            "/users",
            (HttpContext context, AppViewModelFactory models, string? workspace, string? operation, CancellationToken cancellationToken) =>
                HrzResults.Page<UsersPage>(
                    context,
                    new
                    {
                        Model = models.CreateUsersPage(workspace, operationId: operation)
                    },
                    cancellationToken: cancellationToken));

        endpoints.MapGet(
            "/settings/branding",
            (HttpContext context, AppViewModelFactory models, string? workspace, CancellationToken cancellationToken) =>
                HrzResults.Page<BrandingPage>(
                    context,
                    new
                    {
                        Model = models.CreateBrandingPage(workspace)
                    },
                    cancellationToken: cancellationToken));

        return endpoints;
    }
}
