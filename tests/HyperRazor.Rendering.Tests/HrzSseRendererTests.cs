using System.IO;
using System.Net.ServerSentEvents;
using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HyperRazor.Rendering.Tests;

public sealed class HrzSseRendererTests
{
    [Fact]
    public async Task RenderComponent_WithoutHtmxHeaders_RendersHtmlIntoSseItem()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var item = await fixture.SseRenderer.RenderComponent<GreetingComponent>(
            new { Name = "Ava" },
            eventType: "update",
            id: "evt-1",
            retryAfter: TimeSpan.FromSeconds(3));

        Assert.Equal("update", item.EventType);
        Assert.Equal("evt-1", item.EventId);
        Assert.Equal(TimeSpan.FromSeconds(3), item.ReconnectionInterval);
        Assert.Contains("Hello Ava", item.Data, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", item.Data, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderComponent_IncludesOobContentAndSuppressesHeadContent()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var item = await fixture.SseRenderer.RenderComponent<QueuedContentComponent>();

        Assert.Contains("Queued body", item.Data, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=", item.Data, StringComparison.Ordinal);
        Assert.Contains("id=\"queued-status\"", item.Data, StringComparison.Ordinal);
        Assert.DoesNotContain("Queued title", item.Data, StringComparison.Ordinal);
        Assert.False(fixture.Scope.ServiceProvider.GetRequiredService<IHrzHeadService>().ContentAvailable);
        Assert.False(fixture.Scope.ServiceProvider.GetRequiredService<IHrzSwapService>().ContentAvailable);
    }

    [Fact]
    public async Task RenderComponent_WhenRenderFails_ClearsQueuedHeadAndSwapState()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.SseRenderer.RenderComponent<ThrowingQueuedContentComponent>());

        Assert.False(fixture.Scope.ServiceProvider.GetRequiredService<IHrzHeadService>().ContentAvailable);
        Assert.False(fixture.Scope.ServiceProvider.GetRequiredService<IHrzSwapService>().ContentAvailable);
    }

    [Fact]
    public async Task PlatformSseResult_WritesBlankDataDoneEvent()
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var result = TypedResults.ServerSentEvents(GetDoneEvents());

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();

        Assert.Contains("event: done\ndata: \n\n", body, StringComparison.Ordinal);
    }

    private static async Task<TestFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        var httpContextAccessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(httpContextAccessor);
        services.AddRazorComponents();
        services.AddHtmx(config =>
        {
            config.SelfRequestsOnly = true;
            config.HistoryRestoreAsHxRequest = false;
        });
        services.Configure<HrzOptions>(options =>
        {
            options.RootComponent = typeof(HrzApp<HrzAppLayout>);
            options.UseMinimalLayoutForHtmx = true;
        });
        services.AddOptions<HrzSwapOptions>();
        services.AddSingleton<IHrzLayoutFamilyResolver, HrzLayoutFamilyResolver>();
        services.AddSingleton<IHrzFieldPathResolver>(new HrzFieldPathResolver());
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<IHrzSwapService, HrzSwapService>();
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzSseRenderer, HrzSseRenderer>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        httpContextAccessor.HttpContext = httpContext;

        var sseRenderer = scope.ServiceProvider.GetRequiredService<IHrzSseRenderer>();

        await Task.Yield();

        return new TestFixture(provider, scope, httpContextAccessor, httpContext, sseRenderer);
    }

    private static async IAsyncEnumerable<SseItem<string>> GetDoneEvents()
    {
        yield return new SseItem<string>(string.Empty, eventType: "done");
        await Task.CompletedTask;
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        public TestFixture(
            ServiceProvider provider,
            IServiceScope scope,
            IHttpContextAccessor httpContextAccessor,
            HttpContext httpContext,
            IHrzSseRenderer sseRenderer)
        {
            Provider = provider;
            Scope = scope;
            HttpContextAccessor = httpContextAccessor;
            HttpContext = httpContext;
            SseRenderer = sseRenderer;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public IHttpContextAccessor HttpContextAccessor { get; }

        public HttpContext HttpContext { get; }

        public IHrzSseRenderer SseRenderer { get; }

        public void SetCurrentContext()
        {
            HttpContextAccessor.HttpContext = HttpContext;
        }

        public async ValueTask DisposeAsync()
        {
            if (Scope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                Scope.Dispose();
            }

            await Provider.DisposeAsync();
        }
    }

    private sealed class GreetingComponent : ComponentBase
    {
        [Parameter]
        public string Name { get; set; } = string.Empty;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, $"Hello {Name}");
            builder.CloseElement();
        }
    }

    private sealed class QueuedContentComponent : ComponentBase
    {
        [Inject]
        public IHrzHeadService HeadService { get; set; } = default!;

        [Inject]
        public IHrzSwapService SwapService { get; set; } = default!;

        protected override void OnInitialized()
        {
            HeadService.SetTitle("Queued title");
            SwapService.QueueHtml(
                targetId: "queued-status",
                html: "<div id=\"queued-status\">Queued status</div>");
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "section");
            builder.AddContent(1, "Queued body");
            builder.CloseElement();
        }
    }

    private sealed class ThrowingQueuedContentComponent : ComponentBase
    {
        [Inject]
        public IHrzHeadService HeadService { get; set; } = default!;

        [Inject]
        public IHrzSwapService SwapService { get; set; } = default!;

        protected override void OnInitialized()
        {
            HeadService.SetTitle("Exploding title");
            SwapService.QueueHtml(
                targetId: "queued-status",
                html: "<div id=\"queued-status\">Queued status</div>");
            throw new InvalidOperationException("Boom");
        }
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
