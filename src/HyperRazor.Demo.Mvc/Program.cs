using HyperRazor.Components;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Demo.Mvc.Components.Pages;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Hosting;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseHyperRazor();

// AdminLayout routes are intentionally served via Minimal API so the demo shows parity in a real app area.
app.MapGet("/", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<DashboardPage>(context, cancellationToken: cancellationToken));
app.MapGet("/users", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<UsersPage>(context, cancellationToken: cancellationToken));
app.MapGet("/settings/branding", (HttpContext context, CancellationToken cancellationToken) =>
    HrzResults.Page<BrandingSettingsPage>(context, cancellationToken: cancellationToken));

app.MapControllers();

app.Run();

public partial class Program;
