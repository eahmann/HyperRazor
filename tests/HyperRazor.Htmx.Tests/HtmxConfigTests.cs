using System.Text.Json;
using HyperRazor.Htmx;

namespace HyperRazor.Htmx.Tests;

public class HtmxConfigTests
{
    [Fact]
    public void ToJson_UsesExpectedDefaults()
    {
        var json = new HtmxConfig().ToJson();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("selfRequestsOnly").GetBoolean());
        Assert.False(doc.RootElement.GetProperty("historyRestoreAsHxRequest").GetBoolean());
        Assert.False(doc.RootElement.TryGetProperty("defaultSwapStyle", out _));
    }

    [Fact]
    public void ToJson_IncludesConfiguredValues()
    {
        var json = new HtmxConfig
        {
            SelfRequestsOnly = false,
            HistoryRestoreAsHxRequest = true,
            DefaultSwapStyle = "outerHTML"
        }.ToJson();

        using var doc = JsonDocument.Parse(json);
        Assert.False(doc.RootElement.GetProperty("selfRequestsOnly").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("historyRestoreAsHxRequest").GetBoolean());
        Assert.Equal("outerHTML", doc.RootElement.GetProperty("defaultSwapStyle").GetString());
    }
}
