using HyperRazor.Components;
using HyperRazor.Components.Services;
using HyperRazor.Demo.Mvc.Components;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddHyperRazor(options =>
{
    options.RootComponent = typeof(HrzApp<AppLayout>);
    options.UseMinimalLayoutForHtmx = true;
    options.LayoutBoundary.Enabled = true;
    options.LayoutBoundary.OnlyBoostedRequests = true;
    options.LayoutBoundary.PromotionMode = HrzLayoutBoundaryPromotionMode.ShellSwap;
    options.LayoutBoundary.LayoutFamilyHeaderName = HtmxHeaderNames.LayoutFamily;
    options.LayoutBoundary.DefaultLayoutFamily = "admin";
    options.LayoutBoundary.ShellTargetSelector = "#hrz-app-shell";
    options.LayoutBoundary.ShellSwapStyle = "outerHTML";
    options.LayoutBoundary.ShellReselectSelector = "#hrz-app-shell";
    options.LayoutBoundary.AddVaryHeader = true;
});
builder.Services.AddHtmx(htmx =>
{
    htmx.ClientProfile = HtmxClientProfile.Htmx2Defaults;
    htmx.SelfRequestsOnly = true;
    htmx.HistoryRestoreAsHxRequest = false;
    htmx.AllowNestedOobSwaps = false;
    htmx.DefaultSwapStyle = "outerHTML";
    htmx.EnableHeadSupport = true;
    htmx.AntiforgeryMetaName = "hrz-antiforgery";
    htmx.AntiforgeryHeaderName = "RequestVerificationToken";
    htmx.ResponseHandling =
    [
        new HtmxResponseHandlingRule
        {
            Code = "204",
            Swap = false
        },
        new HtmxResponseHandlingRule
        {
            Code = "[23]..",
            Swap = true
        },
        new HtmxResponseHandlingRule
        {
            Code = "[45]..",
            Swap = true,
            Error = false
        }
    ];
});
builder.Services.AddSingleton<IInviteValidationBackend, DemoInviteValidationBackend>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseHyperRazor();
app.Use(async (context, next) =>
{
    var request = context.HtmxRequest();
    if (HttpMethods.IsGet(context.Request.Method)
        && request.RequestType == HtmxRequestType.Partial
        && !request.IsHistoryRestoreRequest
        && DemoChromeState.IsPageChromeRoute(context.Request.Path))
    {
        var chromeState = DemoChromeState.Create(context);
        var swapService = context.RequestServices.GetRequiredService<IHrzSwapService>();

        swapService.QueueComponent<DemoChromeToolbar>(
            targetId: "app-chrome-toolbar",
            parameters: new
            {
                chromeState.RouteLabel,
                chromeState.LayoutFamily,
                chromeState.Theme
            },
            swapStyle: SwapStyle.OuterHtml);

        swapService.QueueComponent<DemoChromeSidebar>(
            targetId: "app-chrome-sidebar",
            parameters: new
            {
                chromeState.ActiveSection,
                chromeState.LayoutFamily
            },
            swapStyle: SwapStyle.OuterHtml);
    }

    await next();
});

// AdminLayout routes are intentionally served via Minimal API so the demo shows parity in a real app area.
app.MapGet("/", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<DashboardPage>(context, cancellationToken: cancellationToken));
app.MapGet("/users", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<UsersPage>(context, cancellationToken: cancellationToken));
app.MapGet("/validation", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<ValidationPage>(context, cancellationToken: cancellationToken));
app.MapGet("/settings/branding", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<BrandingSettingsPage>(context, cancellationToken: cancellationToken));
app.MapPost("/validation/minimal/local", async (
    HttpContext context,
    HrzPosted<InviteUserInput> posted,
    IAntiforgery antiforgery,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (!posted.IsValid)
    {
        context.SetSubmitValidationState(posted.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(posted.ValidationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-local-invalid",
            details: $"Minimal API local validation failed with {CountErrors(posted.ValidationState)} error(s).");

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalInviteForm),
            UserInviteValidationDefinitions.MinimalLocal(posted.Model),
            cancellationToken);
    }

    var count = Random.Shared.Next(100, 200);
    context.HtmxResponse().Trigger("form:valid", new
    {
        name = posted.Model.DisplayName,
        email = posted.Model.Email,
        count
    });
    DemoInspectorUpdates.Queue(
        context,
        action: "validation-minimal-local-valid",
        details: $"Minimal API local validation accepted {posted.Model.DisplayName} (#{count}).");

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
            context,
            new
            {
                Form = UserInviteValidationDefinitions.MinimalLocal(posted.Model, success: true, count: count)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});
app.MapPost("/validation/minimal/proxy", async (
    HttpContext context,
    HrzPosted<InviteUserInput> posted,
    IAntiforgery antiforgery,
    IInviteValidationBackend inviteValidationBackend,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    if (!posted.IsValid)
    {
        context.SetSubmitValidationState(posted.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(posted.ValidationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-proxy-invalid",
            details: $"Minimal API proxy validation failed locally with {CountErrors(posted.ValidationState)} error(s) before the backend call.");

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalProxyInviteForm),
            UserInviteValidationDefinitions.MinimalProxy(posted.Model),
            cancellationToken);
    }

    var backendResult = await inviteValidationBackend.SubmitAsync(posted.Model, cancellationToken);
    if (!backendResult.IsSuccess)
    {
        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var validationState = backendResult.ProblemDetails!.ToSubmitValidationState(
            posted.RootId,
            resolver,
            posted.ValidationState.AttemptedValues);
        context.SetSubmitValidationState(validationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(validationState)
        });
        DemoInspectorUpdates.Queue(
            context,
            action: "validation-minimal-proxy-backend-invalid",
            details: "Minimal API proxy mapped backend validation JSON back into the server-rendered form fragment.");

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalProxyInviteForm),
            UserInviteValidationDefinitions.MinimalProxy(posted.Model),
            cancellationToken);
    }

    context.HtmxResponse().Trigger("form:valid", new
    {
        name = posted.Model.DisplayName,
        email = posted.Model.Email,
        count = backendResult.Count
    });
    DemoInspectorUpdates.Queue(
        context,
        action: "validation-minimal-proxy-valid",
        details: $"Minimal API proxy validated successfully and the backend accepted {posted.Model.DisplayName} (#{backendResult.Count}).");

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
            context,
            new
            {
                Form = UserInviteValidationDefinitions.MinimalProxy(
                    posted.Model,
                    success: true,
                    count: backendResult.Count)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});
app.MapPost("/validation/live", async (
    HttpContext context,
    IAntiforgery antiforgery,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var scope = await context.BindLiveValidationScopeAsync(cancellationToken);
    if (scope is null || scope.Fields.Count == 0)
    {
        return Results.NoContent();
    }

    var formPostState = await context.BindFormAsync<InviteUserInput>(scope.RootId, cancellationToken);
    if (!UserInviteValidationDefinitions.TryResolve(scope.RootId, formPostState.Model, out var form))
    {
        return Results.NoContent();
    }

    var primaryField = ResolvePrimaryField(scope);
    if (primaryField is null)
    {
        return Results.NoContent();
    }

    var livePatch = BuildInviteLiveValidationPatch(scope, formPostState.Model);
    if (livePatch is null)
    {
        return Results.NoContent();
    }

    var htmlIdGenerator = context.RequestServices.GetRequiredService<IHrzHtmlIdGenerator>();

    var fragments = new List<RenderFragment>
    {
        BuildFieldSlotFragment(form, primaryField, livePatch, htmlIdGenerator, swapOob: false)
    };

    foreach (var affectedField in livePatch.AffectedFields.Where(field => !field.Equals(primaryField)))
    {
        fragments.Add(BuildFieldSlotFragment(form, affectedField, livePatch, htmlIdGenerator, swapOob: true));
    }

    foreach (var activationState in livePatch.LiveActivationStates)
    {
        fragments.Add(BuildLiveActivationSlotFragment(form, activationState.Key, activationState.Value, htmlIdGenerator, swapOob: true));
    }

    if (livePatch.ReplaceSummary)
    {
        fragments.Add(BuildSummarySlotFragment(form, livePatch, htmlIdGenerator, swapOob: true));
    }

    DemoInspectorUpdates.Queue(
        context,
        action: "validation-live",
        details: $"Live validation updated {string.Join(", ", livePatch.AffectedFields.Select(static field => field.Value))}.");

    return await HrzResults.Partial(context, cancellationToken, fragments.ToArray());
});

app.MapControllers();

static int CountErrors(HrzSubmitValidationState validationState)
{
    return validationState.SummaryErrors.Count
        + validationState.FieldErrors.Sum(static pair => pair.Value.Count);
}

static HrzFieldPath? ResolvePrimaryField(HrzValidationScope scope)
{
    return scope.ValidateAll
        ? UserInviteValidationForm.EmailPath
        : scope.Fields.FirstOrDefault(static field =>
            field.Equals(UserInviteValidationForm.EmailPath)
            || field.Equals(UserInviteValidationForm.DisplayNamePath));
}

static HrzLiveValidationPatch? BuildInviteLiveValidationPatch(HrzValidationScope scope, InviteUserInput input)
{
    var validatesInviteForm = scope.ValidateAll
        || scope.Fields.Contains(UserInviteValidationForm.EmailPath)
        || scope.Fields.Contains(UserInviteValidationForm.DisplayNamePath);
    if (!validatesInviteForm)
    {
        return null;
    }

    var email = input.Email?.Trim();
    var displayName = input.DisplayName?.Trim();
    if (string.IsNullOrWhiteSpace(email))
    {
        return null;
    }

    var requiresTeamDisplayName = string.Equals(email, "shared-mailbox@example.com", StringComparison.OrdinalIgnoreCase);
    var affectedFields = new[]
    {
        UserInviteValidationForm.EmailPath,
        UserInviteValidationForm.DisplayNamePath
    };
    var fieldErrors = new Dictionary<HrzFieldPath, IReadOnlyList<string>>();
    var liveActivationStates = new Dictionary<HrzFieldPath, bool>
    {
        [UserInviteValidationForm.EmailPath] = true,
        [UserInviteValidationForm.DisplayNamePath] = requiresTeamDisplayName
    };
    var summaryErrors = new List<string>();

    var emailErrors = string.Equals(email, "backend-taken@example.com", StringComparison.OrdinalIgnoreCase)
        ? new[] { "Email already exists in the upstream directory." }
        : Array.Empty<string>();
    fieldErrors[UserInviteValidationForm.EmailPath] = emailErrors;

    if (emailErrors.Length > 0)
    {
        summaryErrors.Add("Backend would reject this invite on submit.");
    }

    var displayNameErrors = requiresTeamDisplayName
        && (string.IsNullOrWhiteSpace(displayName)
            || !displayName.Contains("team", StringComparison.OrdinalIgnoreCase))
        ? new[] { "Shared mailbox invites must use a team display name." }
        : Array.Empty<string>();
    fieldErrors[UserInviteValidationForm.DisplayNamePath] = displayNameErrors;

    if (displayNameErrors.Length > 0)
    {
        summaryErrors.Add("Shared mailbox invites need a team display name before the backend will accept them.");
    }

    return new HrzLiveValidationPatch(
        scope.RootId,
        affectedFields,
        fieldErrors,
        liveActivationStates,
        ReplaceSummary: true,
        SummaryErrors: summaryErrors);
}

static RenderFragment BuildFieldSlotFragment(
    InviteValidationFormViewModel form,
    HrzFieldPath fieldPath,
    HrzLiveValidationPatch patch,
    IHrzHtmlIdGenerator htmlIdGenerator,
    bool swapOob)
{
    var slotId = $"{htmlIdGenerator.GetFieldMessageId(form.RootId.Value, fieldPath)}--server";
    var errors = patch.FieldErrors.TryGetValue(fieldPath, out var messages)
        ? messages
        : Array.Empty<string>();

    return builder =>
    {
        builder.OpenComponent<ValidationServerFieldSlot>(0);
        builder.AddAttribute(1, nameof(ValidationServerFieldSlot.Id), slotId);
        builder.AddAttribute(2, nameof(ValidationServerFieldSlot.FieldPath), fieldPath.Value);
        builder.AddAttribute(3, nameof(ValidationServerFieldSlot.Errors), errors);
        builder.AddAttribute(4, nameof(ValidationServerFieldSlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

static RenderFragment BuildSummarySlotFragment(
    InviteValidationFormViewModel form,
    HrzLiveValidationPatch patch,
    IHrzHtmlIdGenerator htmlIdGenerator,
    bool swapOob)
{
    return builder =>
    {
        builder.OpenComponent<ValidationServerSummarySlot>(0);
        builder.AddAttribute(1, nameof(ValidationServerSummarySlot.Id), htmlIdGenerator.GetSummaryId(form.RootId.Value));
        builder.AddAttribute(2, nameof(ValidationServerSummarySlot.Errors), patch.SummaryErrors);
        builder.AddAttribute(3, nameof(ValidationServerSummarySlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

static RenderFragment BuildLiveActivationSlotFragment(
    InviteValidationFormViewModel form,
    HrzFieldPath fieldPath,
    bool active,
    IHrzHtmlIdGenerator htmlIdGenerator,
    bool swapOob)
{
    var messageId = htmlIdGenerator.GetFieldMessageId(form.RootId.Value, fieldPath);
    var inputId = htmlIdGenerator.GetFieldId(form.RootId.Value, fieldPath);

    return builder =>
    {
        builder.OpenComponent<ValidationLiveFieldActivationSlot>(0);
        builder.AddAttribute(1, nameof(ValidationLiveFieldActivationSlot.Id), $"{messageId}--live-state");
        builder.AddAttribute(2, nameof(ValidationLiveFieldActivationSlot.InputId), inputId);
        builder.AddAttribute(3, nameof(ValidationLiveFieldActivationSlot.Active), active);
        builder.AddAttribute(4, nameof(ValidationLiveFieldActivationSlot.SwapOob), swapOob);
        builder.CloseComponent();
    };
}

app.Run();

public partial class Program;
