namespace HyperRazor.Htmx;

public sealed class HtmxRequest
{
    public bool IsHtmx { get; init; }

    public HtmxVersion Version { get; init; } = HtmxVersion.Unknown;

    public HtmxRequestType RequestType { get; init; } = HtmxRequestType.Unknown;

    public string? Target { get; init; }

    public string? Source { get; init; }

    public string? SourceElement => Source ?? Trigger;

    public string? Trigger { get; init; }

    public string? TriggerName { get; init; }

    public Uri? CurrentUrl { get; init; }

    public bool IsBoosted { get; init; }

    public bool IsHistoryRestoreRequest { get; init; }

    public bool IsPartialRequest => RequestType == HtmxRequestType.Partial;

    public bool IsFullRequest => RequestType == HtmxRequestType.Full;

    public static HtmxRequest FromHeaders(
        IReadOnlyDictionary<string, string?> headers,
        HtmxClientProfile clientProfile = HtmxClientProfile.Htmx2Defaults)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            normalized[header.Key] = header.Value;
        }

        var requestHeader = Read(normalized, HtmxHeaderNames.Request);
        var requestTypeHeader = Read(normalized, HtmxHeaderNames.RequestType);
        var sourceHeader = Read(normalized, HtmxHeaderNames.Source);
        var isHtmxLegacy = ParseTruthy(requestHeader);
        var isHistoryRestoreRequest = ParseTruthy(Read(normalized, HtmxHeaderNames.HistoryRestoreRequest));
        var isHtmx = isHtmxLegacy || !string.IsNullOrWhiteSpace(requestTypeHeader);

        return new HtmxRequest
        {
            IsHtmx = isHtmx,
            Version = ResolveVersion(clientProfile, isHtmxLegacy, requestTypeHeader, sourceHeader),
            RequestType = ResolveRequestType(requestTypeHeader, isHtmx, isHistoryRestoreRequest),
            Target = Read(normalized, HtmxHeaderNames.Target),
            Source = sourceHeader,
            Trigger = Read(normalized, HtmxHeaderNames.Trigger),
            TriggerName = Read(normalized, HtmxHeaderNames.TriggerName),
            CurrentUrl = ParseUri(Read(normalized, HtmxHeaderNames.CurrentUrl)),
            IsBoosted = ParseTruthy(Read(normalized, HtmxHeaderNames.Boosted)),
            IsHistoryRestoreRequest = isHistoryRestoreRequest
        };
    }

    private static HtmxVersion ResolveVersion(
        HtmxClientProfile clientProfile,
        bool isHtmxLegacy,
        string? requestTypeHeader,
        string? sourceHeader)
    {
        if (!string.IsNullOrWhiteSpace(requestTypeHeader) || !string.IsNullOrWhiteSpace(sourceHeader))
        {
            return HtmxVersion.Htmx4;
        }

        if (isHtmxLegacy)
        {
            return HtmxVersion.Htmx2;
        }

        return clientProfile switch
        {
            HtmxClientProfile.Htmx4Compat or HtmxClientProfile.Htmx4Native => HtmxVersion.Htmx4,
            _ => HtmxVersion.Htmx2
        };
    }

    private static HtmxRequestType ResolveRequestType(
        string? requestTypeHeader,
        bool isHtmx,
        bool isHistoryRestoreRequest)
    {
        var parsed = ParseRequestTypeHeader(requestTypeHeader);
        if (parsed != HtmxRequestType.Unknown)
        {
            return parsed;
        }

        if (isHistoryRestoreRequest)
        {
            return HtmxRequestType.Full;
        }

        if (isHtmx)
        {
            return HtmxRequestType.Partial;
        }

        return HtmxRequestType.Unknown;
    }

    private static HtmxRequestType ParseRequestTypeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return HtmxRequestType.Unknown;
        }

        if (value.Equals("partial", StringComparison.OrdinalIgnoreCase))
        {
            return HtmxRequestType.Partial;
        }

        if (value.Equals("full", StringComparison.OrdinalIgnoreCase))
        {
            return HtmxRequestType.Full;
        }

        return HtmxRequestType.Unknown;
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
