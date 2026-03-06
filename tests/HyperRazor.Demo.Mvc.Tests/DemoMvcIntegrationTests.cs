using System.Net;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

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
        Assert.Contains("hx-post=\"/fragments/users/invite\"", body, StringComparison.Ordinal);
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
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
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
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
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
        Assert.Contains("<header id=\"app-shell\">", body, StringComparison.Ordinal);
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
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.Contains("<h2>User Administration</h2>", body, StringComparison.Ordinal);
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
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
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
        Assert.Contains("<header id=\"app-shell\">", body, StringComparison.Ordinal);
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
    public async Task ValidateUserInvite_RequiresHtmxRequest()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "riley@example.com"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidateUserInvite_WithHxRequest_InvalidInputReturnsErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
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
        Assert.Contains("id=\"validated-user-result\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:invalid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidateUserInvite_WithHxRequest_ValidInputReturnsSuccess()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
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
        Assert.Contains("Validation Passed", body, StringComparison.Ordinal);
        Assert.Contains("Riley Stone", body, StringComparison.Ordinal);
        Assert.Contains("riley@example.com", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:valid", string.Join(',', values), StringComparison.Ordinal);
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

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
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
}
