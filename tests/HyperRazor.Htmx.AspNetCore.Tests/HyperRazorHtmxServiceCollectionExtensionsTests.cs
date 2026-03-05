using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Htmx.AspNetCore.Tests;

public class HyperRazorHtmxServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHtmx_RegistersConfiguredSingleton()
    {
        var services = new ServiceCollection();

        services.AddHtmx(config =>
        {
            config.SelfRequestsOnly = false;
        });

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<HtmxConfig>();

        Assert.False(config.SelfRequestsOnly);
    }

    [Fact]
    public void AddHyperRazorHtmx_RegistersConfiguredSingleton()
    {
        var services = new ServiceCollection();

        services.AddHyperRazorHtmx(config =>
        {
            config.SelfRequestsOnly = false;
            config.HistoryRestoreAsHxRequest = true;
            config.AllowNestedOobSwaps = false;
            config.DefaultSwapStyle = "innerHTML";
        });

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<HtmxConfig>();

        Assert.False(config.SelfRequestsOnly);
        Assert.True(config.HistoryRestoreAsHxRequest);
        Assert.False(config.AllowNestedOobSwaps);
        Assert.Equal("innerHTML", config.DefaultSwapStyle);
    }
}
