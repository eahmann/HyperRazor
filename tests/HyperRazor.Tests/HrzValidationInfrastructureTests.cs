using System.ComponentModel.DataAnnotations;
using HyperRazor.Components;
using HyperRazor;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HyperRazor.Tests;

public class HrzValidationInfrastructureTests
{
    [Fact]
    public void DataAnnotationsValidator_MapsFieldErrorsAndPreservesAttemptedValues()
    {
        var validator = new HrzDataAnnotationsModelValidator(new HrzFieldPathResolver());
        var attemptedValues = new Dictionary<HrzFieldPath, HrzAttemptedValue>
        {
            [HrzFieldPaths.FromFieldName(nameof(Phase3ValidationModel.Email))] = new(new[] { "invalid" }, Array.Empty<HrzAttemptedFile>())
        };

        var state = validator.Validate(
            new Phase3ValidationModel(),
            new HrzValidationRootId("validator-test"),
            attemptedValues);

        Assert.False(state.IsValid);
        Assert.Contains(HrzFieldPaths.FromFieldName(nameof(Phase3ValidationModel.Email)), state.FieldErrors.Keys);
        Assert.Same(attemptedValues, state.AttemptedValues);
    }

    [Fact]
    public void ValidationProblemDetails_Mapping_NormalizesKeysAndPreservesAttemptedValues()
    {
        var attemptedValues = new Dictionary<HrzFieldPath, HrzAttemptedValue>
        {
            [HrzFieldPaths.FromFieldName("Email")] = new(new[] { "blocked@example.com" }, Array.Empty<HrzAttemptedFile>())
        };
        var details = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["input.email"] = ["Email is blocked by backend policy."],
            [""] = ["Summary rejection."]
        });

        var state = details.ToSubmitValidationState(
            new HrzValidationRootId("backend-test"),
            new HrzFieldPathResolver(),
            attemptedValues);

        Assert.False(state.IsValid);
        Assert.Contains("Summary rejection.", state.SummaryErrors);
        Assert.Contains(HrzFieldPaths.FromFieldName("Email"), state.FieldErrors.Keys);
        Assert.Equal("blocked@example.com", state.AttemptedValues[HrzFieldPaths.FromFieldName("Email")].Values[0]);
    }

    [Fact]
    public async Task BindFormAndValidateAsync_PreservesAttemptedValuesAfterSuccessfulLocalValidation()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "riley@example.com"
        })));

        var postState = await context.BindFormAndValidateAsync<InviteLikeFormModel>(new HrzValidationRootId("minimal-api"));

        Assert.True(postState.ValidationState.IsValid);
        Assert.Equal("Riley Stone", postState.Model.DisplayName);
        Assert.Equal("riley@example.com", postState.Model.Email);
        Assert.Equal("Riley Stone", postState.ValidationState.AttemptedValues[HrzFieldPaths.FromFieldName("DisplayName")].Values[0]);
        Assert.Equal("riley@example.com", postState.ValidationState.AttemptedValues[HrzFieldPaths.FromFieldName("Email")].Values[0]);
    }

    [Fact]
    public async Task BindLiveValidationRequestAsync_ReadsRootFieldsAndValidateAll()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["__hrz_root"] = "invite-live",
            ["__hrz_fields"] = "displayName,email",
            ["__hrz_validate_all"] = "true"
        })));

        var scopeModel = await context.BindLiveValidationRequestAsync();

        Assert.NotNull(scopeModel);
        Assert.Equal("invite-live", scopeModel!.RootId.Value);
        Assert.True(scopeModel.ValidateAll);
        Assert.Collection(
            scopeModel.Fields,
            field => Assert.Equal("DisplayName", field.Value),
            field => Assert.Equal("Email", field.Value));
    }

    [Fact]
    public void HrzForms_For_FormName_UsesFormNameAsRootAndCurrentRequestValidationState()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var rootId = new HrzValidationRootId("users-invite");
        var validationState = new HrzSubmitValidationState(
            rootId,
            Array.Empty<string>(),
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>
            {
                [HrzFieldPaths.FromFieldName(nameof(InviteLikeFormModel.Email))] = new(new[] { "typed@example.com" }, Array.Empty<HrzAttemptedFile>())
            });
        var context = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        context.SetSubmitValidationState(validationState);
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;

        var model = new InviteLikeFormModel
        {
            Email = "server@example.com"
        };
        var forms = scope.ServiceProvider.GetRequiredService<IHrzForms>();

        var form = forms.For(
            model,
            formName: "users-invite",
            live: new HrzLiveValidationOptions { Path = "/validation/live" });
        var email = form.Field(() => model.Email);

        Assert.Equal(rootId, form.RootId);
        Assert.Same(validationState, form.ValidationState);
        Assert.Equal("users-invite-form", form.FormId);
        Assert.Equal("typed@example.com", email.Value);
        Assert.Equal("/validation/live", email.LiveValidationPath);
    }

    [Fact]
    public void HrzFieldScope_AsTextInput_OmitsClientSlotReferencesWhenRequested()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var model = new InviteLikeFormModel
        {
            Email = "riley@example.com"
        };
        var forms = scope.ServiceProvider.GetRequiredService<IHrzForms>();

        var form = forms.For(model, formName: "users-invite", enableClientValidation: true);
        var email = form.Field(() => model.Email);
        var attributes = email.AsTextInput(includeClientValidationSlot: false);

        Assert.Equal(email.ServerSlotId, attributes["aria-describedby"]);
        Assert.False(attributes.ContainsKey("data-hrz-client-slot-id"));
    }

    [Fact]
    public void HrzFieldScope_AsCheckbox_ReflectsCheckedState()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var forms = scope.ServiceProvider.GetRequiredService<IHrzForms>();

        var checkedModel = new CheckboxModel
        {
            IsSelected = true
        };
        var uncheckedModel = new CheckboxModel
        {
            IsSelected = false
        };

        var checkedField = forms.For(checkedModel, formName: "checked").Field(() => checkedModel.IsSelected);
        var uncheckedField = forms.For(uncheckedModel, formName: "unchecked").Field(() => uncheckedModel.IsSelected);

        Assert.True(checkedField.AsCheckbox().ContainsKey("checked"));
        Assert.False(uncheckedField.AsCheckbox().ContainsKey("checked"));
    }

    [Fact]
    public async Task DefaultLiveValidationPolicyResolver_PreservesAlwaysArmedBehavior()
    {
        var services = CreateServices();
        using var scope = services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<IHrzLiveValidationPolicyResolver>();

        var policy = await resolver.ResolveAsync(
            new InviteLikeFormModel(),
            new HrzValidationRootId("invite-live"),
            HrzFieldPaths.FromFieldName(nameof(InviteLikeFormModel.Email)),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>());

        Assert.True(policy.Enabled);
        Assert.Empty(policy.DependsOn);
        Assert.Empty(policy.AffectedFields);
        Assert.Empty(policy.ClearFields);
        Assert.False(policy.ReplaceSummaryWhenDisabled);
        Assert.False(policy.ImmediateRecheckWhenEnabled);
    }

    [Fact]
    public void AddHyperRazor_RegistersConfiguredSseDefaults()
    {
        using var services = CreateServices(configureSse: options =>
        {
            options.HeartbeatInterval = TimeSpan.FromSeconds(9);
            options.HeartbeatComment = "global-heartbeat";
            options.DisableProxyBuffering = false;
        });

        var options = services.GetRequiredService<IOptions<HrzSseOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(9), options.HeartbeatInterval);
        Assert.Equal("global-heartbeat", options.HeartbeatComment);
        Assert.False(options.DisableProxyBuffering);
    }

    [Fact]
    public void AddHyperRazorComponentServices_RegistersHttpContextAccessorAndForms()
    {
        var services = new ServiceCollection();
        services.AddHyperRazorComponentServices();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IHttpContextAccessor>());
        Assert.NotNull(provider.GetRequiredService<IHrzForms>());
    }

    private static ServiceProvider CreateServices(Action<HrzSseOptions>? configureSse = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        services.AddHyperRazor(configureSse: configureSse);
        return services.BuildServiceProvider();
    }

    private sealed class InviteLikeFormModel
    {
        [Required]
        [MinLength(3)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    private sealed class Phase3ValidationModel : IValidatableObject
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email must be a valid address.")]
        public string Email { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield return new ValidationResult("Model-level rejection.");
        }
    }

    private sealed class CheckboxModel
    {
        public bool IsSelected { get; set; }
    }
}
