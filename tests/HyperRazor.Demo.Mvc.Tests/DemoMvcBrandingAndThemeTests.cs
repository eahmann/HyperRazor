using System.Net;
using System.Text.Json;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Mvc.Tests;

public class DemoMvcBrandingAndThemeTests : DemoMvcIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public DemoMvcBrandingAndThemeTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
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
    public async Task ThemeToggle_WithHxRequest_SetsCookieAndReturnsThemeTrigger()
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
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Refresh));
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var triggerValues));
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues));

        using var triggerDocument = JsonDocument.Parse(triggerValues.Single());
        var themeTrigger = triggerDocument.RootElement.GetProperty("chrome:theme-updated");
        Assert.Equal("light", themeTrigger.GetProperty("theme").GetString());
        Assert.Equal(DemoChromeState.ThemeStylesheetHref, themeTrigger.GetProperty("href").GetString());

        var themeCookie = setCookieValues.Single(value => value.Contains("hrz-demo-theme=light", StringComparison.Ordinal));
        using var themedRequest = new HttpRequestMessage(HttpMethod.Get, "/users");
        themedRequest.Headers.Add("Cookie", themeCookie.Split(';', 2)[0]);

        var themedResponse = await client.SendAsync(themedRequest);
        var themedBody = await themedResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, themedResponse.StatusCode);
        Assert.Contains(DemoChromeState.ThemeStylesheetHref, themedBody, StringComparison.Ordinal);
        Assert.Contains("data-bs-theme=\"light\"", themedBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UsersRoute_WithDarkThemeCookie_UsesSharedStylesheetAndDarkMode()
    {
        using var client = CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Add("Cookie", $"{DemoChromeState.ThemeCookieName}=dark");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(DemoChromeState.ThemeStylesheetHref, body, StringComparison.Ordinal);
        Assert.Contains("data-bs-theme=\"dark\"", body, StringComparison.Ordinal);
    }
}
