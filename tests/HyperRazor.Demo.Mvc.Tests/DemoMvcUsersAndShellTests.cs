using System.Net;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Mvc.Tests;

[Collection("DemoMvcWebAppFactoryCollection")]
public class DemoMvcUsersAndShellTests : DemoMvcIntegrationTestBase
{
    private const string CurrentLayoutHeader = "X-Hrz-Current-Layout";

    public DemoMvcUsersAndShellTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
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
        Assert.Contains("href=\"/demos/sse\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/sse/replay\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/notifications\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/access-requests\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/incidents\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/settings/branding\"", body, StringComparison.Ordinal);
        Assert.Contains("HX Request/Response Inspector", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.Boosted, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(CurrentLayoutHeader, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
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
    public async Task Users_WithBoostedSameLayoutRequest_ReturnsFragment()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(AdminLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Location));
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("<h2>User Administration</h2>", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithBoostedSameLayoutRequest_IncludesChromeOobUpdates()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(AdminLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"app-chrome-toolbar\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"app-chrome-sidebar\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", body, StringComparison.Ordinal);
        Assert.Contains("<code>/users</code>", body, StringComparison.Ordinal);
        Assert.Contains("<span class=\"sidebar-mode-value\">admin</span>", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/users\" aria-current=\"page\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithBoostedRequestAndMissingCurrentLayout_ReturnsHxLocation()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, body);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Location, out var locationValues));
        Assert.Contains("/users", locationValues.Single(), StringComparison.Ordinal);
        Assert.Contains("\"HX-Request-Type\":\"full\"", locationValues.Single(), StringComparison.Ordinal);
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
}
