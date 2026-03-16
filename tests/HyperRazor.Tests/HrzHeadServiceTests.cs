using HyperRazor.Components;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Tests;

#pragma warning disable BL0006

public class HrzHeadServiceTests
{
    [Fact]
    public void RenderToFragment_WithoutHtmxRequest_DoesNotEmitHeadPayload()
    {
        var service = CreateService(isHtmx: false);
        service.AddTitle("Users");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.Equal(0, frames.Count);
        Assert.True(service.ContentAvailable);
    }

    [Fact]
    public void RenderToFragment_WithHtmxRequest_EmitsHeadPayload()
    {
        var service = CreateService(isHtmx: true);
        service.AddTitle("Users");
        service.AddMeta("description", "Users screen", key: "description");

        var fragment = service.RenderToFragment();
        var frames = RenderFrames(fragment);

        Assert.True(frames.Count > 0);
        Assert.Contains(
            frames.Array.Take(frames.Count),
            frame => frame.FrameType == RenderTreeFrameType.Element && frame.ElementName == "head");
    }

    [Fact]
    public void RenderToFragment_ClearTrue_DrainsQueuedHeadItems()
    {
        var service = CreateService(isHtmx: true);
        service.AddTitle("Users");

        _ = service.RenderToFragment(clear: true);

        Assert.False(service.ContentAvailable);
    }

    [Fact]
    public void ContentItemsUpdated_RaisesOnAddAndClear()
    {
        var service = CreateService(isHtmx: true);
        var updates = 0;
        service.ContentItemsUpdated += (_, _) => updates++;

        service.AddTitle("Users");
        service.AddMeta("description", "Users screen");
        service.Clear();

        Assert.Equal(3, updates);
    }

    [Fact]
    public async Task RenderToFragment_WithKeyedItems_DedupesAndPreservesStableOrdering()
    {
        var service = CreateService(isHtmx: true);
        service.AddScript(
            "/head-demo.asset.js",
            new Dictionary<string, object?>
            {
                ["defer"] = true,
                ["data-test"] = "head-demo"
            },
            key: "head-demo-script");
        service.AddStyle(".preview { color: teal; }", key: "head-demo-style");
        service.AddLink("/demo-a.css", key: "demo-stylesheet");
        service.AddMeta("description", "Initial description", key: "description");
        service.SetTitle("Initial title");
        service.AddMeta("description", "Final description", key: "description");
        service.AddLink("/demo-b.css", key: "demo-stylesheet");
        service.SetTitle("Final title");

        var html = await RenderHtmlAsync(service.RenderToFragment());

        Assert.Contains("<head", html, StringComparison.Ordinal);
        Assert.Contains("<title>Final title</title>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Initial title", html, StringComparison.Ordinal);
        Assert.Contains("name=\"description\"", html, StringComparison.Ordinal);
        Assert.Contains("content=\"Final description\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Initial description", html, StringComparison.Ordinal);
        Assert.Contains("rel=\"stylesheet\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/demo-b.css\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/demo-a.css", html, StringComparison.Ordinal);
        Assert.Contains("<style>.preview { color: teal; }</style>", html, StringComparison.Ordinal);
        Assert.Contains("src=\"/head-demo.asset.js\"", html, StringComparison.Ordinal);
        Assert.Contains("data-test=\"head-demo\"", html, StringComparison.Ordinal);
        Assert.Contains("defer", html, StringComparison.Ordinal);

        Assert.True(IndexOf(html, "<title") < IndexOf(html, "name=\"description\""));
        Assert.True(IndexOf(html, "name=\"description\"") < IndexOf(html, "href=\"/demo-b.css\""));
        Assert.True(IndexOf(html, "href=\"/demo-b.css\"") < IndexOf(html, "<style>"));
        Assert.True(IndexOf(html, "<style>") < IndexOf(html, "src=\"/head-demo.asset.js\""));
    }

    [Fact]
    public async Task RenderToFragment_AppendsFragmentsAfterTypedHeadItems()
    {
        var service = CreateService(isHtmx: true);
        service.SetTitle("Users");
        service.AddHeadFragment(builder =>
        {
            builder.OpenElement(0, "meta");
            builder.AddAttribute(1, "name", "robots");
            builder.AddAttribute(2, "content", "noindex");
            builder.CloseElement();
        });
        service.AddRawContent("<!-- tail -->");

        var html = await RenderHtmlAsync(service.RenderToFragment());

        Assert.True(IndexOf(html, "<title>Users</title>") < IndexOf(html, "name=\"robots\""));
        Assert.True(IndexOf(html, "name=\"robots\"") < IndexOf(html, "<!-- tail -->"));
    }

    private static HrzHeadService CreateService(bool isHtmx)
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

        return new HrzHeadService(accessor);
    }

    private static ArrayRange<RenderTreeFrame> RenderFrames(RenderFragment fragment)
    {
        var builder = new RenderTreeBuilder();
        fragment(builder);
        return builder.GetFrames();
    }

    private static async Task<string> RenderHtmlAsync(RenderFragment fragment)
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

        await using var renderer = new HtmlRenderer(provider, loggerFactory);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<HrzFragmentGroup>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(HrzFragmentGroup.Fragments)] = new RenderFragment[] { fragment }
                }));

            return rendered.ToHtmlString();
        });
    }

    private static int IndexOf(string html, string value)
    {
        return html.IndexOf(value, StringComparison.Ordinal);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "HyperRazor.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

#pragma warning restore BL0006
