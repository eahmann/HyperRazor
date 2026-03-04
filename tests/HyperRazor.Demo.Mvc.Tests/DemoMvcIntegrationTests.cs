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
    public async Task Feature_WithoutHxRequest_ReturnsFullPageWithConfigAndVary()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/feature");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<header id=\"app-shell\">", body, StringComparison.Ordinal);
        Assert.Contains("name=\"htmx-config\"", body, StringComparison.Ordinal);
        Assert.Contains("historyRestoreAsHxRequest", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature_WithHxRequest_ReturnsFragmentAndVary()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/feature");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Feature Fragment", body, StringComparison.Ordinal);
        Assert.DoesNotContain("app-shell", body, StringComparison.Ordinal);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Feature_WithHistoryRestoreRequest_ReturnsFullPage()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/feature");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add(HtmxHeaderNames.HistoryRestoreRequest, "true");

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("app-shell", body, StringComparison.Ordinal);
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
    public async Task RedirectEndpoints_UseHxHeadersWithout3xxResponses()
    {
        using var client = CreateClient();

        var softResponse = await client.PostAsync("/fragments/navigation/soft", content: null);
        Assert.Equal(HttpStatusCode.NoContent, softResponse.StatusCode);
        Assert.True(softResponse.Headers.TryGetValues(HtmxHeaderNames.Location, out var locationValues));
        Assert.Equal("/", locationValues.Single());

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
