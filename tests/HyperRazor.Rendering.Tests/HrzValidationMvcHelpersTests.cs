using System.ComponentModel.DataAnnotations;
using HyperRazor;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Rendering.Tests;

public class HrzValidationMvcHelpersTests
{
    [Fact]
    public async Task HrController_HrzInvalid_UsesPartialViewForHtmxAndStoresNormalizedState()
    {
        var viewService = new TestViewService();
        using var services = CreateServices(viewService);
        var httpContext = CreateHttpContext(
            services,
            isHtmx: true,
            formValues: new Dictionary<string, StringValues>
            {
                ["displayName"] = "A",
                ["email"] = "invalid"
            });
        var controller = CreateController(httpContext);
        controller.ModelState.AddModelError("displayName", "Display name must be at least 3 characters.");

        await controller.RenderInvalid<TestRenderComponent>(
            new { Form = "users" },
            new HrzValidationRootId("users-invite"));

        Assert.Equal("partial", viewService.LastMode);
        Assert.Equal(typeof(TestRenderComponent), viewService.LastComponentType);

        var state = httpContext.GetSubmitValidationState(new HrzValidationRootId("users-invite"));
        Assert.NotNull(state);
        Assert.Contains(HrzFieldPaths.FromFieldName("DisplayName"), state!.FieldErrors.Keys);
        Assert.Equal("A", state.AttemptedValues[HrzFieldPaths.FromFieldName("DisplayName")].Values[0]);
    }

    [Fact]
    public async Task HrController_HrzValid_UsesViewForNormalRequestAndClearsValidationState()
    {
        var viewService = new TestViewService();
        using var services = CreateServices(viewService);
        var httpContext = CreateHttpContext(services, isHtmx: false);
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            ["Invalid"],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>()));
        var controller = CreateController(httpContext);

        await controller.RenderValid<TestRenderComponent>(new { Form = "users" });

        Assert.Equal("view", viewService.LastMode);
        Assert.Equal(typeof(TestRenderComponent), viewService.LastComponentType);
        Assert.Null(httpContext.GetSubmitValidationState());
    }

    [Fact]
    public async Task HrzPosted_BindAsync_ReadsRootAndPreservesAttemptedValues()
    {
        using var services = CreateServices();
        var httpContext = CreateHttpContext(
            services,
            isHtmx: false,
            formValues: new Dictionary<string, StringValues>
            {
                [HrzValidationFormFields.Root] = "posted-form",
                ["displayName"] = "A",
                ["email"] = "invalid"
            });

        var posted = await HrzPosted<PostedModel>.BindAsync(httpContext);

        Assert.Equal("posted-form", posted.RootId.Value);
        Assert.False(posted.IsValid);
        Assert.Equal("A", posted.ValidationState.AttemptedValues[HrzFieldPaths.FromFieldName("DisplayName")].Values[0]);
        Assert.Equal("invalid", posted.ValidationState.AttemptedValues[HrzFieldPaths.FromFieldName("Email")].Values[0]);
    }

    [Fact]
    public async Task HrzPosted_Invalid_UsesPartialViewAndSetsExplicitState()
    {
        var viewService = new TestViewService();
        using var services = CreateServices(viewService);
        var httpContext = CreateHttpContext(services, isHtmx: true);
        var explicitState = new HrzSubmitValidationState(
            new HrzValidationRootId("posted-form"),
            ["Backend rejection."],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>());
        var posted = new HrzPosted<PostedModel>
        {
            HttpContext = httpContext,
            RootId = explicitState.RootId,
            Model = new PostedModel(),
            ValidationState = explicitState
        };

        await posted.Invalid<TestRenderComponent>(explicitState, new { Form = "posted" });

        Assert.Equal("partial", viewService.LastMode);
        Assert.Same(explicitState, httpContext.GetSubmitValidationState());
    }

    [Fact]
    public async Task HrzPosted_Valid_UsesViewAndClearsState()
    {
        var viewService = new TestViewService();
        using var services = CreateServices(viewService);
        var httpContext = CreateHttpContext(services, isHtmx: false);
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("posted-form"),
            ["Backend rejection."],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>()));
        var posted = new HrzPosted<PostedModel>
        {
            HttpContext = httpContext,
            RootId = new HrzValidationRootId("posted-form"),
            Model = new PostedModel(),
            ValidationState = new HrzSubmitValidationState(
                new HrzValidationRootId("posted-form"),
                [],
                new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
                new Dictionary<HrzFieldPath, HrzAttemptedValue>())
        };

        await posted.Valid<TestRenderComponent>(new { Form = "posted" });

        Assert.Equal("view", viewService.LastMode);
        Assert.Null(httpContext.GetSubmitValidationState());
    }

    private static ServiceProvider CreateServices(TestViewService? viewService = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddHyperRazor();

        if (viewService is not null)
        {
            services.AddSingleton<IHrzComponentViewService>(viewService);
        }

        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateHttpContext(
        IServiceProvider services,
        bool isHtmx,
        Dictionary<string, StringValues>? formValues = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        if (isHtmx)
        {
            context.Request.Headers[HtmxHeaderNames.Request] = "true";
        }

        if (formValues is not null)
        {
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(formValues)));
        }

        return context;
    }

    private static TestValidationController CreateController(HttpContext httpContext)
    {
        return new TestValidationController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private sealed class TestValidationController : HrController
    {
        public Task<IResult> RenderInvalid<TComponent>(
            object? data,
            HrzValidationRootId rootId)
            where TComponent : IComponent =>
            HrzInvalid<TComponent>(data, validationRootId: rootId);

        public Task<IResult> RenderValid<TComponent>(object? data)
            where TComponent : IComponent =>
            HrzValid<TComponent>(data);
    }

    private sealed class TestViewService : IHrzComponentViewService
    {
        public string? LastMode { get; private set; }

        public Type? LastComponentType { get; private set; }

        public object? LastData { get; private set; }

        public Task<IResult> View<TComponent>(object? data = null, CancellationToken cancellationToken = default)
            where TComponent : IComponent
        {
            LastMode = "view";
            LastComponentType = typeof(TComponent);
            LastData = data;
            return Task.FromResult<IResult>(Results.Text("view"));
        }

        public Task<IResult> View<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
            where TComponent : IComponent
        {
            LastMode = "view";
            LastComponentType = typeof(TComponent);
            LastData = data;
            return Task.FromResult<IResult>(Results.Text("view"));
        }

        public Task<IResult> PartialView<TComponent>(object? data = null, CancellationToken cancellationToken = default)
            where TComponent : IComponent
        {
            LastMode = "partial";
            LastComponentType = typeof(TComponent);
            LastData = data;
            return Task.FromResult<IResult>(Results.Text("partial"));
        }

        public Task<IResult> PartialView<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
            where TComponent : IComponent
        {
            LastMode = "partial";
            LastComponentType = typeof(TComponent);
            LastData = data;
            return Task.FromResult<IResult>(Results.Text("partial"));
        }

        public Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments)
        {
            LastMode = "fragment";
            LastComponentType = null;
            LastData = fragments;
            return Task.FromResult<IResult>(Results.Text("fragment"));
        }
    }

    private sealed class TestRenderComponent : ComponentBase
    {
    }

    private sealed class PostedModel
    {
        [Required]
        [MinLength(3)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
