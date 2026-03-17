using System.Net;
using HyperRazor.Demo.Components.Layouts;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Tests;

[Collection("WebAppFactoryCollection")]
public class UsersWorkflowTests : IntegrationTestBase
{
    private const string CurrentLayoutHeader = "X-Hrz-Current-Layout";

    public UsersWorkflowTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task UsersRoute_RendersConsoleShellAndWorkflow()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/users?workspace=atlas");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-hrz-demo-shell=\"console\"", body, StringComparison.Ordinal);
        Assert.Contains("Invite and provision", body, StringComparison.Ordinal);
        Assert.Contains("id=\"users-invite-region\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"directory-results\"", body, StringComparison.Ordinal);
        Assert.Contains("data-val=\"true\"", body, StringComparison.Ordinal);
        Assert.Contains("data-val-required=", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Users_WithBoostedPortalLayoutRequest_ReturnsRootSwapHeaders()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users?workspace=atlas");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(PortalLayout).FullName!);

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-hrz-demo-shell=\"console\"", body, StringComparison.Ordinal);
        Assert.Equal("#hrz-app-shell", response.Headers.GetValues(HtmxHeaderNames.Retarget).Single());
        Assert.Equal("outerHTML", response.Headers.GetValues(HtmxHeaderNames.Reswap).Single());
        Assert.Equal("#hrz-app-shell", response.Headers.GetValues(HtmxHeaderNames.Reselect).Single());
    }

    [Fact]
    public async Task Branding_WithBoostedSameLayoutRequest_UpdatesNavOobState()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/settings/branding?workspace=atlas");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(CurrentLayoutHeader, typeof(ConsoleLayout).FullName!);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Branding and environment markers", body, StringComparison.Ordinal);
        Assert.Contains("id=\"console-nav-region\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/settings/branding?workspace=atlas\" aria-current=\"page\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invite_WithInvalidHxRequest_ReturnsFormErrors()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/users?workspace=atlas");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workspace"] = "atlas",
            ["displayName"] = "Taylor Reed",
            ["email"] = "taylor@contoso.com",
            ["team"] = "Platform",
            ["accessTier"] = "Privileged",
            ["manager"] = "Taylor Reed",
            ["startDate"] = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)).ToString("yyyy-MM-dd"),
            ["justification"] = "short"
        });

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("validation-summary", body, StringComparison.Ordinal);
        Assert.Contains("Use an @example.com address for the internal demo directory.", body, StringComparison.Ordinal);
        Assert.Contains("Manager approval must come from someone else.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("invite-provision-shell", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invite_WithValidHxRequest_ReturnsProvisioningShellAndStatusOob()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/users?workspace=atlas");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workspace"] = "atlas",
            ["displayName"] = "Casey Quinn",
            ["email"] = "casey.quinn@example.com",
            ["team"] = "Finance Ops",
            ["accessTier"] = "Analyst",
            ["manager"] = "Priya Shah",
            ["startDate"] = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd"),
            ["justification"] = "Needs access to reconcile the vendor review queue this week."
        });

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"invite-provision-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("sse-connect=\"/streams/users/provision/op-", body, StringComparison.Ordinal);
        Assert.Contains("id=\"users-status-region\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"innerHTML\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invite_WithValidStandardPost_RedirectsToProvisioningView()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/users?workspace=atlas");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workspace"] = "atlas",
            ["displayName"] = "Casey Quinn",
            ["email"] = "casey.quinn@example.com",
            ["team"] = "Finance Ops",
            ["accessTier"] = "Analyst",
            ["manager"] = "Priya Shah",
            ["startDate"] = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd"),
            ["justification"] = "Needs access to reconcile the vendor review queue this week."
        });

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.StartsWith("/users?workspace=atlas&operation=op-", response.Headers.Location!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProvisioningStream_ReturnsHtmlMessagesOobUpdatesAndDone()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client, "/users?workspace=atlas");
        using var invite = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/invite");
        invite.Headers.Add("RequestVerificationToken", token);
        invite.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["workspace"] = "atlas",
            ["displayName"] = "Jordan Avery",
            ["email"] = "jordan.avery@example.com",
            ["team"] = "Finance Ops",
            ["accessTier"] = "Analyst",
            ["manager"] = "Priya Shah",
            ["startDate"] = DateOnly.FromDateTime(DateTime.Today.AddDays(1)).ToString("yyyy-MM-dd"),
            ["justification"] = "Needs access to reconcile the vendor review queue this week."
        });

        using var inviteResponse = await client.SendAsync(invite);
        var operationId = inviteResponse.Headers.Location!.ToString().Split("operation=", StringSplitOptions.None)[1];

        using var response = await client.GetAsync($"/streams/users/provision/{operationId}", HttpCompletionOption.ResponseHeadersRead);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var firstBlock = await ReadEventBlockAsync(reader);
        Assert.Contains("Directory entry reserved", firstBlock, StringComparison.Ordinal);
        Assert.Contains("users-status-region", firstBlock, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=", firstBlock, StringComparison.Ordinal);

        string block = string.Empty;
        for (var index = 0; index < 5; index++)
        {
            block = await ReadEventBlockAsync(reader);
        }

        Assert.Contains("Provisioning complete", block, StringComparison.Ordinal);
        Assert.Contains("users-count-region", block, StringComparison.Ordinal);
        Assert.Contains("users-activity-region", block, StringComparison.Ordinal);

        var doneBlock = await ReadEventBlockAsync(reader);
        Assert.Contains("event: done", doneBlock, StringComparison.Ordinal);
        Assert.Contains("data:", doneBlock, StringComparison.Ordinal);
    }
}
