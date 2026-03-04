using System.Text.Json;

namespace HyperRazor.Htmx;

public sealed class HtmxResponse
{
    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Headers => _headers;

    public HtmxResponse Redirect(string url) => Set(HtmxHeaderNames.Redirect, RequireValue(url, nameof(url)));

    public HtmxResponse Location(string url) => Set(HtmxHeaderNames.Location, RequireValue(url, nameof(url)));

    public HtmxResponse Location(object options) => Set(HtmxHeaderNames.Location, SerializeJson(options, nameof(options)));

    public HtmxResponse PushUrl(string url) => Set(HtmxHeaderNames.PushUrl, RequireValue(url, nameof(url)));

    public HtmxResponse PushUrl(bool pushUrl) => Set(HtmxHeaderNames.PushUrl, pushUrl ? "true" : "false");

    public HtmxResponse ReplaceUrl(string url) => Set(HtmxHeaderNames.ReplaceUrl, RequireValue(url, nameof(url)));

    public HtmxResponse ReplaceUrl(bool replaceUrl) => Set(HtmxHeaderNames.ReplaceUrl, replaceUrl ? "true" : "false");

    public HtmxResponse Refresh() => Refresh(true);

    public HtmxResponse Refresh(bool refresh) => Set(HtmxHeaderNames.Refresh, refresh ? "true" : "false");

    public HtmxResponse Retarget(string cssSelector) => Set(HtmxHeaderNames.Retarget, RequireValue(cssSelector, nameof(cssSelector)));

    public HtmxResponse Reswap(string value) => Set(HtmxHeaderNames.Reswap, RequireValue(value, nameof(value)));

    public HtmxResponse Reselect(string cssSelector) => Set(HtmxHeaderNames.Reselect, RequireValue(cssSelector, nameof(cssSelector)));

    public HtmxResponse Trigger(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerResponse, BuildTriggerValue(name, payload));

    public HtmxResponse Trigger(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerResponse, BuildTriggerValue(events, nameof(events)));

    public HtmxResponse TriggerAfterSwap(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerAfterSwap, BuildTriggerValue(name, payload));

    public HtmxResponse TriggerAfterSwap(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerAfterSwap, BuildTriggerValue(events, nameof(events)));

    public HtmxResponse TriggerAfterSettle(string name, object? payload = null) =>
        Set(HtmxHeaderNames.TriggerAfterSettle, BuildTriggerValue(name, payload));

    public HtmxResponse TriggerAfterSettle(IReadOnlyDictionary<string, object?> events) =>
        Set(HtmxHeaderNames.TriggerAfterSettle, BuildTriggerValue(events, nameof(events)));

    public void ApplyTo(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        foreach (var header in _headers)
        {
            headers[header.Key] = header.Value;
        }
    }

    private HtmxResponse Set(string headerName, string value)
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
