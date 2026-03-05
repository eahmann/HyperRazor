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
        Assert.Contains("href=\"/demos/errors\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/validation\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/oob\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/head\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/layout-swap\"", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.Boosted, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.LayoutFamily, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
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
    public async Task StatusDemo_ReturnsStatusHandlingScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/errors");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Status Handling</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/errors/401\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-get=\"/fragments/errors/500\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HeadDemo_ReturnsHeadHandlingScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/head");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Head Handling</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"head-demo-form\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/head/update\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayoutSwapDemo_ReturnsAlternateLayoutScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/layout-swap");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Layout Swap Demo</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"side-nav-layout-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/layout-swap/details\"", body, StringComparison.Ordinal);
        Assert.Contains("href=\"/demos/basic\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayoutSwapDetails_ReturnsAlternateLayoutDetailsScenario()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/demos/layout-swap/details");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Layout Swap Details</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"side-nav-layout-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
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
        Assert.Contains("allowNestedOobSwaps", body, StringComparison.Ordinal);
        Assert.Contains("<h2>OOB Swaps</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"user-count-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/fragments/users/create-rendered\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"render-to-string-demo-result\"", body, StringComparison.Ordinal);

        var metaMarker = "name=\"htmx-config\"";
        var metaStart = body.IndexOf(metaMarker, StringComparison.Ordinal);
        Assert.True(metaStart >= 0, "Expected htmx-config meta tag in rendered HTML.");
        var scriptIndex = body.IndexOf("src=\"/_content/HyperRazor.Client/vendor/htmx/htmx-2.0.4.min.js\"", StringComparison.Ordinal);
        Assert.True(scriptIndex >= 0 && metaStart < scriptIndex, "Expected htmx-config meta to render before htmx.js.");
        Assert.Contains("name=\"hrz-antiforgery\"", body, StringComparison.Ordinal);
        Assert.Contains("_content/HyperRazor.Client/hyperrazor.htmx.js", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
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
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.HistoryRestoreRequest, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LayoutSwap_WithHxRequest_ReturnsSideLayoutWithoutTopNav()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/layout-swap");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Layout Swap Demo</h2>", body, StringComparison.Ordinal);
        Assert.Contains("id=\"side-nav-layout-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-swap-oob", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayoutSwap_WithBoostedRequestAndMainFamily_PromotesToShellSwap()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/layout-swap");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "main");

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
        Assert.Contains("data-hrz-layout-family=\"side\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Basic_WithBoostedRequestAndMainFamily_DoesNotPromoteLayoutBoundary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/basic");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.Boosted, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "main");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Retarget));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reswap));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reselect));
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
        Assert.Contains("<h2>Server Trigger</h2>", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LayoutSwap_WithNonBoostedRequest_DoesNotPromoteLayoutBoundary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/layout-swap");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.LayoutFamily, "main");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Retarget));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reswap));
        Assert.False(response.Headers.Contains(HtmxHeaderNames.Reselect));
        Assert.DoesNotContain("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.Contains("id=\"side-nav-layout-shell\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Basic_WithHxRequest_ReturnsMainLayoutFragmentWithoutHeaderNav()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/demos/basic");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Server Trigger</h2>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"app-nav\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-swap-oob", body, StringComparison.Ordinal);
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
        Assert.Contains(HtmxHeaderNames.RequestType, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
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
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-validated");
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
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create");
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
        Assert.Contains("create-user", body, StringComparison.Ordinal);
        Assert.Contains("HX-Request", body, StringComparison.Ordinal);
        Assert.Contains("HX-Trigger", body, StringComparison.Ordinal);

        var oobCount = body.Split("hx-swap-oob=", StringSplitOptions.None).Length - 1;
        Assert.True(oobCount >= 4, $"Expected at least four OOB swaps, but found {oobCount}.");
    }

    [Fact]
    public async Task CreateUserRendered_WithHxRequest_ReturnsPreviewAndOobUpdatesFromStringRenderer()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-rendered");
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
        Assert.Contains("create-user-rendered", body, StringComparison.Ordinal);

        var oobCount = body.Split("hx-swap-oob=", StringSplitOptions.None).Length - 1;
        Assert.True(oobCount >= 5, $"Expected at least five OOB swaps, but found {oobCount}.");
    }

    [Fact]
    public async Task CreateUserValidated_WithHxRequest_InvalidInputReturnsErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-validated");
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
    public async Task CreateUserValidated_WithHxRequest_ValidInputReturnsSuccess()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create-validated");
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
    public async Task CreateUser_WithoutHxRequest_DoesNotRenderSwappableOobBlocks()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/users/create");
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

    [Fact]
    public async Task RedirectEndpoints_UseHxHeadersWithout3xxResponses()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var softRequest = new HttpRequestMessage(HttpMethod.Post, "/fragments/navigation/soft");
        softRequest.Headers.Add("RequestVerificationToken", antiforgeryToken);
        var softResponse = await client.SendAsync(softRequest);
        Assert.Equal(HttpStatusCode.NoContent, softResponse.StatusCode);
        Assert.True(softResponse.Headers.TryGetValues(HtmxHeaderNames.Location, out var locationValues));
        using (var locationDoc = JsonDocument.Parse(locationValues.Single()))
        {
            Assert.Equal("/", locationDoc.RootElement.GetProperty("path").GetString());
            Assert.Equal("#hrz-main-layout", locationDoc.RootElement.GetProperty("target").GetString());
            Assert.Equal("innerHTML", locationDoc.RootElement.GetProperty("swap").GetString());
        }

        using var hardRequest = new HttpRequestMessage(HttpMethod.Post, "/fragments/navigation/hard");
        hardRequest.Headers.Add("RequestVerificationToken", antiforgeryToken);
        var hardResponse = await client.SendAsync(hardRequest);
        Assert.Equal(HttpStatusCode.NoContent, hardResponse.StatusCode);
        Assert.True(hardResponse.Headers.TryGetValues(HtmxHeaderNames.Redirect, out var redirectValues));
        Assert.Equal("/", redirectValues.Single());
    }

    [Theory]
    [InlineData("/fragments/errors/401", HttpStatusCode.Unauthorized, "401 Unauthorized")]
    [InlineData("/fragments/errors/403", HttpStatusCode.Forbidden, "403 Forbidden")]
    [InlineData("/fragments/errors/404", HttpStatusCode.NotFound, "404 Not Found")]
    [InlineData("/fragments/errors/500", HttpStatusCode.InternalServerError, "500 Server Error")]
    public async Task StatusEndpoints_WithHxRequest_ReturnExpectedStatusAndFragment(
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
    public async Task HeadUpdate_WithHxRequest_ReturnsFragmentAndHeadPayload()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/fragments/head/update");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["title"] = "HyperRazor Head Test",
            ["description"] = "Head payload test."
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"head-demo-result\"", body, StringComparison.Ordinal);
        Assert.Contains("HyperRazor Head Test", body, StringComparison.Ordinal);
        Assert.Contains("<head", body, StringComparison.Ordinal);
        Assert.Contains("hx-head=\"merge\"", body, StringComparison.Ordinal);
        Assert.Contains("<title>HyperRazor Head Test</title>", body, StringComparison.Ordinal);
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
