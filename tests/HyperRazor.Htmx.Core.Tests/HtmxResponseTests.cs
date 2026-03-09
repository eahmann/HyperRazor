using System.Text.Json;
using HyperRazor.Htmx;

namespace HyperRazor.Htmx.Core.Tests;

public class HtmxResponseTests
{
    [Fact]
    public void HeaderSetters_WriteExpectedValues()
    {
        var response = new HtmxResponse()
            .Redirect("/login")
            .Location("/users")
            .PushUrl(false)
            .ReplaceUrl("/users?page=2")
            .Refresh()
            .Retarget("#main")
            .Reswap("outerHTML")
            .Reselect("#panel");

        Assert.Equal("/login", response.Headers[HtmxHeaderNames.Redirect]);
        Assert.Equal("/users", response.Headers[HtmxHeaderNames.Location]);
        Assert.Equal("false", response.Headers[HtmxHeaderNames.PushUrl]);
        Assert.Equal("/users?page=2", response.Headers[HtmxHeaderNames.ReplaceUrl]);
        Assert.Equal("true", response.Headers[HtmxHeaderNames.Refresh]);
        Assert.Equal("#main", response.Headers[HtmxHeaderNames.Retarget]);
        Assert.Equal("outerHTML", response.Headers[HtmxHeaderNames.Reswap]);
        Assert.Equal("#panel", response.Headers[HtmxHeaderNames.Reselect]);
    }

    [Fact]
    public void Location_ObjectPayload_IsSerializedAsJson()
    {
        var response = new HtmxResponse()
            .Location(new { path = "/users", target = "#main", swap = "innerHTML" });

        using var doc = JsonDocument.Parse(response.Headers[HtmxHeaderNames.Location]);
        Assert.Equal("/users", doc.RootElement.GetProperty("path").GetString());
        Assert.Equal("#main", doc.RootElement.GetProperty("target").GetString());
        Assert.Equal("innerHTML", doc.RootElement.GetProperty("swap").GetString());
    }

    [Fact]
    public void TriggerHelpers_SupportStringSinglePayloadAndMultiPayload()
    {
        var response = new HtmxResponse()
            .Trigger("refresh:list")
            .TriggerAfterSwap("toast:show", new { message = "Saved" })
            .TriggerAfterSettle(new Dictionary<string, object?>
            {
                ["refresh:stats"] = true,
                ["log:event"] = new { level = "info" }
            });

        Assert.Equal("refresh:list", response.Headers[HtmxHeaderNames.TriggerResponse]);

        using (var swapDoc = JsonDocument.Parse(response.Headers[HtmxHeaderNames.TriggerAfterSwap]))
        {
            Assert.Equal("Saved", swapDoc.RootElement.GetProperty("toast:show").GetProperty("message").GetString());
        }

        using var settleDoc = JsonDocument.Parse(response.Headers[HtmxHeaderNames.TriggerAfterSettle]);
        Assert.True(settleDoc.RootElement.GetProperty("refresh:stats").GetBoolean());
        Assert.Equal("info", settleDoc.RootElement.GetProperty("log:event").GetProperty("level").GetString());
    }

    [Fact]
    public void ApplyTo_OverwritesExistingHeaders()
    {
        var destination = new Dictionary<string, string>
        {
            [HtmxHeaderNames.PushUrl] = "old"
        };

        var response = new HtmxResponse().PushUrl("/new");
        response.ApplyTo(destination);

        Assert.Equal("/new", destination[HtmxHeaderNames.PushUrl]);
    }

    [Fact]
    public void Trigger_Throws_ForInvalidInput()
    {
        var response = new HtmxResponse();

        Assert.Throws<ArgumentException>(() => response.Trigger(" "));
        Assert.Throws<ArgumentException>(() => response.Trigger(new Dictionary<string, object?>()));
    }
}
