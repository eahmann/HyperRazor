using HyperRazor.Htmx.AspNetCore;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Mvc;

public static class HrxResults
{
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
        return new HrxStatusResult(statusCode, inner);
    }

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
        return new HrxStatusResult(statusCode, inner);
    }

    private static IHrxComponentViewService ResolveViewService(HttpContext context)
    {
        return context.RequestServices.GetRequiredService<IHrxComponentViewService>();
    }

    private sealed class HrxStatusResult : IResult
    {
        private readonly int _statusCode;
        private readonly IResult _inner;

        public HrxStatusResult(int statusCode, IResult inner)
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
