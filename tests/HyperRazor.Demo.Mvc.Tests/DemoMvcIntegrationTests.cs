using System.Net;
using System.Text.Json;
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
    public async Task Home_ReturnsNavigationAndInteractiveSections()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Server Trigger", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/basic\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/search\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/redirects\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/validation\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/oob\"", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BasicDemo_ReturnsServerTriggerExperience()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/basic");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Server Trigger</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/toast/success\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/toast/success-attribute\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"server-trigger-demo\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchDemo_ReturnsLiveSearchScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/search");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Live Search</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"search-controls\"", body, StringComparison.Ordinal);
        Assert.Contains("name=\"sort\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/users/search\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RedirectDemo_ReturnsRedirectScenarios()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/redirects");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Redirect Headers</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/navigation/soft\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/navigation/hard\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationDemo_ReturnsValidationScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/validation");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Form Validation</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-form\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/users/create-validated\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Feature_WithoutHxRequest_ReturnsFullPageWithConfigAndVary()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/oob");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.Contains("name=\"htmx-config\"", body, StringComparison.Ordinal);
        Assert.Contains("historyRestoreAsHxRequest", body, StringComparison.Ordinal);
        Assert.Contains("<h2>OOB Swaps</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"user-count-shell\"", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature_WithHxRequest_ReturnsFragmentAndVary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/oob");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>OOB Swaps</h2>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("app-shell", body, StringComparison.Ordinal);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature_WithHistoryRestoreRequest_ReturnsFullPage()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/oob");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.HistoryRestoreRequest, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("app-shell", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LegacyLevelRoutes_AreNotMapped()
    {
        using var client = CreateClient();

        var intermediate = await client.GetAsync("/demos/intermediate");
        var advanced = await client.GetAsync("/demos/advanced");
        var feature = await client.GetAsync("/feature");

        Assert.Equal(HttpStatusCode.NotFound, intermediate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, advanced.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, feature.StatusCode);
    }

    [Fact]
    public async Task ToastSuccess_SetsHxTriggerHeader()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/fragments/toast/success");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("toast", body, StringComparison.OrdinalIgnoreCase);
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
    public async Task CreateUserValidated_RequiresHtmxRequest()
    {
        using var client = CreateClient();

        var response = await client.PostAsync("/fragments/users/create-validated",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["displayName"] = "Riley Stone",
                ["email"] = "riley@example.com"
            }));

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
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
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
    public async Task ToastSuccess_WithHxRequest_AlsoIncludesInspectorOobUpdate()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fragments/toast/success");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("toast", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("toast-success", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("toast:show", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToastSuccessAttribute_WithHxRequest_AlsoIncludesInspectorOobUpdate()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/fragments/toast/success-attribute");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Saved successfully (attribute trigger).", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("toast-success-attribute", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("toast:show", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateUser_WithHxRequest_ReturnsMainFragmentAndOobUpdates()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
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
        Assert.Contains("beforeend:#activity-feed", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("create-user", body, StringComparison.Ordinal);
        Assert.Contains("HX-Request", body, StringComparison.Ordinal);
        Assert.Contains("HX-Trigger", body, StringComparison.Ordinal);

        var oobCount = body.Split("hx-swap-oob=", StringSplitOptions.None).Length - 1;
        Assert.True(oobCount >= 4, $"Expected at least four OOB swaps, but found {oobCount}.");
    }

    [Fact]
    public async Task CreateUserValidated_WithHxRequest_InvalidInputReturns422AndErrors()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-validated");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains("id=\"validated-user-result\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:invalid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateUserValidated_WithHxRequest_ValidInputReturnsSuccess()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-validated");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
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
    public async Task CreateUser_WithoutHxRequest_DoesNotRenderSwappableOobBlocks()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create");
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

    [Fact]
    public async Task RedirectEndpoints_UseHxHeadersWithout3xxResponses()
    {
        using var client = CreateClient();

        var softResponse = await client.PostAsync("/fragments/navigation/soft", content: null);
        Assert.Equal(HttpStatusCode.NoContent, softResponse.StatusCode);
        Assert.True(softResponse.Headers.TryGetValues(HtmxHeaderNames.Location, out var locationValues));
        using (var locationDoc = JsonDocument.Parse(locationValues.Single()))
        {
            Assert.Equal("/", locationDoc.RootElement.GetProperty("path").GetString());
            Assert.Equal("#hrx-main-layout", locationDoc.RootElement.GetProperty("target").GetString());
            Assert.Equal("innerHTML", locationDoc.RootElement.GetProperty("swap").GetString());
        }

        var hardResponse = await client.PostAsync("/fragments/navigation/hard", content: null);
        Assert.Equal(HttpStatusCode.NoContent, hardResponse.StatusCode);
        Assert.True(hardResponse.Headers.TryGetValues(HtmxHeaderNames.Redirect, out var redirectValues));
        Assert.Equal("/", redirectValues.Single());
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
