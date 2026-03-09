using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using HyperRazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Rendering.Tests;

public class HrzFormAuthoringComponentsTests
{
    [Fact]
    public async Task HrzForm_EmitsAntiforgeryRootFieldAndEnhancedSubmitDefaults()
    {
        var model = new AuthoringModel();

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite"
            });

        Assert.Contains("<form", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite\"", html, StringComparison.Ordinal);
        Assert.Contains("method=\"post\"", html, StringComparison.Ordinal);
        Assert.Contains("action=\"/users/invite\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/users/invite\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-target=\"#users-invite\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap=\"outerHTML\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"__RequestVerificationToken\"", html, StringComparison.Ordinal);
        Assert.Contains($"name=\"{HrzValidationFormFields.Root}\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"users-invite\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzForm_WithEnhanceFalse_DoesNotEmitEnhancedSubmitDefaults()
    {
        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = new AuthoringModel(),
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.Enhance)] = false
            });

        Assert.DoesNotContain("hx-post=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-target=", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-swap=", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzField_ResolvesIndexedFieldContextAndAttemptedValueAccess()
    {
        var model = new AuthoringModel
        {
            Items =
            [
                new AuthoringItem
                {
                    Name = "Original"
                }
            ]
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Items[0].Name")] = new(
                    new StringValues("Edited"),
                    Array.Empty<HrzAttemptedFile>())
            }));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildIndexedFieldContent(model)
            },
            httpContext);

        Assert.Contains("data-path=\"Items[0].Name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-name=\"Items[0].Name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-id=\"users-invite-items-0-name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-message-id=\"users-invite-items-0-name-message\"", html, StringComparison.Ordinal);
        Assert.Contains("data-display-name=\"Item Name\"", html, StringComparison.Ordinal);
        Assert.Contains("data-current-value=\"Original\"", html, StringComparison.Ordinal);
        Assert.Contains("data-attempted-value=\"Edited\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationSummaryAndMessage_RenderSubmitStateAndAlignWithInputDescription()
    {
        var model = new AuthoringModel
        {
            Email = "not-an-email"
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            ["Summary rejection."],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>
            {
                [HrzFieldPaths.FromFieldName("Email")] = ["Enter a valid email address."]
            },
            new Dictionary<HrzFieldPath, HrzAttemptedValue>()));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildSummaryAndMessageContent(model)
            },
            httpContext);

        Assert.Contains("id=\"users-invite-summary\"", html, StringComparison.Ordinal);
        Assert.Contains("Summary rejection.", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-email-message\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-email-message--client\"", html, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-email-message--server\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-validation-for=\"Email\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"users-invite-email-message\"", html, StringComparison.Ordinal);
        Assert.Contains("aria-invalid", html, StringComparison.Ordinal);
        Assert.Contains("Enter a valid email address.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzInput_ReplaysAttemptedValueAndEmitsValidationAndLiveMetadata()
    {
        var model = new AuthoringModel
        {
            Email = "original@example.com"
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Email")] = new(
                    new StringValues("edited@example.com"),
                    Array.Empty<HrzAttemptedFile>())
            }));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildEmailInputContent(model)
            },
            httpContext,
            services => services.AddSingleton<IHrzValidationDescriptorProvider>(CreateLiveDescriptorProvider()));

        Assert.Contains("type=\"email\"", html, StringComparison.Ordinal);
        Assert.Contains("value=\"edited@example.com\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val=\"true\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-required=\"Email is required.\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-email=\"Enter a valid email address.\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/users/live-validate\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-target=\"#users-invite-email-message--server\"", html, StringComparison.Ordinal);
        Assert.Contains("hx-include=\"closest form\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-client-slot-id=\"users-invite-email-message--client\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-slot-id=\"users-invite-email-message--server\"", html, StringComparison.Ordinal);
        Assert.Contains("data-hrz-summary-slot-id=\"users-invite-summary\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzInput_Password_DoesNotReplayValuesOrEmitLiveMetadata()
    {
        var model = new AuthoringModel
        {
            Password = "OriginalSecret!"
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Password")] = new(
                    new StringValues("AttemptedSecret!"),
                    Array.Empty<HrzAttemptedFile>())
            }));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildPasswordInputContent(model)
            },
            httpContext,
            services => services.AddSingleton<IHrzValidationDescriptorProvider>(CreateLiveDescriptorProvider()));

        Assert.Contains("type=\"password\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"OriginalSecret!\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"AttemptedSecret!\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-target=\"#users-invite-password-message--server\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hrz-server-slot-id=\"users-invite-password-message--server\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-required=\"Password is required.\"", html, StringComparison.Ordinal);
        Assert.Contains("data-val-minlength=\"Password must be at least 8 characters.\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzTextArea_UsesAttemptedValueAsContent()
    {
        var model = new AuthoringModel
        {
            Description = "Original description"
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("Description")] = new(
                    new StringValues("Edited description"),
                    Array.Empty<HrzAttemptedFile>())
            }));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildTextAreaContent(model)
            },
            httpContext);

        Assert.Contains("<textarea", html, StringComparison.Ordinal);
        Assert.Contains("Edited description", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Original description</textarea>", html, StringComparison.Ordinal);
        Assert.Contains("data-val-length=", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzLabel_UsesAmbientDisplayNameAndFieldId()
    {
        var model = new AuthoringModel
        {
            Email = "riley@example.com"
        };

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildLabelContent(model)
            });

        Assert.Contains("<label for=\"users-invite-email\">Email Address</label>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HrzCheckbox_RendersHiddenFalseCompanionAndAttemptedStateWins()
    {
        var model = new AuthoringModel
        {
            AcceptTerms = true
        };
        var httpContext = new DefaultHttpContext();
        httpContext.SetSubmitValidationState(new HrzSubmitValidationState(
            new HrzValidationRootId("users-invite"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName("AcceptTerms")] = new(
                    new StringValues("false"),
                    Array.Empty<HrzAttemptedFile>())
            }));

        var html = await RenderComponentAsync<HrzForm<AuthoringModel>>(
            new Dictionary<string, object?>
            {
                [nameof(HrzForm<AuthoringModel>.Model)] = model,
                [nameof(HrzForm<AuthoringModel>.Action)] = "/users/invite",
                [nameof(HrzForm<AuthoringModel>.FormName)] = "users-invite",
                [nameof(HrzForm<AuthoringModel>.ChildContent)] = BuildCheckboxContent(model)
            },
            httpContext);

        Assert.Contains("<input type=\"hidden\" name=\"AcceptTerms\" value=\"false\"", html, StringComparison.Ordinal);
        Assert.Contains("type=\"checkbox\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"AcceptTerms\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("checked=\"checked\"", html, StringComparison.Ordinal);
    }

    private static RenderFragment BuildIndexedFieldContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<string>>(0);
            builder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Items[0].Name));
            builder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<FieldContextProbe>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildSummaryAndMessageContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzValidationSummary>(0);
            builder.CloseComponent();

            builder.OpenComponent<HrzField<string>>(1);
            builder.AddAttribute(2, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Email));
            builder.AddAttribute(3, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzInput>(0);
                childBuilder.AddAttribute(1, nameof(HrzInput.Type), "email");
                childBuilder.CloseComponent();

                childBuilder.OpenComponent<HrzValidationMessage>(2);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildEmailInputContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<string>>(0);
            builder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Email));
            builder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzInput>(0);
                childBuilder.AddAttribute(1, nameof(HrzInput.Type), "email");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildPasswordInputContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<string>>(0);
            builder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Password));
            builder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzInput>(0);
                childBuilder.AddAttribute(1, nameof(HrzInput.Type), "password");
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildTextAreaContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<string>>(0);
            builder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Description));
            builder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzTextArea>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildLabelContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<string>>(0);
            builder.AddAttribute(1, nameof(HrzField<string>.For), (Expression<Func<string>>)(() => model.Email));
            builder.AddAttribute(2, nameof(HrzField<string>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzLabel>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment BuildCheckboxContent(AuthoringModel model)
    {
        return builder =>
        {
            builder.OpenComponent<HrzField<bool>>(0);
            builder.AddAttribute(1, nameof(HrzField<bool>.For), (Expression<Func<bool>>)(() => model.AcceptTerms));
            builder.AddAttribute(2, nameof(HrzField<bool>.ChildContent), (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<HrzCheckbox>(0);
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static async Task<string> RenderComponentAsync<TComponent>(
        IReadOnlyDictionary<string, object?> parameters,
        HttpContext? httpContext = null,
        Action<IServiceCollection>? configureServices = null)
        where TComponent : IComponent
    {
        httpContext ??= new DefaultHttpContext();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        var environment = new TestWebHostEnvironment();
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddControllers();
        services.AddAntiforgery();
        services.AddRazorComponents();
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = httpContext
        });
        configureServices?.Invoke(services);
        services.AddHyperRazor();

        using var provider = services.BuildServiceProvider();
        httpContext.RequestServices = provider;

        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        await using var renderer = new HtmlRenderer(provider, loggerFactory);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var rendered = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(
                parameters.ToDictionary(static pair => pair.Key, static pair => pair.Value)));
            return rendered.ToHtmlString();
        });
    }

    private sealed class FieldContextProbe : ComponentBase
    {
        [CascadingParameter]
        public HrzFieldContext? Field { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(Field);

            builder.OpenElement(0, "output");
            builder.AddAttribute(1, "data-path", Field.Path.Value);
            builder.AddAttribute(2, "data-name", Field.HtmlName);
            builder.AddAttribute(3, "data-id", Field.HtmlId);
            builder.AddAttribute(4, "data-message-id", Field.MessageId);
            builder.AddAttribute(5, "data-display-name", Field.Descriptor.DisplayName);
            builder.AddAttribute(6, "data-current-value", Convert.ToString(Field.CurrentValue, System.Globalization.CultureInfo.InvariantCulture));
            builder.AddAttribute(
                7,
                "data-attempted-value",
                HrzFormRendering.ValueOrAttempted(
                    Field.Form.SubmitValidationState,
                    Field.Path,
                    Convert.ToString(Field.CurrentValue, System.Globalization.CultureInfo.InvariantCulture)));
            builder.CloseElement();
        }
    }

    private sealed class AuthoringModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description must be 200 characters or fewer.")]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(bool), "true", "true", ErrorMessage = "Accept terms is required.")]
        public bool AcceptTerms { get; set; }

        public List<AuthoringItem> Items { get; set; } = [];
    }

    private sealed class AuthoringItem
    {
        [Display(Name = "Item Name")]
        public string Name { get; set; } = string.Empty;
    }

    private static IHrzValidationDescriptorProvider CreateLiveDescriptorProvider()
    {
        var resolver = new HrzFieldPathResolver();
        var baseProvider = new HrzDataAnnotationsValidationDescriptorProvider(resolver);
        var descriptor = baseProvider.GetDescriptor(typeof(AuthoringModel));
        var fields = new Dictionary<HrzFieldPath, HrzFieldDescriptor>(descriptor.Fields)
        {
            [resolver.FromFieldName("Email")] = new HrzFieldDescriptor
            {
                Path = resolver.FromFieldName("Email"),
                HtmlName = "Email",
                DisplayName = "Email Address",
                LocalRules = descriptor.Fields[resolver.FromFieldName("Email")].LocalRules,
                LiveRule = new HrzLiveRuleDescriptor
                {
                    Endpoint = "/users/live-validate"
                }
            },
            [resolver.FromFieldName("Password")] = new HrzFieldDescriptor
            {
                Path = resolver.FromFieldName("Password"),
                HtmlName = "Password",
                DisplayName = "Password",
                LocalRules = descriptor.Fields[resolver.FromFieldName("Password")].LocalRules,
                LiveRule = new HrzLiveRuleDescriptor
                {
                    Endpoint = "/users/live-validate"
                }
            }
        };

        return new TestValidationDescriptorProvider(new HrzValidationDescriptor
        {
            ModelType = typeof(AuthoringModel),
            Fields = fields
        });
    }

    private sealed class TestValidationDescriptorProvider : IHrzValidationDescriptorProvider
    {
        private readonly HrzValidationDescriptor _descriptor;

        public TestValidationDescriptorProvider(HrzValidationDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        public HrzValidationDescriptor GetDescriptor(Type modelType)
        {
            Assert.Equal(typeof(AuthoringModel), modelType);
            return _descriptor;
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
