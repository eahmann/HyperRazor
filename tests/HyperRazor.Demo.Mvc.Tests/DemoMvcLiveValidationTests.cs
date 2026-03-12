using System.Net;
using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Mvc.Tests;

public class DemoMvcLiveValidationTests : DemoMvcIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public DemoMvcLiveValidationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
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
}
