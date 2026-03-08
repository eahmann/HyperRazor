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
    IAntiforgery antiforgery,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
        UserInviteValidationRoots.MinimalLocal,
        cancellationToken);

    if (!formPostState.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(formPostState.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(formPostState.ValidationState)
        });

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalInviteForm),
            UserInviteValidationDefinitions.MinimalLocal(formPostState.Model),
            cancellationToken);
    }

    var count = Random.Shared.Next(100, 200);
    context.HtmxResponse().Trigger("form:valid", new
    {
        name = formPostState.Model.DisplayName,
        email = formPostState.Model.Email,
        count
    });

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
            context,
            new
            {
                Form = UserInviteValidationDefinitions.MinimalLocal(formPostState.Model, success: true, count: count)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});
app.MapPost("/validation/minimal/proxy", async (
    HttpContext context,
    IAntiforgery antiforgery,
    IInviteValidationBackend inviteValidationBackend,
    CancellationToken cancellationToken) =>
{
    await antiforgery.ValidateRequestAsync(context);

    var formPostState = await context.BindFormAndValidateAsync<InviteUserInput>(
        UserInviteValidationRoots.MinimalProxy,
        cancellationToken);

    if (!formPostState.ValidationState.IsValid)
    {
        context.SetSubmitValidationState(formPostState.ValidationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(formPostState.ValidationState)
        });

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalProxyInviteForm),
            UserInviteValidationDefinitions.MinimalProxy(formPostState.Model),
            cancellationToken);
    }

    var backendResult = await inviteValidationBackend.SubmitAsync(formPostState.Model, cancellationToken);
    if (!backendResult.IsSuccess)
    {
        var resolver = context.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        var validationState = backendResult.ProblemDetails!.ToSubmitValidationState(
            UserInviteValidationRoots.MinimalProxy,
            resolver,
            formPostState.ValidationState.AttemptedValues);
        context.SetSubmitValidationState(validationState);
        context.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount = CountErrors(validationState)
        });

        return await UserInviteValidationResponses.RenderValidationAsync(
            context,
            nameof(ValidationPage.MinimalProxyInviteForm),
            UserInviteValidationDefinitions.MinimalProxy(formPostState.Model),
            cancellationToken);
    }

    context.HtmxResponse().Trigger("form:valid", new
    {
        name = formPostState.Model.DisplayName,
        email = formPostState.Model.Email,
        count = backendResult.Count
    });

    if (context.HtmxRequest().IsHtmx)
    {
        return await HrzResults.Partial<UserInviteValidationForm>(
            context,
            new
            {
                Form = UserInviteValidationDefinitions.MinimalProxy(
                    formPostState.Model,
                    success: true,
                    count: backendResult.Count)
            },
            cancellationToken: cancellationToken);
    }

    return Results.Redirect("/validation");
});

app.MapControllers();

static int CountErrors(HrzSubmitValidationState validationState)
{
    return validationState.SummaryErrors.Count
        + validationState.FieldErrors.Sum(static pair => pair.Value.Count);
}

app.Run();

public partial class Program;
