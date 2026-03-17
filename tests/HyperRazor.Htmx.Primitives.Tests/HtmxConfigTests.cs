using System.Text.Json;
using HyperRazor.Htmx;

namespace HyperRazor.Htmx.Primitives.Tests;

public class HtmxConfigTests
{
    [Fact]
    public void ToJson_UsesExpectedDefaults()
    {
        var json = new HtmxConfig().ToJson();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("selfRequestsOnly").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("historyRestoreAsHxRequest").GetBoolean());
        Assert.False(doc.RootElement.TryGetProperty("allowNestedOobSwaps", out _));
        Assert.False(doc.RootElement.TryGetProperty("defaultSwapStyle", out _));
        Assert.False(doc.RootElement.TryGetProperty("responseHandling", out _));
    }

    [Fact]
    public void ToJson_IncludesConfiguredValues()
    {
        var json = new HtmxConfig
        {
            SelfRequestsOnly = false,
            HistoryRestoreAsHxRequest = true,
            AllowNestedOobSwaps = false,
            DefaultSwapStyle = "outerHTML",
            ResponseHandling =
            [
                new HtmxResponseHandlingRule
                {
                    Code = "422",
                    Swap = true,
                    Error = false
                }
            ]
        }.ToJson();

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("selfRequestsOnly").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("historyRestoreAsHxRequest").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("allowNestedOobSwaps").GetBoolean());
        Assert.Equal("outerHTML", doc.RootElement.GetProperty("defaultSwapStyle").GetString());
        var responseHandling = doc.RootElement.GetProperty("responseHandling");
        Assert.Equal("422", responseHandling[0].GetProperty("code").GetString());
        Assert.True(responseHandling[0].GetProperty("swap").GetBoolean());
        Assert.False(responseHandling[0].GetProperty("error").GetBoolean());
    }
}
