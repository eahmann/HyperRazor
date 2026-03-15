using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using HyperRazor.Components;
using HyperRazor.Components.Layouts;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;

namespace HyperRazor.Rendering.Tests;

public class HrzRenderServiceTests
{
    [Fact]
    public async Task View_WithoutHtmxRequest_RendersFullShell()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Page<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.Contains($"data-hrz-current-layout=\"{HrzLayoutKey.None}\"", html, StringComparison.Ordinal);
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

        var result = await fixture.RenderService.Page<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.Contains($"data-hrz-current-layout=\"{HrzLayoutKey.None}\"", html, StringComparison.Ordinal);
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

        var result = await fixture.RenderService.Page<GreetingComponent>(new { Name = "Ava" });
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

        var result = await fixture.RenderService.Page<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_NeverRendersLayoutShell()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<GreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hrz-current-layout=", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_BindsAnonymousObjectAndDictionaryParameters()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var anonymousResult = await fixture.RenderService.Page<GreetingComponent>(new { Name = "Anonymous" });
        var anonymousHtml = await ExecuteResultAsync(anonymousResult, fixture.HttpContext);

        var dictionaryResult = await fixture.RenderService.Page<GreetingComponent>(
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

        var result = await fixture.RenderService.Page<AsyncGreetingComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Loaded", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Loading", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task View_WithBoostedSameLayoutRequest_RendersPageFragment_AndStoresNavigationDiagnostics()
    {
        await using var fixture = await CreateFixtureAsync(
            headers =>
            {
                headers[HtmxHeaderNames.Request] = "true";
                headers[HtmxHeaderNames.Boosted] = "true";
                headers[HtmxHeaderNames.CurrentUrl] = "https://localhost/side/list";
                headers[HrzInternalHeaderNames.CurrentLayout] = HrzLayoutKey.Create(typeof(SideTestLayout));
            });
        fixture.SetCurrentContext();
        fixture.HttpContext.Request.Path = "/side/detail";

        var result = await fixture.RenderService.Page<SideGreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-minimal-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.False(fixture.HttpContext.Response.Headers.ContainsKey(HtmxHeaderNames.Location));

        var diagnostics = Assert.IsType<HtmxPageNavigationDiagnostics>(
            fixture.HttpContext.Items[typeof(HtmxPageNavigationDiagnostics)]);
        Assert.Equal("https://localhost/side/list", diagnostics.CurrentUrl);
        Assert.Equal(HrzLayoutKey.Create(typeof(SideTestLayout)), diagnostics.SourceLayout);
        Assert.Equal(HrzLayoutKey.Create(typeof(SideTestLayout)), diagnostics.TargetLayout);
        Assert.Equal(nameof(HrzPageNavigationResponseMode.PageFragment), diagnostics.Mode);
    }

    [Fact]
    public async Task View_WithBoostedDifferentLayoutRequest_ReturnsRootSwap_AndStoresNavigationDiagnostics()
    {
        await using var fixture = await CreateFixtureAsync(
            headers =>
            {
                headers[HtmxHeaderNames.Request] = "true";
                headers[HtmxHeaderNames.Boosted] = "true";
                headers[HtmxHeaderNames.CurrentUrl] = "https://localhost/main";
                headers[HrzInternalHeaderNames.CurrentLayout] = HrzLayoutKey.None;
            });
        fixture.SetCurrentContext();
        fixture.HttpContext.Request.Path = "/side/detail";

        var result = await fixture.RenderService.Page<SideGreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"hrz-app-shell\"", html, StringComparison.Ordinal);
        Assert.Contains("Hello Ava", html, StringComparison.Ordinal);
        Assert.False(fixture.HttpContext.Response.Headers.ContainsKey(HtmxHeaderNames.Location));
        Assert.Equal("#hrz-app-shell", fixture.HttpContext.Response.Headers[HtmxHeaderNames.Retarget].ToString());
        Assert.Equal("outerHTML", fixture.HttpContext.Response.Headers[HtmxHeaderNames.Reswap].ToString());
        Assert.Equal("#hrz-app-shell", fixture.HttpContext.Response.Headers[HtmxHeaderNames.Reselect].ToString());
        Assert.Equal("/side/detail", fixture.HttpContext.Response.Headers[HtmxHeaderNames.PushUrl].ToString());

        var diagnostics = Assert.IsType<HtmxPageNavigationDiagnostics>(
            fixture.HttpContext.Items[typeof(HtmxPageNavigationDiagnostics)]);
        Assert.Equal("https://localhost/main", diagnostics.CurrentUrl);
        Assert.Equal(HrzLayoutKey.None, diagnostics.SourceLayout);
        Assert.Equal(HrzLayoutKey.Create(typeof(SideTestLayout)), diagnostics.TargetLayout);
        Assert.Equal(nameof(HrzPageNavigationResponseMode.RootSwap), diagnostics.Mode);
    }

    [Fact]
    public async Task View_WithBoostedRequestAndMissingCurrentLayout_ReturnsHxLocation()
    {
        await using var fixture = await CreateFixtureAsync(headers =>
        {
            headers[HtmxHeaderNames.Request] = "true";
            headers[HtmxHeaderNames.Boosted] = "true";
            headers[HtmxHeaderNames.CurrentUrl] = "https://localhost/not-mapped";
        });
        fixture.SetCurrentContext();
        fixture.HttpContext.Request.Path = "/side/detail";

        var result = await fixture.RenderService.Page<SideGreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Equal(string.Empty, html);
        Assert.True(fixture.HttpContext.Response.Headers.ContainsKey(HtmxHeaderNames.Location));

        var diagnostics = Assert.IsType<HtmxPageNavigationDiagnostics>(
            fixture.HttpContext.Items[typeof(HtmxPageNavigationDiagnostics)]);
        Assert.Equal(nameof(HrzPageNavigationResponseMode.HxLocation), diagnostics.Mode);
        Assert.Null(diagnostics.SourceLayout);
    }

    [Fact]
    public async Task View_WithBoostedRequestAndBlankCurrentLayout_ReturnsHxLocation()
    {
        await using var fixture = await CreateFixtureAsync(headers =>
        {
            headers[HtmxHeaderNames.Request] = "true";
            headers[HtmxHeaderNames.Boosted] = "true";
            headers[HrzInternalHeaderNames.CurrentLayout] = "   ";
        });
        fixture.SetCurrentContext();
        fixture.HttpContext.Request.Path = "/side/detail";

        var result = await fixture.RenderService.Page<SideGreetingComponent>(new { Name = "Ava" });
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Equal(string.Empty, html);
        Assert.True(fixture.HttpContext.Response.Headers.ContainsKey(HtmxHeaderNames.Location));
    }

    [Fact]
    public async Task PartialView_WithSubmitValidationState_RendersAttemptedValueFromHttpContext()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.HttpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("test-form"),
            Array.Empty<string>(),
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Email")] = new(new StringValues("typed@example.com"), Array.Empty<HrzAttemptedFile>())
            }));
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<AttemptedValueComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("value=\"typed@example.com\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_WithSubmitValidationState_BridgeHydratesValidationSummaryAndFieldMessage()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.HttpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("edit-form"),
            new[] { "Form summary error." },
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>
            {
                [HrzFieldPaths.FromFieldName("Email")] = new[] { "Email must be unique." }
            },
            new Dictionary<HrzFieldPath, HrzAttemptedValue>()));
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<EditFormBridgeComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Form summary error.", html, StringComparison.Ordinal);
        Assert.Contains("Email must be unique.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_EmitsLiveRuntimeContract()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationAuthoringSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"users-invite-form\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"form-custom\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"users-invite\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-disabled-elt=\"find button\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-disabled-elt=", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-server-summary\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"validation-summary validation-summary--empty summary-custom\"", html, StringComparison.Ordinal);
        Assert.Contains("label-custom", html, StringComparison.Ordinal);
        Assert.Contains("for=\"users-invite-email\"", html, StringComparison.Ordinal);
        Assert.Contains(">Email</label>", html, StringComparison.Ordinal);
        Assert.Contains("class=\"input-custom\"", html, StringComparison.Ordinal);
        Assert.Contains("placeholder=\"name@example.com\"", html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("inputmode=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/live\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-trigger=\"input changed delay:400ms, blur\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-target=\"#users-invite-email-server\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-include=\"closest form\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-sync=\"closest form:abort\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-email=", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-client-slot-id=\"users-invite-email-client\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-slot-id=\"users-invite-email-server\"", html, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"message-custom\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-live-policies\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-email-live\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-summary-slot-id=\"users-invite-server-summary\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_HonorsFieldOverrides()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationAuthoringOverridesComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"override-form\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-email\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/email-live\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-sync=\"closest form:queue last\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-live-policies\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-email-live\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-email=", html, StringComparison.Ordinal);

        Assert.Contains("id=\"override-display-name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-slot-id=\"override-display-name-server\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"override-display-name-live\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-post=\"/validation/live\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-val-minlength", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_RendersExpandedTypedInputs()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.HttpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("expanded"),
            Array.Empty<string>(),
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Notes")] = new(new StringValues("Need elevated access"), Array.Empty<HrzAttemptedFile>()),
                [HrzFieldPaths.FromFieldName("Role")] = new(new StringValues("manager"), Array.Empty<HrzAttemptedFile>()),
                [HrzFieldPaths.FromFieldName("IsAdmin")] = new(new StringValues(new[] { "false", "true" }), Array.Empty<HrzAttemptedFile>()),
                [HrzFieldPaths.FromFieldName("Age")] = new(new StringValues("42"), Array.Empty<HrzAttemptedFile>())
            }));
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationExpandedInputSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"expanded-notes\"", html, StringComparison.Ordinal);
        Assert.Contains("rows=\"5\"", html, StringComparison.Ordinal);
        Assert.Contains(">Need elevated access</textarea>", html, StringComparison.Ordinal);
        Assert.Contains("data-val-required=", html, StringComparison.Ordinal);
        Assert.Contains("data-val-minlength=", html, StringComparison.Ordinal);
        Assert.Contains("data-val-minlength-min=\"3\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-role\"", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"manager\" selected>Manager</option>", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-is-admin\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"hidden\" name=\"isAdmin\" value=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"checkbox\"", html, StringComparison.Ordinal);
        Assert.Contains("checked", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-age\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"number\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"42\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-range=", html, StringComparison.Ordinal);
        Assert.Contains("data-val-range-min=\"1\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-range-max=\"120\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/live\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-notes-live\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-role-live\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-is-admin-live\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"expanded-age-live\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_SelectPlaceholderMatchesEmptyValue()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationPlaceholderSelectSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"placeholder-select-role\"", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"\" selected disabled>Select a role</option>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"analyst\" selected>Analyst</option>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<option value=\"manager\" selected>Manager</option>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_UsesRegisteredClientValidationMetadataProviders()
    {
        await using var fixture = await CreateFixtureAsync(
            configureServices: services =>
            {
                services.AddSingleton<IHrzClientValidationMetadataProvider, RolloutPlanClientValidationMetadataProvider>();
            });
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationCustomMetadataSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("data-val-rolloutplan=", html, StringComparison.Ordinal);
        Assert.Contains("data-val-rolloutplan-keyword=\"rollback\"", html, StringComparison.Ordinal);
        Assert.Contains("data-valmsg-for=\"notes\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_AllowsManualSelectMarkup()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.RenderService.Fragment<ValidationManualSelectSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("<optgroup label=\"Operators\">", html, StringComparison.Ordinal);
        Assert.Contains("<option value=\"manager\" selected>Manager</option>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("placeholder=\"", html, StringComparison.Ordinal);
    }

    private static async Task<TestFixture> CreateFixtureAsync(
        Action<IHeaderDictionary>? configureHeaders = null,
        Action<HrzOptions>? configureOptions = null,
        Action<IServiceCollection>? configureServices = null)
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
            configureOptions?.Invoke(options);
        });
        services.AddSingleton<IHrzLayoutTypeResolver, HrzLayoutTypeResolver>();
        services.AddSingleton<IHrzFieldPathResolver>(new HrzFieldPathResolver());
        services.AddSingleton<IHrzLiveValidationPolicyResolver, HrzDefaultLiveValidationPolicyResolver>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHrzClientValidationMetadataProvider, HrzDataAnnotationsClientValidationMetadataProvider>());
        services.AddScoped<IHrzHeadService, HrzHeadService>();
        services.AddScoped<HrzSwapService>();
        services.AddScoped<IHrzSwapService>(serviceProvider => serviceProvider.GetRequiredService<HrzSwapService>());
        services.AddScoped<IHrzHtmlRendererAdapter, HrzHtmlRendererAdapter>();
        services.AddScoped<IHrzRenderService, HrzRenderService>();
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        configureHeaders?.Invoke(httpContext.Request.Headers);

        httpContextAccessor.HttpContext = httpContext;

        var renderService = scope.ServiceProvider.GetRequiredService<IHrzRenderService>();

        await Task.Yield();

        return new TestFixture(provider, scope, httpContextAccessor, httpContext, renderService);
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
            IHrzRenderService renderService)
        {
            Provider = provider;
            Scope = scope;
            HttpContextAccessor = httpContextAccessor;
            HttpContext = httpContext;
            RenderService = renderService;
        }

        public ServiceProvider Provider { get; }

        public IServiceScope Scope { get; }

        public IHttpContextAccessor HttpContextAccessor { get; }

        public HttpContext HttpContext { get; }

        public IHrzRenderService RenderService { get; }

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

    private sealed class SideTestLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }

    private sealed class AttemptedValueComponent : ComponentBase
    {
        private static readonly HrzValidationRootId RootId = new("test-form");
        private static readonly HrzFieldPath EmailPath = HrzFieldPaths.FromFieldName("Email");

        [CascadingParameter]
        public HttpContext? HttpContext { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "value", HrzFormRendering.ValueOrAttempted(
                HttpContext?.GetSubmitValidationState(RootId),
                EmailPath,
                "default@example.com"));
            builder.CloseElement();
        }
    }

    private sealed class EditFormBridgeComponent : ComponentBase
    {
        private readonly EditFormBridgeModel _model = new();
        private EditContext? _editContext;
        private static readonly HrzValidationRootId RootId = new("edit-form");

        protected override void OnInitialized()
        {
            _editContext = new EditContext(_model);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<EditContext>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<EditContext>.Value), _editContext);
            builder.AddAttribute(2, nameof(CascadingValue<EditContext>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzValidationBridge>(0);
                childBuilder.AddAttribute(1, nameof(HrzValidationBridge.RootId), RootId);
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<ValidationSummary>(2);
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<ValidationMessage<string>>(3);
                childBuilder.AddAttribute(4, nameof(ValidationMessage<string>.For), (Expression<Func<string>>)(() => _model.Email));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class EditFormBridgeModel
    {
        public string Email { get; set; } = string.Empty;
    }

    private sealed class ValidationAuthoringSurfaceComponent : ComponentBase
    {
        private readonly ValidationAuthoringModel _model = new()
        {
            Email = "riley@example.com"
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationAuthoringModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationAuthoringModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationAuthoringModel>.Action), "/users/invite");
            builder.AddAttribute(3, nameof(HrzForm<ValidationAuthoringModel>.FormName), "users-invite");
            builder.AddAttribute(4, nameof(HrzForm<ValidationAuthoringModel>.LiveValidationPath), "/validation/live");
            builder.AddAttribute(5, nameof(HrzForm<ValidationAuthoringModel>.Class), "form-custom");
            builder.AddAttribute(6, nameof(HrzForm<ValidationAuthoringModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzValidationSummary>(0);
                formBuilder.AddAttribute(1, nameof(HrzValidationSummary.Class), "summary-custom");
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField<string>>(2);
                formBuilder.AddAttribute(3, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Email));
                formBuilder.AddAttribute(4, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.AddAttribute(1, nameof(HrzLabel.Class), "label-custom");
                    fieldBuilder.CloseComponent();

                    fieldBuilder.OpenComponent<HrzInputText>(2);
                    fieldBuilder.AddAttribute(3, nameof(HrzInputText.Type), "email");
                    fieldBuilder.AddAttribute(4, nameof(HrzInputText.Class), "input-custom");
                    fieldBuilder.AddAttribute(5, nameof(HrzInputText.Placeholder), "name@example.com");
                    fieldBuilder.AddAttribute(6, nameof(HrzInputText.Autocomplete), "email");
                    fieldBuilder.AddAttribute(7, nameof(HrzInputText.InputMode), "email");
                    fieldBuilder.CloseComponent();

                    fieldBuilder.OpenComponent<HrzValidationMessage>(8);
                    fieldBuilder.AddAttribute(9, nameof(HrzValidationMessage.Class), "message-custom");
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationAuthoringOverridesComponent : ComponentBase
    {
        private readonly ValidationAuthoringModel _model = new();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationAuthoringModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationAuthoringModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationAuthoringModel>.Action), "/users/invite");
            builder.AddAttribute(3, nameof(HrzForm<ValidationAuthoringModel>.FormName), "override");
            builder.AddAttribute(4, nameof(HrzForm<ValidationAuthoringModel>.LiveValidationPath), "/validation/live");
            builder.AddAttribute(5, nameof(HrzForm<ValidationAuthoringModel>.EnableClientValidation), true);
            builder.AddAttribute(6, nameof(HrzForm<ValidationAuthoringModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzField<string>>(0);
                formBuilder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.DisplayName));
                formBuilder.AddAttribute(2, nameof(HrzField<string>.Live), false);
                formBuilder.AddAttribute(3, nameof(HrzField<string>.EnableClientValidation), false);
                formBuilder.AddAttribute(4, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputText>(1);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(2);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField<string>>(5);
                formBuilder.AddAttribute(6, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Email));
                formBuilder.AddAttribute(7, nameof(HrzField<string>.LiveValidationPath), "/validation/email-live");
                formBuilder.AddAttribute(8, nameof(HrzField<string>.LiveSync), "closest form:queue last");
                formBuilder.AddAttribute(9, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputText>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputText.Type), "email");
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(3);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationExpandedInputSurfaceComponent : ComponentBase
    {
        private static readonly IReadOnlyList<HrzInputSelectOption> RoleOptions =
        [
            new("analyst", "Analyst"),
            new("manager", "Manager"),
            new("admin", "Administrator")
        ];

        private readonly ValidationExpandedModel _model = new()
        {
            Notes = "Initial notes",
            Role = "analyst",
            IsAdmin = false,
            Age = 21
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationExpandedModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationExpandedModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationExpandedModel>.Action), "/users/invite");
            builder.AddAttribute(3, nameof(HrzForm<ValidationExpandedModel>.FormName), "expanded");
            builder.AddAttribute(4, nameof(HrzForm<ValidationExpandedModel>.LiveValidationPath), "/validation/live");
            builder.AddAttribute(5, nameof(HrzForm<ValidationExpandedModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzField<string>>(0);
                formBuilder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Notes));
                formBuilder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputTextArea>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputTextArea.Rows), 5);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(3);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField<string>>(4);
                formBuilder.AddAttribute(5, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Role));
                formBuilder.AddAttribute(6, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputSelect>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputSelect.Options), RoleOptions);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(3);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField<bool>>(8);
                formBuilder.AddAttribute(9, nameof(HrzField<bool>.For), (Expression<Func<bool>>)(() => _model.IsAdmin));
                formBuilder.AddAttribute(10, nameof(HrzField<bool>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzInputCheckbox>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzLabel>(1);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(2);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField<int>>(12);
                formBuilder.AddAttribute(13, nameof(HrzField<int>.For), (Expression<Func<int>>)(() => _model.Age));
                formBuilder.AddAttribute(14, nameof(HrzField<int>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputNumber>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputNumber.Min), "0");
                    fieldBuilder.AddAttribute(3, nameof(HrzInputNumber.Step), "1");
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(4);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationCustomMetadataSurfaceComponent : ComponentBase
    {
        private readonly ValidationCustomMetadataModel _model = new()
        {
            Notes = "Requesting rollback-ready plan."
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationCustomMetadataModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationCustomMetadataModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationCustomMetadataModel>.Action), "/validation/custom");
            builder.AddAttribute(3, nameof(HrzForm<ValidationCustomMetadataModel>.FormName), "custom-metadata");
            builder.AddAttribute(4, nameof(HrzForm<ValidationCustomMetadataModel>.EnableClientValidation), true);
            builder.AddAttribute(5, nameof(HrzForm<ValidationCustomMetadataModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzField<string>>(0);
                formBuilder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Notes));
                formBuilder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputTextArea>(1);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(2);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationPlaceholderSelectSurfaceComponent : ComponentBase
    {
        private static readonly IReadOnlyList<HrzInputSelectOption> RoleOptions =
        [
            new("analyst", "Analyst"),
            new("manager", "Manager")
        ];

        private readonly ValidationExpandedModel _model = new()
        {
            Role = string.Empty
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationExpandedModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationExpandedModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationExpandedModel>.Action), "/validation/select");
            builder.AddAttribute(3, nameof(HrzForm<ValidationExpandedModel>.FormName), "placeholder-select");
            builder.AddAttribute(4, nameof(HrzForm<ValidationExpandedModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzField<string>>(0);
                formBuilder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Role));
                formBuilder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputSelect>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputSelect.Options), RoleOptions);
                    fieldBuilder.AddAttribute(3, nameof(HrzInputSelect.Placeholder), "Select a role");
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(4);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationManualSelectSurfaceComponent : ComponentBase
    {
        private readonly ValidationExpandedModel _model = new()
        {
            Role = "manager"
        };

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<HrzForm<ValidationExpandedModel>>(0);
            builder.AddAttribute(1, nameof(HrzForm<ValidationExpandedModel>.Model), _model);
            builder.AddAttribute(2, nameof(HrzForm<ValidationExpandedModel>.Action), "/validation/select");
            builder.AddAttribute(3, nameof(HrzForm<ValidationExpandedModel>.FormName), "manual-select");
            builder.AddAttribute(4, nameof(HrzForm<ValidationExpandedModel>.ChildContent), (RenderFragment)(formBuilder =>
            {
                formBuilder.OpenComponent<HrzField<string>>(0);
                formBuilder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => _model.Role));
                formBuilder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputSelect>(1);
                    fieldBuilder.AddAttribute(2, nameof(HrzInputSelect.ChildContent), (RenderFragment)(optionsBuilder =>
                    {
                        optionsBuilder.OpenElement(0, "optgroup");
                        optionsBuilder.AddAttribute(1, "label", "Operators");
                        optionsBuilder.OpenElement(2, "option");
                        optionsBuilder.AddAttribute(3, "value", "manager");
                        optionsBuilder.AddAttribute(4, "selected", true);
                        optionsBuilder.AddContent(5, "Manager");
                        optionsBuilder.CloseElement();
                        optionsBuilder.OpenElement(6, "option");
                        optionsBuilder.AddAttribute(7, "value", "analyst");
                        optionsBuilder.AddContent(8, "Analyst");
                        optionsBuilder.CloseElement();
                        optionsBuilder.CloseElement();
                    }));
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(3);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ValidationAuthoringModel
    {
        [MinLength(3)]
        public string DisplayName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class ValidationExpandedModel
    {
        [Required]
        [MinLength(3)]
        public string Notes { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        [Range(1, 120)]
        public int Age { get; set; }
    }

    private sealed class ValidationCustomMetadataModel
    {
        [RolloutPlanKeyword("rollback")]
        public string Notes { get; set; } = string.Empty;
    }

    [AttributeUsage(AttributeTargets.Property)]
    private sealed class RolloutPlanKeywordAttribute : ValidationAttribute
    {
        public RolloutPlanKeywordAttribute(string keyword)
        {
            Keyword = keyword;
            ErrorMessage = "Notes must mention rollback.";
        }

        public string Keyword { get; }
    }

    private sealed class RolloutPlanClientValidationMetadataProvider : IHrzClientValidationMetadataProvider
    {
        public void AddValidationAttributes(
            PropertyInfo property,
            string displayName,
            IDictionary<string, string> attributes)
        {
            var attribute = property.GetCustomAttribute<RolloutPlanKeywordAttribute>();
            if (attribute is null)
            {
                return;
            }

            attributes["data-val"] = "true";
            attributes["data-val-rolloutplan"] = attribute.FormatErrorMessage(displayName);
            attributes["data-val-rolloutplan-keyword"] = attribute.Keyword;
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
