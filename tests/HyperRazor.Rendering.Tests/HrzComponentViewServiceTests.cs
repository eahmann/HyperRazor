using System.IO;
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

public class HrzComponentViewServiceTests
{
    [Fact]
    public async Task View_WithoutHtmxRequest_RendersFullShell()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, fixture.HttpContext.Response.Headers.Vary.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, fixture.HttpContext.Response.Headers.Vary.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, fixture.HttpContext.Response.Headers.Vary.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task View_WithHtmxRequest_RendersMinimalShellWithoutAppShell()
    {
        await using var fixture = await CreateFixtureAsync(headers =>
        {
            headers[HtmxHeaderNames.Request] = "true";
        });
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_WithHistoryRestoreRequest_RendersFullShell()
    {
        await using var fixture = await CreateFixtureAsync(headers =>
        {
            headers[HtmxHeaderNames.Request] = "true";
            headers[HtmxHeaderNames.HistoryRestoreRequest] = "true";
        });
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_WithHtmx4FullRequestType_RendersFullShell()
    {
        await using var fixture = await CreateFixtureAsync(headers =>
        {
            headers[HtmxHeaderNames.RequestType] = "full";
        });
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_NeverRendersLayoutShell()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.PartialView<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_BindsAnonymousObjectAndDictionaryParameters()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var anonymousResult = await fixture.ViewService.View<GreetingComponent>(new { Name = "Anonymous" });
        var anonymousHtml = await ExecuteResultAsync(anonymousResult, fixture.HttpContext);

        var dictionaryResult = await fixture.ViewService.View<GreetingComponent>(
            new Dictionary<string, object?> { ["Name"] = "Dictionary" });
        var dictionaryHtml = await ExecuteResultAsync(dictionaryResult, fixture.HttpContext);

        Assert.Contains("Hello Anonymous", anonymousHtml, StringComparison.Ordinal);
        Assert.Contains("Hello Dictionary", dictionaryHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_AwaitsAsyncComponentQuiescence()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<AsyncGreetingComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Loaded", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Loading", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_WithCrossFamilyBoostedRequest_StoresLayoutPromotionDiagnostics()
    {
        await using var fixture = await CreateFixtureAsync(
            headers =>
            {
                headers[HtmxHeaderNames.Request] = "true";
                headers[HtmxHeaderNames.Boosted] = "true";
                headers[HtmxHeaderNames.LayoutFamily] = "main";
            },
            options =>
            {
                options.LayoutBoundary.Enabled = true;
                options.LayoutBoundary.PromotionMode = HrzLayoutBoundaryPromotionMode.ShellSwap;
            });
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<SideGreetingComponent>(new { Name = "Ava" });
        _ = await ExecuteResultAsync(result, fixture.HttpContext);

        var diagnostics = Assert.IsType<HtmxLayoutPromotionDiagnostics>(
            fixture.HttpContext.Items[typeof(HtmxLayoutPromotionDiagnostics)]);
        Assert.Equal("main", diagnostics.ClientLayoutFamily);
        Assert.Equal("side", diagnostics.RouteLayoutFamily);
        Assert.Equal(nameof(HrzLayoutBoundaryPromotionMode.ShellSwap), diagnostics.PromotionMode);
        Assert.True(diagnostics.PromotionApplied);
    }

    [Fact]
    public async Task View_WithSameFamilyBoostedRequest_StoresNonAppliedLayoutPromotionDiagnostics()
    {
        await using var fixture = await CreateFixtureAsync(
            headers =>
            {
                headers[HtmxHeaderNames.Request] = "true";
                headers[HtmxHeaderNames.Boosted] = "true";
                headers[HtmxHeaderNames.LayoutFamily] = "main";
            },
            options =>
            {
                options.LayoutBoundary.Enabled = true;
                options.LayoutBoundary.PromotionMode = HrzLayoutBoundaryPromotionMode.ShellSwap;
            });
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.View<GreetingComponent>(new { Name = "Ava" });
        _ = await ExecuteResultAsync(result, fixture.HttpContext);

        var diagnostics = Assert.IsType<HtmxLayoutPromotionDiagnostics>(
            fixture.HttpContext.Items[typeof(HtmxLayoutPromotionDiagnostics)]);
        Assert.Equal("main", diagnostics.ClientLayoutFamily);
        Assert.Equal("main", diagnostics.RouteLayoutFamily);
        Assert.Equal(nameof(HrzLayoutBoundaryPromotionMode.Off), diagnostics.PromotionMode);
        Assert.False(diagnostics.PromotionApplied);
    }

    private static async Task<TestFixture> CreateFixtureAsync(
        Action<IHeaderDictionary>? configureHeaders = null,
        Action<HrzOptions>? configureOptions = null)
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
            configureOptions?.Invoke(options);
        });
        services.AddOptions<HrzSwapOptions>();
        services.AddSingleton<IHrzLayoutFamilyResolver, HrzLayoutFamilyResolver>();
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<IHrzSwapService, HrzSwapService>();
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzComponentViewService, HrzComponentViewService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        configureHeaders?.Invoke(httpContext.Request.Headers);

        httpContextAccessor.HttpContext = httpContext;

        var viewService = scope.ServiceProvider.GetRequiredService<IHrzComponentViewService>();

        await Task.Yield();

        return new TestFixture(provider, scope, httpContextAccessor, httpContext, viewService);
    }

    private static async Task<string> ExecuteResultAsync(IResult result, HttpContext context)
    {
        context.Response.Body = new MemoryStream();

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        public TestFixture(
            ServiceProvider provider,
            IServiceScope scope,
            IHttpContextAccessor httpContextAccessor,
            HttpContext httpContext,
            IHrzComponentViewService viewService)
        {
            Provider = provider;
            Scope = scope;
            HttpContextAccessor = httpContextAccessor;
            HttpContext = httpContext;
            ViewService = viewService;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public IHttpContextAccessor HttpContextAccessor { get; }

        public HttpContext HttpContext { get; }

        public IHrzComponentViewService ViewService { get; }

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

    private sealed class AsyncGreetingComponent : ComponentBase
    {
        private string _message = "Loading";

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(25);
            _message = "Loaded";
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddContent(1, _message);
            builder.CloseElement();
        }
    }

    [Layout(typeof(SideTestLayout))]
    private sealed class SideGreetingComponent : ComponentBase
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

    [HrzLayoutFamily("side")]
    private sealed class SideTestLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
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
