using HyperRazor.Components;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Rendering.Tests;

#pragma warning disable BL0006

public class HrxSwapServiceTests
{
    [Fact]
    public void SwapStyle_ToHtmxString_UsesExpectedFormats()
    {
        Assert.Equal("outerHTML", SwapStyle.OuterHtml.ToHtmxString());
        Assert.Equal("beforeend:#toast-stack", BuildOobValue(SwapStyle.BeforeEnd, "#toast-stack"));
    }

    [Fact]
    public void RenderToFragment_WithoutHtmxRequest_ExcludesSwappables()
    {
        var service = CreateService(isHtmx: false);
        service.AddSwappableContent("toast-item", "Created");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
        Assert.True(service.ContentAvailable);
    }

    [Fact]
    public void RenderToFragment_WithHtmxRequest_IncludesSwappables()
    {
        var service = CreateService(isHtmx: true);
        service.AddSwappableContent("toast-item", "Created");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
    }

    [Fact]
    public void AddRawContent_WithoutOptIn_DoesNotRenderForNonHtmx()
    {
        var service = CreateService(isHtmx: false, allowRawContentOnNonHtmx: false);
        service.AddRawContent("<p id=\"raw-content\">Raw</p>");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
    }

    [Fact]
    public void AddRawContent_WithOptIn_RendersForNonHtmx()
    {
        var service = CreateService(isHtmx: false, allowRawContentOnNonHtmx: true);
        service.AddRawContent("<p id=\"raw-content\">Raw</p>");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
    }

    [Fact]
    public void RenderToFragment_ClearTrue_DrainsQueuedItems()
    {
        var service = CreateService(isHtmx: true);
        service.AddSwappableComponent<TestBadgeComponent>("badge-item", new { Message = "Hello" });

        _ = service.RenderToFragment(clear: true);

        Assert.False(service.ContentAvailable);
    }

    [Fact]
    public void ContentItemsUpdated_RaisesOnAddAndClear()
    {
        var service = CreateService(isHtmx: true);
        var updates = 0;
        service.ContentItemsUpdated += (_, _) => updates++;

        service.AddSwappableContent("toast-item", "Created");
        service.AddRawContent("<p>Raw</p>");
        service.Clear();

        Assert.Equal(3, updates);
    }

    [Fact]
    public async Task RenderToString_WithHtmxRequest_IncludesSwappableMarkup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddRazorComponents();
        using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        var service = CreateService(
            isHtmx: true,
            serviceProvider: provider,
            loggerFactory: loggerFactory);
        service.AddSwappableContent("toast-item", "Created");

        var html = await service.RenderToString();

        Assert.Contains("hx-swap-oob=", html, StringComparison.Ordinal);
        Assert.Contains("id=\"toast-item\"", html, StringComparison.Ordinal);
        Assert.Contains("Created", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToString_WithoutRendererDependencies_Throws()
    {
        var service = CreateService(isHtmx: true);
        service.AddSwappableContent("toast-item", "Created");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenderToString());
    }

    private static HrxSwapService CreateService(
        bool isHtmx,
        bool allowRawContentOnNonHtmx = false,
        IServiceProvider? serviceProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        var context = new DefaultHttpContext();
        if (isHtmx)
        {
            context.Request.Headers[HtmxHeaderNames.Request] = "true";
        }

        var accessor = new HttpContextAccessor
        {
            HttpContext = context
        };

        var options = Options.Create(new HrxSwapOptions
        {
            AllowRawContentOnNonHtmx = allowRawContentOnNonHtmx
        });

        if (serviceProvider is not null && loggerFactory is not null)
        {
            return new HrxSwapService(accessor, options, serviceProvider, loggerFactory);
        }

        return new HrxSwapService(accessor, options);
    }

    private static ArrayRange<RenderTreeFrame> RenderFrames(RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder.GetFrames();
    }

    private static string BuildOobValue(SwapStyle style, string selector)
    {
        return $"{style.ToHtmxString()}:{selector}";
    }

    private sealed class TestBadgeComponent : ComponentBase
    {
        [Parameter]
        public string Message { get; set; } = string.Empty;
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HyperRazor.Rendering.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

#pragma warning restore BL0006
