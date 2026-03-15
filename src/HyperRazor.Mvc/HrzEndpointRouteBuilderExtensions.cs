using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace HyperRazor.Mvc;

public static class HrzEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a GET endpoint that renders a routable HyperRazor page component.
    /// </summary>
    public static RouteHandlerBuilder MapPage<TComponent>(this IEndpointRouteBuilder endpoints, string pattern)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints
            .MapGet(pattern, (HttpContext context, CancellationToken cancellationToken) =>
                HrzResults.Page<TComponent>(context, cancellationToken: cancellationToken));
    }

    /// <summary>
    /// Maps a GET endpoint that renders a HyperRazor fragment component.
    /// </summary>
    public static RouteHandlerBuilder MapFragment<TComponent>(this IEndpointRouteBuilder endpoints, string pattern)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapGet(pattern, (HttpContext context, CancellationToken cancellationToken) =>
            HrzResults.Fragment<TComponent>(context, cancellationToken: cancellationToken));
    }
}
