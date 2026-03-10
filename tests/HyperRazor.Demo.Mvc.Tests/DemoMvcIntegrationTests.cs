using System.Net;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HyperRazor.Demo.Mvc.Tests;

public class DemoMvcIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DemoMvcIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_ReturnsOperationsShellAndWorkflowNavigation()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Operations Console", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/users\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/validation\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/access-requests\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/incidents\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/settings/branding\"", body, StringComparison.Ordinal);
        Assert.Contains("HX Request/Response Inspector", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.Boosted, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.LayoutFamily, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsersRoute_ReturnsAdminWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/users");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>User Administration</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"search-controls\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/users/provision\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/users/invite\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"users-invite\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationRoute_ReturnsDedicatedValidationHarness()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/validation");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Validation Paths</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/local\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-minimal-local\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-minimal-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-mixed-authoring\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mixed\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-live-policies\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-live\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-live-policies\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-email-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-policy-id=\"validation-minimal-proxy-email-live\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-disinherit=\"hx-disabled-elt\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"false\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-immediate-recheck-when-enabled=\"true\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrandingRoute_ReturnsBrandingWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/settings/branding");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Branding Settings</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"head-demo-form\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/settings/branding\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationRoute_ReturnsComparisonHarness()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/validation");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Validation Paths</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/local\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/proxy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessRequestsRoute_ReturnsWorkbenchWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/access-requests");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Access Requests</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"workbench-layout-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/access-requests/104/review\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewAccessRequestRoute_ReturnsTaskWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/access-requests/104/review");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h3>Access Review</h3>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"review-request-form\"", body, StringComparison.Ordinal);
        Assert.Contains("/fragments/access-requests/104/review", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IncidentsRoute_ReturnsWorkbenchWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/incidents");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Incidents</h2>", body, StringComparison.Ordinal);
        Assert.Contains("/fragments/incidents/drills/backend-failure", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/incidents/8801/triage\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IncidentTriageRoute_ReturnsTaskWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/incidents/8801/triage");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h3>Incident Triage</h3>", body, StringComparison.Ordinal);
        Assert.Contains("/fragments/incidents/drills/auth", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/incidents\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LegacyDemoRoutes_AreNotMapped()
    {
        using var client = CreateClient();

        var responses = await Task.WhenAll(
            client.GetAsync("/minimal"),
            client.GetAsync("/minimal/basic"),
            client.GetAsync("/demos/basic"),
            client.GetAsync("/demos/oob"),
            client.GetAsync("/demos/layout-swap"));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
    }

    [Fact]
    public async Task Users_WithHxRequest_ReturnsFragmentWithoutShell()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>User Administration</h2>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessRequests_WithHxRequest_ReturnsWorkbenchFragmentWithoutAppShell()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"workbench-layout-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessRequests_WithBoostedRequestAndAdminFamily_PromotesToShellSwap()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "admin");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Retarget, out var retargetValues));
        Assert.Equal("#hrz-app-shell", retargetValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reswap, out var reswapValues));
        Assert.Equal("outerHTML", reswapValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reselect, out var reselectValues));
        Assert.Equal("#hrz-app-shell", reselectValues.Single());
        Assert.Contains("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-layout-family=\"workbench\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewAccessRequest_WithBoostedRequestAndWorkbenchFamily_PromotesToShellSwap()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests/104/review");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "workbench");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains(HtmxHeaderNames.Retarget));
        Assert.True(response.Headers.Contains(HtmxHeaderNames.Reswap));
        Assert.True(response.Headers.Contains(HtmxHeaderNames.Reselect));
        Assert.Contains("data-hrz-layout-family=\"task\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithBoostedRequestAndAdminFamily_DoesNotPromoteLayoutBoundary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "admin");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Retarget));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reswap));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reselect));
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("<h2>User Administration</h2>", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithBoostedRequestAndAdminFamily_IncludesChromeOobUpdates()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "admin");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"app-chrome-toolbar\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"app-chrome-sidebar\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body, StringComparison.Ordinal);
        Assert.Contains("<code>/users</code>", body, StringComparison.Ordinal);
        Assert.Contains("<span class=\"sidebar-mode-value\">admin</span>", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/users\" aria-current=\"page\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccessRequests_WithNonBoostedRequest_DoesNotPromoteLayoutBoundary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "admin");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Retarget));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reswap));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reselect));
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"workbench-layout-shell\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithHistoryRestoreRequest_ReturnsFullPage()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.HistoryRestoreRequest, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardSyncCheck_SetsHxTriggerHeader()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/fragments/dashboard/sync-check");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Saved successfully", body, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("toast:show", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DashboardBannerCheck_WithHxRequest_AlsoIncludesInspectorOobUpdate()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fragments/dashboard/banner-check");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Saved successfully (attribute trigger).", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("dashboard-banner-check", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("toast:show", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchUsers_RequiresHtmxRequest()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/fragments/users/search?query=alex");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_WithHxRequest_ReturnsPartialHtml()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fragments/users/search?query=alex");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Alex Smith", body, StringComparison.Ordinal);
        Assert.Contains("data-search-results=\"true\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("search-users", body, StringComparison.Ordinal);
        Assert.DoesNotContain("app-shell", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchUsers_WithSortAndPage_RendersPagerAndSortMetadata()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/fragments/users/search?query=a&sort=username-desc&page=2&pageSize=2");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Page 2 of", body, StringComparison.Ordinal);
        Assert.Contains("Sort: <code>username-desc</code>", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/users/search?", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProvisionUser_WithHxRequest_ReturnsMainFragmentAndOobUpdates()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/provision");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Jordan Avery"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"users-list\"", body, StringComparison.Ordinal);
        Assert.Contains("beforeend:#toast-stack", body, StringComparison.Ordinal);
        Assert.Contains("id=\"user-count-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", body, StringComparison.Ordinal);
        Assert.Contains("beforeend:#activity-feed", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-provision", body, StringComparison.Ordinal);

        var oobCount = body.Split("hx-swap-oob=", StringSplitOptions.None).Length - 1;
        Assert.True(oobCount >= 4, $"Expected at least four OOB swaps, but found {oobCount}.");
    }

    [Fact]
    public async Task ProvisionUserRendered_WithHxRequest_ReturnsPreviewAndOobUpdatesFromStringRenderer()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/provision-rendered");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Casey Quinn"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"render-to-string-demo-result\"", body, StringComparison.Ordinal);
        Assert.Contains("IHrzSwapService.RenderToString(clear: true)", body, StringComparison.Ordinal);
        Assert.Contains("id=\"users-list\"", body, StringComparison.Ordinal);
        Assert.Contains("beforeend:#toast-stack", body, StringComparison.Ordinal);
        Assert.Contains("id=\"user-count-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("beforeend:#activity-feed", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-provision-rendered", body, StringComparison.Ordinal);

        var oobCount = body.Split("hx-swap-oob=", StringSplitOptions.None).Length - 1;
        Assert.True(oobCount >= 5, $"Expected at least five OOB swaps, but found {oobCount}.");
    }

    [Fact]
    public async Task InviteUser_WithoutAntiforgery_IsRejected()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InviteUser_InvalidNormalPost_RerendersFullPageWithAttemptedValuesAndErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"invalid\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteUser_WithHxRequest_InvalidInputReturnsFormFragmentErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-validation-for=\"DisplayName\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-validation-for=\"Email\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"invalid\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-invite-invalid", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:invalid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteUser_WithHxRequest_ValidInputReturnsSuccess()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "riley@example.com"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Created <strong>Riley Stone</strong>", body, StringComparison.Ordinal);
        Assert.Contains("Riley Stone", body, StringComparison.Ordinal);
        Assert.Contains("riley@example.com", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-invite-valid", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:valid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteProxy_LocalInvalid_DoesNotCallBackend()
    {
        var backend = new SpyInviteValidationBackend();
        using var client = CreateClient(services =>
        {
            services.RemoveAll<IInviteValidationBackend>();
            services.AddSingleton<IInviteValidationBackend>(backend);
        });

        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mvc-proxy");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, backend.InvocationCount);
        Assert.Contains("id=\"validation-mvc-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-mvc-proxy-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteProxy_WithHxRequest_BackendValidationReturnsMappedErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mvc-proxy");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "backend-taken@example.com"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mvc-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Upstream directory rejected the invite.", body, StringComparison.Ordinal);
        Assert.Contains("Email already exists in the upstream directory.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"backend-taken@example.com\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-mvc-proxy-backend-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MinimalInvite_WithHxRequest_LocalInvalidRerendersHtml()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/minimal/local");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-local-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-minimal-local-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MinimalInviteProxy_WithHxRequest_BackendValidationReturnsMappedErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/minimal/proxy");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "backend-taken@example.com"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Upstream directory rejected the invite.", body, StringComparison.Ordinal);
        Assert.Contains("Email already exists in the upstream directory.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"backend-taken@example.com\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-minimal-proxy-backend-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_WithReservedEmail_ReturnsTargetedServerSlotAndSummaryOob()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "backend-taken@example.com",
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.EmailPath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-email-server\"", body, StringComparison.Ordinal);
        Assert.Contains("Email already exists in the upstream directory.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-server-summary\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-live", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"validation-minimal-proxy-form-shell\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MixedValidation_WithInvalidInput_RerendersMixedFormErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mixed");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["environment"] = "production",
            ["seatCount"] = "0",
            ["notes"] = "short"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mixed-authoring-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Seat count must be between 1 and 50.", body, StringComparison.Ordinal);
        Assert.Contains("Notes must be at least 10 characters.", body, StringComparison.Ordinal);
        Assert.Contains("validation-mixed-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MixedLiveValidation_WithProductionOverage_ReturnsDependentSeatCountOob()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mixed/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["environment"] = "production",
            ["seatCount"] = "20",
            ["notes"] = "Requesting production rollout for the release window.",
            ["__hrz_root"] = UserInviteValidationRoots.MixedAuthoring.Value,
            ["__hrz_fields"] = MixedValidationAuthoringForm.EnvironmentPath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mixed-authoring-environment-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-live\"", body, StringComparison.Ordinal);
        Assert.Contains("Production rollouts above 10 seats require approval.", body, StringComparison.Ordinal);
        Assert.Contains("Approval is required before a production rollout can exceed 10 seats.", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MixedLiveValidation_WithApprovalChecked_ClearsDependentSeatCount()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mixed/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("environment", "production"),
            new KeyValuePair<string, string>("requiresApproval", "true"),
            new KeyValuePair<string, string>("requiresApproval", "false"),
            new KeyValuePair<string, string>("seatCount", "20"),
            new KeyValuePair<string, string>("notes", "Requesting production rollout for the release window."),
            new KeyValuePair<string, string>("__hrz_root", UserInviteValidationRoots.MixedAuthoring.Value),
            new KeyValuePair<string, string>("__hrz_fields", MixedValidationAuthoringForm.RequiresApprovalPath.Value)
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mixed-authoring-requires-approval-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-live\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Production rollouts above 10 seats require approval.", body, StringComparison.Ordinal);
        Assert.Contains("validation-summary--empty", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_WithMissingEmail_ReturnsClearFragments()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = string.Empty,
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.EmailPath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-email-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"false\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-server-summary\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_WithSharedMailboxEmail_ReturnsDependentDisplayNameOob()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "shared-mailbox@example.com",
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.EmailPath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-email-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-immediate-recheck-when-enabled=\"true\"", body, StringComparison.Ordinal);
        Assert.Contains("Shared mailbox invites must use a team display name.", body, StringComparison.Ordinal);
        Assert.Contains("Shared mailbox invites need a team display name before the backend will accept them.", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"validation-minimal-proxy-form-shell\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_WithMissingDisplayNameDependency_ReturnsCarrierUpdateAndClearFragments()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = string.Empty,
            ["email"] = "shared-mailbox@example.com",
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.EmailPath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-email-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"true\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Shared mailbox invites must use a team display name.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_DisplayNameScope_WhenPolicyIsDisabled_ReturnsClearsAndCarrierOob()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Team Inbox",
            ["email"] = "riley@example.com",
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.DisplayNamePath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"false\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-server-summary\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-live-policy-disabled", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveValidation_DisplayNameScope_ClearsDependentMessageWhenRuleIsSatisfied()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/live");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Team Inbox",
            ["email"] = "shared-mailbox@example.com",
            ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
            ["__hrz_fields"] = UserInviteValidationForm.DisplayNamePath.Value
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-server-summary\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-summary--empty", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Shared mailbox invites must use a team display name.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Submit_InvalidAfterLiveValidationRoundTrip_StillRerendersFormErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using (var liveRequest = new HttpRequestMessage(HttpMethod.Post, "/validation/live"))
        {
            liveRequest.Headers.Add(HtmxHeaderNames.Request, "true");
            liveRequest.Headers.Add("RequestVerificationToken", antiforgeryToken);
            liveRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["displayName"] = "Riley Stone",
                ["email"] = "backend-taken@example.com",
                ["__hrz_root"] = UserInviteValidationRoots.MinimalProxy.Value,
                ["__hrz_fields"] = UserInviteValidationForm.EmailPath.Value
            });

            using var liveResponse = await client.SendAsync(liveRequest);
            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        }

        using var submitRequest = new HttpRequestMessage(HttpMethod.Post, "/validation/minimal/proxy");
        submitRequest.Headers.Add(HtmxHeaderNames.Request, "true");
        submitRequest.Headers.Add("RequestVerificationToken", antiforgeryToken);
        submitRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var submitResponse = await client.SendAsync(submitRequest);
        var submitBody = await submitResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, submitResponse.StatusCode);
        Assert.Contains("Display name must be at least 3 characters.", submitBody, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", submitBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProvisionUser_WithoutHxRequest_DoesNotRenderSwappableOobBlocks()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/provision");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Jordan Avery"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"users-list\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-swap-oob", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/fragments/incidents/drills/auth", HttpStatusCode.Unauthorized, "401 Unauthorized")]
    [InlineData("/fragments/incidents/drills/permission", HttpStatusCode.Forbidden, "403 Forbidden")]
    [InlineData("/fragments/incidents/drills/playbook-missing", HttpStatusCode.NotFound, "404 Not Found")]
    [InlineData("/fragments/incidents/drills/backend-failure", HttpStatusCode.InternalServerError, "500 Server Error")]
    public async Task IncidentDrillEndpoints_WithHxRequest_ReturnExpectedStatusAndFragment(
        string route,
        HttpStatusCode expectedStatusCode,
        string expectedHeading)
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Contains("id=\"status-result\"", body, StringComparison.Ordinal);
        Assert.Contains(expectedHeading, body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BrandingUpdate_WithHxRequest_ReturnsFragmentAndHeadPayload()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/settings/branding");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["title"] = "Operations Console • Branding Test",
            ["description"] = "Branding payload test.",
            ["accent"] = "rose"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"head-demo-result\"", body, StringComparison.Ordinal);
        Assert.Contains("Title queued:", body, StringComparison.Ordinal);
        Assert.Contains("Branding payload test.", body, StringComparison.Ordinal);
        Assert.Contains("<head", body, StringComparison.Ordinal);
        Assert.Contains("hx-head=\"merge\"", body, StringComparison.Ordinal);
        Assert.Contains("Branding Test</title>", body, StringComparison.Ordinal);
        Assert.Contains("<meta name=\"description\" content=\"Branding payload test.\"", body, StringComparison.Ordinal);
        Assert.Contains("<style>", body, StringComparison.Ordinal);
        Assert.Contains("#head-demo-result .head-demo-style-preview", body, StringComparison.Ordinal);
        Assert.Contains("<script src=\"/head-demo.asset.js\" defer></script>", body, StringComparison.Ordinal);
        Assert.Contains("Accent preset:</strong> Rose", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThemeToggle_WithHxRequest_SetsCookieAndRefreshes()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/chrome/theme");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["theme"] = "light",
            ["returnUrl"] = "/users"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Refresh, out var refreshValues));
        Assert.Equal("true", refreshValues.Single());
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));

        var themeCookie = setCookieValues.Single(value => value.Contains("hrz-demo-theme=light", StringComparison.Ordinal));
        using var themedRequest = new HttpRequestMessage(HttpMethod.Get, "/users");
        themedRequest.Headers.Add("Cookie", themeCookie.Split(';', 2)[0]);

        var themedResponse = await client.SendAsync(themedRequest);
        var themedBody = await themedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, themedResponse.StatusCode);
        Assert.Contains("/vendor/bootswatch/flatly.min.css", themedBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewAccessRequest_WithInvalidInput_ReturnsValidationFragment()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/access-requests/104/review");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ticketId"] = "SEC",
            ["justification"] = "short"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Review Validation Errors", body, StringComparison.Ordinal);
        Assert.Contains("Ticket ID must include the system prefix and numeric identifier.", body, StringComparison.Ordinal);
        Assert.Contains("Justification must be at least 12 characters.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewAccessRequest_WithValidInput_ReturnsHxLocation()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/access-requests/104/review");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ticketId"] = "SEC-104",
            ["justification"] = "Approve temporary billing export access for the planned change window."
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Location, out var locationValues));
        Assert.Contains("/access-requests?completed=104", locationValues.Single(), StringComparison.Ordinal);
    }

    private HttpClient CreateClient(Action<IServiceCollection>? configureServices = null)
    {
        var factory = configureServices is null
            ? _factory
            : _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(configureServices);
            });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractMetaContent(html, "hrz-antiforgery");
    }

    private static string ExtractMetaContent(string html, string metaName)
    {
        var marker = $"<meta name=\"{metaName}\" content=\"";
        var start = html.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected meta tag '{metaName}' in response HTML.");

        start += marker.Length;
        var end = html.IndexOf('"', start);
        Assert.True(end > start, $"Expected meta tag '{metaName}' content value.");

        return html[start..end];
    }

    private sealed class SpyInviteValidationBackend : IInviteValidationBackend
    {
        public int InvocationCount { get; private set; }

        public void Reset()
        {
            InvocationCount = 0;
        }

        public Task<InviteValidationBackendResult> SubmitAsync(HyperRazor.Demo.Mvc.Models.InviteUserInput input, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(new InviteValidationBackendResult(true, 999, null));
        }
    }
}
