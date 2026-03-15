using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public static class DemoInspectorUpdates
{
    public static void Queue(HttpContext context, string action, string details)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(details);

        var swapService = context.RequestServices.GetRequiredService<IHrzSwapService>();
        swapService.Replace<HxRequestResponseInspector>(
            AppLayout.InspectorRegion,
            BuildParameters(context, action, details));
    }

    private static IReadOnlyDictionary<string, object?> BuildParameters(
        HttpContext context,
        string action,
        string details)
    {
        var requestHeaders = context.Request.Headers;
        var responseHeaders = context.Response.Headers;
        var parsedRequest = context.HtmxRequest();
        var route = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";

        return new Dictionary<string, object?>
        {
            [nameof(HxRequestResponseInspector.ActionName)] = action,
            [nameof(HxRequestResponseInspector.Details)] = details,
            [nameof(HxRequestResponseInspector.Route)] = route,
            [nameof(HxRequestResponseInspector.HxRequest)] = ReadHeader(requestHeaders, HtmxHeaderNames.Request),
            [nameof(HxRequestResponseInspector.HxRequestType)] = ReadHeader(requestHeaders, HtmxHeaderNames.RequestType),
            [nameof(HxRequestResponseInspector.HxTarget)] = ReadHeader(requestHeaders, HtmxHeaderNames.Target),
            [nameof(HxRequestResponseInspector.HxTrigger)] = ReadHeader(requestHeaders, HtmxHeaderNames.Trigger),
            [nameof(HxRequestResponseInspector.HxSource)] = ReadHeader(requestHeaders, HtmxHeaderNames.Source),
            [nameof(HxRequestResponseInspector.HxCurrentUrl)] = ReadHeader(requestHeaders, HtmxHeaderNames.CurrentUrl),
            [nameof(HxRequestResponseInspector.ParsedVersion)] = parsedRequest.Version.ToString(),
            [nameof(HxRequestResponseInspector.ParsedRequestType)] = parsedRequest.RequestType.ToString(),
            [nameof(HxRequestResponseInspector.HxTriggerResponse)] = ReadHeader(responseHeaders, HtmxHeaderNames.TriggerResponse),
            [nameof(HxRequestResponseInspector.HxRedirect)] = ReadHeader(responseHeaders, HtmxHeaderNames.Redirect),
            [nameof(HxRequestResponseInspector.HxLocation)] = ReadHeader(responseHeaders, HtmxHeaderNames.Location),
            [nameof(HxRequestResponseInspector.HxPushUrl)] = ReadHeader(responseHeaders, HtmxHeaderNames.PushUrl),
            [nameof(HxRequestResponseInspector.StatusCode)] = context.Response.StatusCode
        };
    }

    private static string ReadHeader(IHeaderDictionary headers, string key)
    {
        if (!headers.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return "(none)";
        }

        return value.ToString();
    }
}
