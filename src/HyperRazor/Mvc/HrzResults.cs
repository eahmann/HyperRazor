using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
#if NET10_0_OR_GREATER
using Microsoft.Extensions.Options;
using System.Net.ServerSentEvents;
#endif

namespace HyperRazor.Mvc;

public static class HrzResults
{
#if NET10_0_OR_GREATER
    private const string ProxyBufferingHeader = "X-Accel-Buffering";
    private const string ProxyBufferingDisabledValue = "no";
#endif

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
        return new HrzServerSentEventsResult<string>(source, options, configureResponse);
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
        var inner = await ResolveRenderService(context).Fragment<TComponent>(data, cancellationToken);
        return new HrzStatusResult(statusCode, inner);
    }

    private static IHrzRenderService ResolveRenderService(HttpContext context)
    {
        return HrzRegistrationRequirements.ResolveRenderService(context.RequestServices);
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

#if NET10_0_OR_GREATER
    private sealed class HrzServerSentEventsResult<T> : IResult
    {
        private readonly IAsyncEnumerable<SseItem<T>> _source;
        private readonly HrzSseResultOptions? _options;
        private readonly Action<HttpResponse>? _configureResponse;

        public HrzServerSentEventsResult(
            IAsyncEnumerable<SseItem<T>> source,
            HrzSseResultOptions? options,
            Action<HttpResponse>? configureResponse)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _options = options;
            _configureResponse = configureResponse;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var resolvedOptions = ResolveSseOptions(httpContext.RequestServices, _options);
            var response = httpContext.Response;

            ApplyDefaultSseHeaders(response, resolvedOptions);

            if (resolvedOptions.HeartbeatInterval is null)
            {
                _configureResponse?.Invoke(response);
                await TypedResults.ServerSentEvents(_source).ExecuteAsync(httpContext);
                return;
            }

            var heartbeatInterval = resolvedOptions.HeartbeatInterval.Value;
            ValidateHeartbeatInterval(heartbeatInterval);

            if (typeof(T) != typeof(string))
            {
                throw new InvalidOperationException("Heartbeat comments are only supported for string SSE streams.");
            }

            ApplyHeartbeatResponseDefaults(response);
            _configureResponse?.Invoke(response);
            await ExecuteWithHeartbeatsAsync(httpContext, heartbeatInterval, resolvedOptions.HeartbeatComment);
        }

        private async Task ExecuteWithHeartbeatsAsync(
            HttpContext httpContext,
            TimeSpan heartbeatInterval,
            string? heartbeatComment)
        {
            var cancellationToken = httpContext.RequestAborted;
            var response = httpContext.Response;

            await using var enumerator = _source.GetAsyncEnumerator(cancellationToken);
            using var timer = new PeriodicTimer(heartbeatInterval);

            var pendingMoveNext = enumerator.MoveNextAsync().AsTask();
            var pendingHeartbeat = timer.WaitForNextTickAsync(cancellationToken).AsTask();

            while (true)
            {
                var completed = await Task.WhenAny(pendingMoveNext, pendingHeartbeat);

                if (completed == pendingHeartbeat)
                {
                    if (await pendingHeartbeat)
                    {
                        await HrzSse.WriteCommentAsync(response.Body, heartbeatComment, cancellationToken);
                        await response.Body.FlushAsync(cancellationToken);
                    }

                    pendingHeartbeat = timer.WaitForNextTickAsync(cancellationToken).AsTask();
                    continue;
                }

                if (!await pendingMoveNext)
                {
                    break;
                }

                await SseFormatter.WriteAsync(YieldSingleItem((SseItem<string>)(object)enumerator.Current), response.Body, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
                pendingMoveNext = enumerator.MoveNextAsync().AsTask();
            }
        }

        private static async IAsyncEnumerable<SseItem<string>> YieldSingleItem(SseItem<string> item)
        {
            yield return item;
            await Task.CompletedTask;
        }

        private static void ValidateHeartbeatInterval(TimeSpan heartbeatInterval)
        {
            if (heartbeatInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(HrzSseOptions.HeartbeatInterval),
                    heartbeatInterval,
                    "HeartbeatInterval must be greater than TimeSpan.Zero.");
            }
        }
    }

    private static HrzSseOptions ResolveSseOptions(IServiceProvider services, HrzSseResultOptions? overrides)
    {
        var defaults = services.GetService<IOptions<HrzSseOptions>>()?.Value ?? new HrzSseOptions();

        return new HrzSseOptions
        {
            HeartbeatInterval = ResolveHeartbeatInterval(overrides, defaults),
            HeartbeatComment = overrides?.HeartbeatComment ?? defaults.HeartbeatComment,
            DisableProxyBuffering = overrides?.DisableProxyBuffering ?? defaults.DisableProxyBuffering
        };
    }

    private static TimeSpan? ResolveHeartbeatInterval(HrzSseResultOptions? overrides, HrzSseOptions defaults)
    {
        if (overrides?.DisableHeartbeat == true)
        {
            return null;
        }

        return overrides?.HeartbeatInterval ?? defaults.HeartbeatInterval;
    }

    private static void ApplyDefaultSseHeaders(HttpResponse response, HrzSseOptions options)
    {
        if (options.DisableProxyBuffering)
        {
            response.Headers[ProxyBufferingHeader] = ProxyBufferingDisabledValue;
        }
    }

    private static void ApplyHeartbeatResponseDefaults(HttpResponse response)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType ??= "text/event-stream";

        if (!response.Headers.ContainsKey("Cache-Control"))
        {
            response.Headers["Cache-Control"] = "no-cache,no-store";
        }
    }
#endif
}
