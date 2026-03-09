using HyperRazor.Htmx;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Htmx.Tests;

public class HyperRazorHtmxHttpContextExtensionsTests
{
    [Fact]
    public void HtmxRequest_ParsesRequestHeaders_FromHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HtmxHeaderNames.Request] = "true";
        context.Request.Headers[HtmxHeaderNames.Target] = "#main";

        var request = context.HtmxRequest();

        Assert.True(request.IsHtmx);
        Assert.Equal("#main", request.Target);
        Assert.Equal(HtmxRequestType.Partial, request.RequestType);
    }

    [Fact]
    public void HtmxResponse_WritesHeaders_ToHttpContextResponse()
    {
        var context = new DefaultHttpContext();

        context.HtmxResponse()
            .Location("/users")
            .Trigger("toast:show", new { message = "Saved" });

        Assert.Equal("/users", context.Response.Headers[HtmxHeaderNames.Location].ToString());
        Assert.Contains("toast:show", context.Response.Headers[HtmxHeaderNames.TriggerResponse].ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Htmx_ReturnsRequestAndResponseHelpers()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HtmxHeaderNames.Request] = "true";

        var (request, response) = context.Htmx();
        response.Redirect("/");

        Assert.True(request.IsHtmx);
        Assert.Equal("/", context.Response.Headers[HtmxHeaderNames.Redirect].ToString());
    }
}
