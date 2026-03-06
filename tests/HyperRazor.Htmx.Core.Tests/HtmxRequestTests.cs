using HyperRazor.Htmx;

namespace HyperRazor.Htmx.Core.Tests;

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
        Assert.Equal(HtmxVersion.Htmx2, request.Version);
        Assert.Equal(HtmxRequestType.Full, request.RequestType);
        Assert.Equal("#search-results", request.Target);
        Assert.Equal("keyup", request.Trigger);
        Assert.Equal("keyup", request.SourceElement);
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
        Assert.Equal(HtmxRequestType.Unknown, request.RequestType);
        Assert.Null(request.Target);
        Assert.Null(request.Trigger);
        Assert.Null(request.TriggerName);
        Assert.Null(request.CurrentUrl);
    }

    [Fact]
    public void FromHeaders_ParsesHtmx4Headers_WhenRequestTypeAndSourceArePresent()
    {
        var headers = new Dictionary<string, string?>
        {
            [HtmxHeaderNames.RequestType] = "partial",
            [HtmxHeaderNames.Source] = "save-user-button",
            [HtmxHeaderNames.Target] = "#users-list",
            [HtmxHeaderNames.CurrentUrl] = "https://example.test/demos/users"
        };

        var request = HtmxRequest.FromHeaders(headers, HtmxClientProfile.Htmx4Compat);

        Assert.True(request.IsHtmx);
        Assert.Equal(HtmxVersion.Htmx4, request.Version);
        Assert.Equal(HtmxRequestType.Partial, request.RequestType);
        Assert.Equal("save-user-button", request.Source);
        Assert.Equal("save-user-button", request.SourceElement);
        Assert.Equal("#users-list", request.Target);
    }

    [Fact]
    public void FromHeaders_HistoryRestoreRequest_ResolvesAsFullRequest()
    {
        var headers = new Dictionary<string, string?>
        {
            [HtmxHeaderNames.Request] = "true",
            [HtmxHeaderNames.HistoryRestoreRequest] = "true"
        };

        var request = HtmxRequest.FromHeaders(headers);

        Assert.True(request.IsHtmx);
        Assert.True(request.IsHistoryRestoreRequest);
        Assert.Equal(HtmxRequestType.Full, request.RequestType);
        Assert.True(request.IsFullRequest);
        Assert.False(request.IsPartialRequest);
    }
}
