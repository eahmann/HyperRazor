using System.Net;
using HyperRazor.Demo.Mvc.Components.Layouts;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Mvc.Tests;

[Collection("DemoMvcWebAppFactoryCollection")]
public class DemoMvcWorkbenchTests : DemoMvcIntegrationTestBase
{
    private const string CurrentLayoutHeader = "X-Hrz-Current-Layout";

    public DemoMvcWorkbenchTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
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
    public async Task AccessRequests_WithBoostedCrossLayoutRequest_ReturnsRootSwap()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(AdminLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"hrz-app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Access Requests</h2>", body, StringComparison.Ordinal);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Location));
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Retarget, out var retargetValues));
        Assert.Equal("#hrz-app-shell", retargetValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reswap, out var reswapValues));
        Assert.Equal("outerHTML", reswapValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reselect, out var reselectValues));
        Assert.Equal("#hrz-app-shell", reselectValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.PushUrl, out var pushUrlValues));
        Assert.Equal("true", pushUrlValues.Single());
    }

    [Fact]
    public async Task ReviewAccessRequest_WithBoostedCrossLayoutRequest_ReturnsRootSwap()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/access-requests/104/review");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(WorkbenchLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"hrz-app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("<h3>Access Review</h3>", body, StringComparison.Ordinal);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Location));
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Retarget, out var retargetValues));
        Assert.Equal("#hrz-app-shell", retargetValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reswap, out var reswapValues));
        Assert.Equal("outerHTML", reswapValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Reselect, out var reselectValues));
        Assert.Equal("#hrz-app-shell", reselectValues.Single());
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.PushUrl, out var pushUrlValues));
        Assert.Equal("true", pushUrlValues.Single());
    }

    [Fact]
    public async Task Incidents_WithBoostedSameLayoutRequest_RemainsFragmentFirst()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/incidents");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(WorkbenchLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Location));
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Incidents</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"workbench-layout-shell\"", body, StringComparison.Ordinal);
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
}
