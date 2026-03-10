using System.IO;
using System.ComponentModel.DataAnnotations;
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
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;

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

        var result = await fixture.ViewService.PartialView<AttemptedValueComponent>();
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

        var result = await fixture.ViewService.PartialView<EditFormBridgeComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("Form summary error.", html, StringComparison.Ordinal);
        Assert.Contains("Email must be unique.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PartialView_ValidationAuthoringSurface_EmitsLiveRuntimeContract()
    {
        await using var fixture = await CreateFixtureAsync();
        fixture.SetCurrentContext();

        var result = await fixture.ViewService.PartialView<ValidationAuthoringSurfaceComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"users-invite-form\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"form-custom\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"users-invite\"", html, StringComparison.Ordinal);
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
        Assert.Contains("data-hrz-local-validation=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-client-slot-id=\"users-invite-email-client\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-slot-id=\"users-invite-email-server\"", html, StringComparison.Ordinal);
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

        var result = await fixture.ViewService.PartialView<ValidationAuthoringOverridesComponent>();
        var html = await ExecuteResultAsync(result, fixture.HttpContext);

        Assert.Contains("id=\"override-form\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-email\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/email-live\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-sync=\"closest form:queue last\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-live-policies\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"override-email-live\"", html, StringComparison.Ordinal);

        Assert.Contains("id=\"override-display-name\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"override-display-name-live\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-post=\"/validation/live\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hrz-local-min-length", html, StringComparison.Ordinal);
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
        services.AddSingleton<IHrzFieldPathResolver>(new HrzFieldPathResolver());
        services.AddSingleton<IHrzLiveValidationPolicyResolver, HrzDefaultLiveValidationPolicyResolver>();
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

                formBuilder.OpenComponent<HrzField>(2);
                formBuilder.AddAttribute(3, nameof(HrzField.For), (Expression<Func<string?>>)(() => _model.Email));
                formBuilder.AddAttribute(4, nameof(HrzField.ChildContent), (RenderFragment)(fieldBuilder =>
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
                formBuilder.OpenComponent<HrzField>(0);
                formBuilder.AddAttribute(1, nameof(HrzField.For), (Expression<Func<string?>>)(() => _model.DisplayName));
                formBuilder.AddAttribute(2, nameof(HrzField.Live), false);
                formBuilder.AddAttribute(3, nameof(HrzField.EnableClientValidation), false);
                formBuilder.AddAttribute(4, nameof(HrzField.ChildContent), (RenderFragment)(fieldBuilder =>
                {
                    fieldBuilder.OpenComponent<HrzLabel>(0);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzInputText>(1);
                    fieldBuilder.CloseComponent();
                    fieldBuilder.OpenComponent<HrzValidationMessage>(2);
                    fieldBuilder.CloseComponent();
                }));
                formBuilder.CloseComponent();

                formBuilder.OpenComponent<HrzField>(5);
                formBuilder.AddAttribute(6, nameof(HrzField.For), (Expression<Func<string?>>)(() => _model.Email));
                formBuilder.AddAttribute(7, nameof(HrzField.LiveValidationPath), "/validation/email-live");
                formBuilder.AddAttribute(8, nameof(HrzField.LiveSync), "closest form:queue last");
                formBuilder.AddAttribute(9, nameof(HrzField.ChildContent), (RenderFragment)(fieldBuilder =>
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

    private sealed class ValidationAuthoringModel
    {
        [MinLength(3)]
        public string DisplayName { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;
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
