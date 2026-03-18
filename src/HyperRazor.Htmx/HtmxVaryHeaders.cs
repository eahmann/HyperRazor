using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Htmx;

public static class HtmxVaryHeaders
{
    public static void EnsureForRequestBranching(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        EnsureBy(headers, HtmxHeaderNames.Request);
        EnsureBy(headers, HtmxHeaderNames.RequestType);
        EnsureBy(headers, HtmxHeaderNames.HistoryRestoreRequest);
    }

    public static void EnsureBy(IHeaderDictionary headers, string varyHeader)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentException.ThrowIfNullOrWhiteSpace(varyHeader);

        if (!headers.TryGetValue(HeaderNames.Vary, out var existingVary)
            || string.IsNullOrWhiteSpace(existingVary))
        {
            headers[HeaderNames.Vary] = varyHeader;
            return;
        }

        var values = existingVary
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Contains(varyHeader, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        headers[HeaderNames.Vary] = $"{existingVary}, {varyHeader}";
    }
}
