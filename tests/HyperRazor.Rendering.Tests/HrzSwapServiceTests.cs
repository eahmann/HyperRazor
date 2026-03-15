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
using Microsoft.Extensions.Logging;

namespace HyperRazor.Rendering.Tests;

#pragma warning disable BL0006

public class HrzSwapServiceTests
{
    [Fact]
    public void RenderToFragment_WithoutHtmxRequest_ExcludesQueuedSwaps()
    {
        var service = CreateService(isHtmx: false);
        var buffer = (IHrzSwapBuffer)service;
        service.Replace("toast-stack", builder => builder.AddMarkupContent(0, "<div>Created</div>"));

        var fragment = buffer.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
        Assert.True(buffer.ContentAvailable);
    }

    [Fact]
    public void RenderToFragment_WithHtmxRequest_IncludesQueuedSwaps()
    {
        var service = CreateService(isHtmx: true);
        var buffer = (IHrzSwapBuffer)service;
        service.Replace("toast-stack", builder => builder.AddMarkupContent(0, "<div>Created</div>"));

        var fragment = buffer.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
    }

    [Fact]
    public void RenderToFragment_ClearTrue_DrainsQueuedItems()
    {
        var service = CreateService(isHtmx: true);
        var buffer = (IHrzSwapBuffer)service;
        service.Replace<TestBadgeComponent>("badge-shell", new { Message = "Hello" });

        _ = buffer.RenderToFragment(clear: true);

        Assert.False(buffer.ContentAvailable);
    }

    [Fact]
    public void ContentItemsUpdated_RaisesOnAddAndClear()
    {
        var service = CreateService(isHtmx: true);
        var buffer = (IHrzSwapBuffer)service;
        var updates = 0;
        buffer.ContentItemsUpdated += (_, _) => updates++;

        service.Replace("badge-shell", builder => builder.AddContent(0, "Created"));
        service.Append("toast-stack", "toast-1", builder => builder.AddContent(0, "Toast"));
        service.Clear();

        Assert.Equal(3, updates);
    }

    [Fact]
    public async Task RenderToString_PreservesOrdering_AndEncodesRegionAndSelectorTargets()
    {
        using var provider = BuildRendererProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        var service = CreateService(
            isHtmx: true,
            serviceProvider: provider,
            loggerFactory: loggerFactory);

        service.Append(
            "toast-stack",
            "toast-first",
            builder => builder.AddMarkupContent(0, "<div class=\"toast\">First</div>"));
        service.Replace(
            "status-shell",
            builder => builder.AddMarkupContent(0, "<article>Second</article>"));
        service.Replace<TestBadgeComponent>(
            "#legacy-shell",
            new { Message = "Third" },
            new HrzSwapOptions
            {
                TargetKind = HrzSwapTargetKind.Selector,
                TargetId = "legacy-shell"
            });

        var html = await service.RenderToString();

        Assert.Contains("hx-swap-oob=\"beforeend:#toast-stack\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML:#legacy-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"toast-first\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"status-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"legacy-shell\"", html, StringComparison.Ordinal);

        Assert.True(IndexOf(html, "id=\"toast-first\"") < IndexOf(html, "id=\"status-shell\""));
        Assert.True(IndexOf(html, "id=\"status-shell\"") < IndexOf(html, "id=\"legacy-shell\""));
    }

    [Fact]
    public async Task RenderToString_WithHtmxRequest_IncludesSwappableMarkup()
    {
        using var provider = BuildRendererProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        var service = CreateService(
            isHtmx: true,
            serviceProvider: provider,
            loggerFactory: loggerFactory);
        service.Replace("toast-shell", builder => builder.AddContent(0, "Created"));

        var html = await service.RenderToString();

        Assert.Contains("hx-swap-oob=", html, StringComparison.Ordinal);
        Assert.Contains("id=\"toast-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Created", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderToString_WithoutRendererDependencies_Throws()
    {
        var service = CreateService(isHtmx: true);
        service.Replace("toast-shell", builder => builder.AddContent(0, "Created"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RenderToString());
    }

    [Fact]
    public void Replace_WithSelectorTarget_RequiresTargetId()
    {
        var service = CreateService(isHtmx: true);

        Assert.Throws<ArgumentException>(() =>
            service.Replace<TestBadgeComponent>(
                "#legacy-shell",
                new { Message = "Oops" },
                new HrzSwapOptions
                {
                    TargetKind = HrzSwapTargetKind.Selector
                }));
    }

    private static HrzSwapService CreateService(
        bool isHtmx,
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

        if (serviceProvider is not null && loggerFactory is not null)
        {
            return new HrzSwapService(accessor, serviceProvider, loggerFactory);
        }

        return new HrzSwapService(accessor);
    }

    private static ServiceProvider BuildRendererProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddRazorComponents();
        return services.BuildServiceProvider();
    }

    private static ArrayRange<RenderTreeFrame> RenderFrames(RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder.GetFrames();
    }

    private static int IndexOf(string html, string value)
    {
        return html.IndexOf(value, StringComparison.Ordinal);
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
