using HyperRazor.Components;
using HyperRazor.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Tests;

public class HrzSwapContentTests
{
    [Fact]
    public async Task HrzSwapContent_RendersFragmentFromInjectedBuffer()
    {
        var buffer = new TestSwapBuffer();
        buffer.SetContent("Buffered swap");

        using var provider = BuildProvider(buffer);
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        await using var renderer = new HtmlRenderer(provider, loggerFactory);
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<HrzSwapContent>();
            return rendered.ToHtmlString();
        });

        Assert.Contains("Buffered swap", html, StringComparison.Ordinal);
        Assert.Equal(1, buffer.RenderCallCount);
        Assert.Equal([true], buffer.ClearArguments);
        Assert.False(buffer.ContentAvailable);
    }

    [Fact]
    public async Task HrzSwapContent_RerendersWhenBufferRaisesContentItemsUpdated()
    {
        var buffer = new TestSwapBuffer();

        using var provider = BuildProvider(buffer);
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        await using var renderer = new HtmlRenderer(provider, loggerFactory);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<HrzSwapContent>();

            Assert.Equal(string.Empty, rendered.ToHtmlString());

            buffer.SetContent("Updated swap");
            buffer.RaiseContentItemsUpdated();

            await Task.Yield();

            Assert.Contains("Updated swap", rendered.ToHtmlString(), StringComparison.Ordinal);
            Assert.Equal(1, buffer.RenderCallCount);
        });
    }

    private static ServiceProvider BuildProvider(TestSwapBuffer buffer)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddRazorComponents();
        services.AddSingleton<IHrzSwapBuffer>(buffer);

        return services.BuildServiceProvider();
    }

    private sealed class TestSwapBuffer : IHrzSwapBuffer
    {
        private RenderFragment _fragment = _ => { };

        public event EventHandler? ContentItemsUpdated;

        public bool ContentAvailable { get; private set; }

        public int RenderCallCount { get; private set; }

        public List<bool> ClearArguments { get; } = [];

        public RenderFragment RenderToFragment(bool clear = false)
        {
            RenderCallCount++;
            ClearArguments.Add(clear);

            var fragment = _fragment;
            if (clear)
            {
                ContentAvailable = false;
                _fragment = _ => { };
            }

            return fragment;
        }

        public void RaiseContentItemsUpdated()
        {
            ContentItemsUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void SetContent(string content)
        {
            ContentAvailable = true;
            _fragment = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, content);
                builder.CloseElement();
            };
        }
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
