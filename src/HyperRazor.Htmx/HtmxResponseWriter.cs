using System.Text.Json;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Htmx.AspNetCore;

public sealed class HtmxResponseWriter
{
    private readonly IHeaderDictionary _headers;

    public HtmxResponseWriter(IHeaderDictionary headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public HtmxResponseWriter Apply(HtmxResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        foreach (var header in response.Headers)
        {
            _headers[header.Key] = header.Value;
        }

        return this;
    }

    public HtmxResponseWriter Redirect(string url) => Set(HtmxHeaderNames.Redirect, RequireValue(url, nameof(url)));

    public HtmxResponseWriter Location(string url) => Set(HtmxHeaderNames.Location, RequireValue(url, nameof(url)));

    public HtmxResponseWriter Location(object options) => Set(HtmxHeaderNames.Location, SerializeJson(options, nameof(options)));

    public HtmxResponseWriter PushUrl(string url) => Set(HtmxHeaderNames.PushUrl, RequireValue(url, nameof(url)));

    public HtmxResponseWriter PushUrl(bool pushUrl) => Set(HtmxHeaderNames.PushUrl, pushUrl ? "true" : "false");

    public HtmxResponseWriter ReplaceUrl(string url) => Set(HtmxHeaderNames.ReplaceUrl, RequireValue(url, nameof(url)));

    public HtmxResponseWriter ReplaceUrl(bool replaceUrl) => Set(HtmxHeaderNames.ReplaceUrl, replaceUrl ? "true" : "false");

    public HtmxResponseWriter Refresh() => Refresh(true);

    public HtmxResponseWriter Refresh(bool refresh) => Set(HtmxHeaderNames.Refresh, refresh ? "true" : "false");

    public HtmxResponseWriter Retarget(string cssSelector) => Set(HtmxHeaderNames.Retarget, RequireValue(cssSelector, nameof(cssSelector)));

    public HtmxResponseWriter Reswap(string value) => Set(HtmxHeaderNames.Reswap, RequireValue(value, nameof(value)));

    public HtmxResponseWriter Reselect(string cssSelector) => Set(HtmxHeaderNames.Reselect, RequireValue(cssSelector, nameof(cssSelector)));

    public HtmxResponseWriter Trigger(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerResponse, BuildTriggerValue(name, payload));

    public HtmxResponseWriter Trigger(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerResponse, BuildTriggerValue(events, nameof(events)));

    public HtmxResponseWriter TriggerAfterSwap(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerAfterSwap, BuildTriggerValue(name, payload));

    public HtmxResponseWriter TriggerAfterSwap(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerAfterSwap, BuildTriggerValue(events, nameof(events)));

    public HtmxResponseWriter TriggerAfterSettle(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerAfterSettle, BuildTriggerValue(name, payload));

    public HtmxResponseWriter TriggerAfterSettle(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerAfterSettle, BuildTriggerValue(events, nameof(events)));

    private HtmxResponseWriter Set(string headerName, string value)
    {
        _headers[headerName] = value;
        return this;
    }

    private static string RequireValue(string value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", argumentName);
        }

        return value;
    }

    private static string BuildTriggerValue(string name, object? payload)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        if (payload is null)
        {
            return name;
        }

        var eventPayload = new Dictionary<string, object?> { [name] = payload };
        return JsonSerializer.Serialize(eventPayload);
    }

    private static string BuildTriggerValue(IReadOnlyDictionary<string, object?> events, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(events, argumentName);

        if (events.Count == 0)
        {
            throw new ArgumentException("At least one event is required.", argumentName);
        }

        return JsonSerializer.Serialize(events);
    }

    private static string SerializeJson(object value, string argumentName)
    {
        ArgumentNullException.ThrowIfNull(value, argumentName);
        return JsonSerializer.Serialize(value);
    }
}
