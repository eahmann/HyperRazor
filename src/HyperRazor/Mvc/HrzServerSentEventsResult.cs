#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.ServerSentEvents;
using HyperRazor.Rendering;

namespace HyperRazor.Mvc;

public sealed class HrzServerSentEventsResult : IResult
{
    private const string ProxyBufferingHeader = "X-Accel-Buffering";
    private const string ProxyBufferingDisabledValue = "no";

    private readonly IAsyncEnumerable<SseItem<string>> _source;
    private readonly HrzSseResultOptions? _options;
    private readonly Action<HttpResponse>? _configureResponse;

    public HrzServerSentEventsResult(
        IAsyncEnumerable<SseItem<string>> source,
        HrzSseResultOptions? options = null,
        Action<HttpResponse>? configureResponse = null)
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

            await SseFormatter.WriteAsync(YieldSingleItem(enumerator.Current), response.Body, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
            pendingMoveNext = enumerator.MoveNextAsync().AsTask();
        }
    }

    private static async IAsyncEnumerable<SseItem<string>> YieldSingleItem(SseItem<string> item)
    {
        yield return item;
        await Task.CompletedTask;
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
#endif
