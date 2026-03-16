using HyperRazor.Htmx;
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
        var marker = provider.GetRequiredService<IHtmxRegistrationMarker>();

        Assert.False(config.SelfRequestsOnly);
        Assert.NotNull(marker);
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

    [Fact]
    public void AddHtmx_ComposesMultipleConfigureCallbacks()
    {
        var services = new ServiceCollection();

        services.AddHtmx(config =>
        {
            config.SelfRequestsOnly = false;
            config.AllowNestedOobSwaps = false;
        });
        services.AddHtmx(config =>
        {
            config.HistoryRestoreAsHxRequest = true;
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
