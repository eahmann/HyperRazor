using System.Net;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Tests;

[Collection("WebAppFactoryCollection")]
public class PortalWorkflowTests : IntegrationTestBase
{
    public PortalWorkflowTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task PortalRoute_RendersPortalShell()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/portal");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-hrz-demo-shell=\"portal\"", body, StringComparison.Ordinal);
        Assert.Contains("Workspace access", body, StringComparison.Ordinal);
        Assert.Contains("aspnet-validation.min.js", body, StringComparison.Ordinal);
        Assert.Contains("hyperrazor.validation.js", body, StringComparison.Ordinal);
        Assert.Contains("data-val=\"true\"", body, StringComparison.Ordinal);
        Assert.Contains("data-val-required=", body, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hrz-demo-shell=\"console\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PortalEnter_WithInvalidHxRequest_RerendersFormAndPreservesValues()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/portal/enter");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "not-allowed@contoso.com",
            ["workspace"] = "atlas",
            ["accessCode"] = "WRONG26"
        });

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("validation-summary", body, StringComparison.Ordinal);
        Assert.Contains("value=\"not-allowed@contoso.com\"", body, StringComparison.Ordinal);
        Assert.Contains("Use an @example.com address for the demo portal.", body, StringComparison.Ordinal);
        Assert.Contains("Use the workspace access code for Atlas Finance.", body, StringComparison.Ordinal);
        Assert.DoesNotContain("data-hrz-demo-shell=", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PortalEnter_WithValidHxRequest_ReturnsHxLocationIntoUsers()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/portal/enter");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "operator@example.com",
            ["workspace"] = "atlas",
            ["accessCode"] = "ATLAS26"
        });

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, body);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.Location, out var values));
        Assert.Contains("/users?workspace=atlas", values.Single(), StringComparison.Ordinal);
        Assert.Contains("\"HX-Request-Type\":\"full\"", values.Single(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PortalEnter_WithValidStandardPost_ReturnsSeeOther()
    {
        using var client = CreateClient();
        var token = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/portal/enter");
        request.Headers.Add("RequestVerificationToken", token);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = "operator@example.com",
            ["workspace"] = "northstar",
            ["accessCode"] = "NSTAR26"
        });

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/users?workspace=northstar", response.Headers.Location?.ToString());
    }
}
