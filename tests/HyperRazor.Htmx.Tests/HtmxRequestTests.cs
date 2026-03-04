using HyperRazor.Htmx;

namespace HyperRazor.Htmx.Tests;

public class HtmxRequestTests
{
    [Fact]
    public void FromHeaders_ParsesKnownHeaders_CaseInsensitive_AndTrimmed()
    {
        var headers = new Dictionary<string, string?>
        {
            ["hx-request"] = " true ",
            ["HX-TARGET"] = " #search-results ",
            ["Hx-Trigger"] = " keyup ",
            ["HX-TRIGGER-NAME"] = " query ",
            ["hx-current-url"] = " https://example.test/users?q=a ",
            ["hx-boosted"] = " true ",
            ["hx-history-restore-request"] = " 1 "
        };

        var request = HtmxRequest.FromHeaders(headers);

        Assert.True(request.IsHtmx);
        Assert.Equal("#search-results", request.Target);
        Assert.Equal("keyup", request.Trigger);
        Assert.Equal("query", request.TriggerName);
        Assert.Equal("https://example.test/users?q=a", request.CurrentUrl?.ToString());
        Assert.True(request.IsBoosted);
        Assert.True(request.IsHistoryRestoreRequest);
    }

    [Fact]
    public void FromHeaders_UsesSafeDefaults_WhenHeadersAreMissingOrFalsey()
    {
        var headers = new Dictionary<string, string?>
        {
            [HtmxHeaderNames.Request] = "false",
            [HtmxHeaderNames.Boosted] = "",
            [HtmxHeaderNames.HistoryRestoreRequest] = null,
            [HtmxHeaderNames.CurrentUrl] = "not-a-valid-uri"
        };

        var request = HtmxRequest.FromHeaders(headers);

        Assert.False(request.IsHtmx);
        Assert.False(request.IsBoosted);
        Assert.False(request.IsHistoryRestoreRequest);
        Assert.Null(request.Target);
        Assert.Null(request.Trigger);
        Assert.Null(request.TriggerName);
        Assert.Null(request.CurrentUrl);
    }
}
