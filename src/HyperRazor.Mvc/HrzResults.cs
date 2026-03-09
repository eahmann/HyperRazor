using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        return ResolveViewService(context).View<TComponent>(data, cancellationToken);
    }

    /// <summary>
    /// Renders a fragment component without page-shell semantics.
    /// </summary>
    public static Task<IResult> Partial<TComponent>(
        HttpContext context,
        object? data = null,
        Action<HtmxResponseWriter>? configureResponse = null,
        CancellationToken cancellationToken = default)
        where TComponent : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);

        configureResponse?.Invoke(context.HtmxResponse());
        return ResolveViewService(context).PartialView<TComponent>(data, cancellationToken);
    }

    /// <summary>
    /// Renders one or more fragments without page-shell semantics.
    /// </summary>
    public static Task<IResult> Partial(
        HttpContext context,
        CancellationToken cancellationToken = default,
        params RenderFragment[] fragments)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(fragments);

        return ResolveViewService(context).PartialView(cancellationToken, fragments);
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
        var inner = await ResolveViewService(context).PartialView<TComponent>(data, cancellationToken);
        return new HrzStatusResult(statusCode, inner);
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
        var inner = await ResolveViewService(context).PartialView<TComponent>(data, cancellationToken);
        return new HrzStatusResult(statusCode, inner);
    }

    private static IHrzComponentViewService ResolveViewService(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IHrzComponentViewService>();
    }

    private sealed class HrzStatusResult : IResult
    {
        private readonly int _statusCode;
        private readonly IResult _inner;

        public HrzStatusResult(int statusCode, IResult inner)
        {
            _statusCode = statusCode;
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.StatusCode = _statusCode;
            await _inner.ExecuteAsync(httpContext);
        }
    }
}
