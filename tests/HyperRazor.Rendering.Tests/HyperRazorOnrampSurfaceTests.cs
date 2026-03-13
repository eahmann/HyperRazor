using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HyperRazor.Rendering.Tests;

public class HyperRazorOnrampSurfaceTests
{
    [Fact]
    public async Task MapPage_RendersFullPageShell_WithZeroArgumentRegistration()
    {
        await using var app = await BuildAppAsync(endpoints => endpoints.MapPage<TestPage>("/page"));

        var client = app.GetTestClient();
        var response = await client.GetAsync("/page");
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello from the onramp page.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapPartial_RendersFragmentWithoutShell_WithZeroArgumentRegistration()
    {
        await using var app = await BuildAppAsync(endpoints => endpoints.MapPartial<TestPartial>("/partial"));

        var client = app.GetTestClient();
        var response = await client.GetAsync("/partial");
        var html = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello from the onramp fragment.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UseHyperRazor_WithoutAddHtmx_ThrowsClearMessage()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHyperRazor();

        await using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.UseHyperRazor());

        Assert.Contains("UseHyperRazor()", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddHtmx()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UseHyperRazor_WithManualHtmxConfigButWithoutAddHtmx_StillThrowsClearMessage()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHyperRazor();
        builder.Services.AddSingleton(new HtmxConfig
        {
            SelfRequestsOnly = false
        });

        await using var app = builder.Build();
        var exception = Assert.Throws<InvalidOperationException>(() => app.UseHyperRazor());

        Assert.Contains("UseHyperRazor()", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddHtmx()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzResultsPage_WithoutAddHtmx_ThrowsClearMessage()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddHyperRazor()
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => HrzResults.Page<TestPage>(context));

        Assert.Contains("Rendering a HyperRazor page or partial", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddHtmx()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrControllerPage_WithoutAddHtmx_ThrowsClearMessage()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddHyperRazor()
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        var controller = new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = context
            }
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => controller.RenderPage());

        Assert.Contains("Rendering a HyperRazor page or partial", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AddHtmx()", exception.Message, StringComparison.Ordinal);
    }

    private static async Task<WebApplication> BuildAppAsync(Action<IEndpointRouteBuilder> mapEndpoints)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HyperRazorOnrampSurfaceTests).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddHyperRazor();
        builder.Services.AddHtmx();

        var app = builder.Build();
        app.UseHyperRazor();
        mapEndpoints(app);

        await app.StartAsync();
        return app;
    }

    private sealed class TestController : HrController
    {
        public Task<IResult> RenderPage()
        {
            return Page<TestPage>();
        }
    }

    private sealed class TestPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "main");
            builder.AddContent(1, "Hello from the onramp page.");
            builder.CloseElement();
        }
    }

    private sealed class TestPartial : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Hello from the onramp fragment.");
            builder.CloseElement();
        }
    }
}
