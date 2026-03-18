using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.ServerSentEvents;

namespace HyperRazor.Mvc;

public static class HrzResults
{
    /// <summary>
    /// Renders a routable page component, automatically returning full HTML or an HTMX fragment
    /// based on the current request.
    /// </summary>
    public static Task<IResult> Page<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        configureResponse?.Invoke(context.HtmxResponse());
        return ResolveRenderService(context).Page<TComponent>(data, cancellationToken);
    }

    /// <summary>
    /// Renders a fragment component without page-shell semantics, optionally customizing
    /// the HTMX response before rendering.
    /// </summary>
    public static Task<IResult> Fragment<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        configureResponse?.Invoke(context.HtmxResponse());
        return ResolveRenderService(context).Fragment<TComponent>(data, cancellationToken);
    }

    /// <summary>
    /// Renders one or more fragments without page-shell semantics, optionally customizing
    /// the HTMX response before rendering.
    /// </summary>
    public static Task<IResult> Fragment(
        HttpContext context,
        Action<HtmxResponseWriter>? configureResponse,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fragments);

        configureResponse?.Invoke(context.HtmxResponse());
        return ResolveRenderService(context).Fragment(cancellationToken, fragments);
    }

    /// <summary>
    /// Renders one or more fragments without page-shell semantics.
    /// </summary>
    public static Task<IResult> Fragment(
        HttpContext context,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments)
    {
        return Fragment(
            context,
            configureResponse: null,
            cancellationToken: cancellationToken,
            fragments: fragments);
    }

    /// <summary>
    /// Renders a page component and forces an app-root swap for HTMX partial requests.
    /// </summary>
    public static Task<IResult> RootSwap<TComponent>(
        HttpContext context,
        object? data = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        return ResolveRenderService(context).RootSwap<TComponent>(data, cancellationToken);
    }

    /// <summary>
    /// Returns a platform-backed SSE response with HyperRazor's default streaming headers.
    /// </summary>
#if NET10_0_OR_GREATER
    public static IResult ServerSentEvents(
        IAsyncEnumerable<SseItem<string>> source,
        Action<HttpResponse>? configureResponse = null,
        HrzSseResultOptions? options = null)
    {
        return new HrzServerSentEventsResult(source, options, configureResponse);
    }
#endif

    /// <summary>
    /// Returns an explicit 204 response to tell SSE clients not to reconnect.
    /// </summary>
    public static IResult NoReconnect()
    {
        return TypedResults.NoContent();
    }

    /// <summary>
    /// Renders a fragment component and applies the supplied status code.
    /// </summary>
    public static async Task<IResult> Validation<TComponent>(
        HttpContext context,
        object? data = null,
        int statusCode = StatusCodes.Status200OK,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        configureResponse?.Invoke(context.HtmxResponse());
        var inner = await ResolveRenderService(context).Fragment<TComponent>(data, cancellationToken);
        return new HrzStatusCodeResult(statusCode, inner);
    }

    /// <summary>
    /// Renders a fragment component with a 404 status code.
    /// </summary>
    public static Task<IResult> NotFound<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return WithStatus<TComponent>(
            context,
            StatusCodes.Status404NotFound,
            data,
            configureResponse,
            cancellationToken);
    }

    /// <summary>
    /// Renders a fragment component with a 403 status code.
    /// </summary>
    public static Task<IResult> Forbidden<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return WithStatus<TComponent>(
            context,
            StatusCodes.Status403Forbidden,
            data,
            configureResponse,
            cancellationToken);
    }

    /// <summary>
    /// Renders a fragment component with a 401 status code.
    /// </summary>
    public static Task<IResult> Unauthorized<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return WithStatus<TComponent>(
            context,
            StatusCodes.Status401Unauthorized,
            data,
            configureResponse,
            cancellationToken);
    }

    /// <summary>
    /// Renders a fragment component with a 500 status code.
    /// </summary>
    public static Task<IResult> ServerError<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        return WithStatus<TComponent>(
            context,
            StatusCodes.Status500InternalServerError,
            data,
            configureResponse,
            cancellationToken);
    }

    private static async Task<IResult> WithStatus<TComponent>(
        HttpContext context,
        int statusCode,
        object? data,
        Action<HtmxResponseWriter>? configureResponse,
        CancellationToken cancellationToken)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        configureResponse?.Invoke(context.HtmxResponse());
        var inner = await ResolveRenderService(context).Fragment<TComponent>(data, cancellationToken);
        return new HrzStatusCodeResult(statusCode, inner);
    }

    private static IHrzRenderService ResolveRenderService(HttpContext context)
    {
        return HrzRegistrationRequirements.ResolveRenderService(context.RequestServices);
    }
}
