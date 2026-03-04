namespace HyperRazor.Htmx;

public sealed class HtmxRequest
{
    public bool IsHtmx { get; init; }

    public string? Target { get; init; }

    public string? Trigger { get; init; }

    public string? TriggerName { get; init; }

    public Uri? CurrentUrl { get; init; }

    public bool IsBoosted { get; init; }

    public bool IsHistoryRestoreRequest { get; init; }

    public static HtmxRequest FromHeaders(IReadOnlyDictionary<string, string?> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            normalized[header.Key] = header.Value;
        }

        return new HtmxRequest
        {
            IsHtmx = ParseTruthy(Read(normalized, HtmxHeaderNames.Request)),
            Target = Read(normalized, HtmxHeaderNames.Target),
            Trigger = Read(normalized, HtmxHeaderNames.Trigger),
            TriggerName = Read(normalized, HtmxHeaderNames.TriggerName),
            CurrentUrl = ParseUri(Read(normalized, HtmxHeaderNames.CurrentUrl)),
            IsBoosted = ParseTruthy(Read(normalized, HtmxHeaderNames.Boosted)),
            IsHistoryRestoreRequest = ParseTruthy(Read(normalized, HtmxHeaderNames.HistoryRestoreRequest))
        };
    }

    private static string? Read(IReadOnlyDictionary<string, string?> headers, string key)
    {
        if (!headers.TryGetValue(key, out var value))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool ParseTruthy(string? value)
    {
        return value is not null
            && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }

    private static Uri? ParseUri(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri : null;
    }
}
