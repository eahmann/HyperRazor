using System.Net;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HyperRazor.Demo.Mvc.Tests;

public class DemoMvcValidationTests : DemoMvcIntegrationTestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public DemoMvcValidationTests(WebApplicationFactory<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task ValidationRoute_ReturnsDedicatedValidationHarness()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/validation");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Validation Paths</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/local\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-minimal-local\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-minimal-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-validation-root=\"validation-mixed-authoring\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mixed\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-live-policies\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-mixed-authoring-seat-count-live\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-live-policies\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-display-name-live\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-minimal-proxy-email-live\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-policy-id=\"validation-minimal-proxy-email-live\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-disinherit=\"*\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-disabled-elt=\"find button\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("hx-disabled-elt=\"find button\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-live-enabled=\"false\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-immediate-recheck-when-enabled=\"true\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationRoute_ReturnsComparisonHarness()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/validation");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2>Validation Paths</h2>", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/mvc-proxy\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/local\"", body, StringComparison.Ordinal);
        Assert.Contains("hx-post=\"/validation/minimal/proxy\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteUser_WithoutAntiforgery_IsRejected()
    {
        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InviteUser_InvalidNormalPost_RerendersFullPageWithAttemptedValuesAndErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "A",
            ["email"] = "invalid"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"invalid\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteUser_WithHxRequest_InvalidInputReturnsFormFragmentErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
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
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<header id=\"app-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-validation-for=\"DisplayName\"", body, StringComparison.Ordinal);
        Assert.Contains("data-hrz-server-validation-for=\"Email\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("value=\"invalid\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-invite-invalid", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:invalid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteUser_WithHxRequest_ValidInputReturnsSuccess()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/users/invite");
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
        Assert.Contains("id=\"validation-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Created <strong>Riley Stone</strong>", body, StringComparison.Ordinal);
        Assert.Contains("Riley Stone", body, StringComparison.Ordinal);
        Assert.Contains("riley@example.com", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("users-invite-valid", body, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues(HtmxHeaderNames.TriggerResponse, out var values));
        Assert.Contains("form:valid", string.Join(',', values), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteProxy_LocalInvalid_DoesNotCallBackend()
    {
        var backend = new SpyInviteValidationBackend();
        using var client = CreateClient(services =>
        {
            services.RemoveAll<IInviteValidationBackend>();
            services.AddSingleton<IInviteValidationBackend>(backend);
        });

        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mvc-proxy");
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
        Assert.Equal(0, backend.InvocationCount);
        Assert.Contains("id=\"validation-mvc-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-mvc-proxy-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InviteProxy_WithHxRequest_BackendValidationReturnsMappedErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mvc-proxy");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "backend-taken@example.com"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mvc-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Upstream directory rejected the invite.", body, StringComparison.Ordinal);
        Assert.Contains("Email already exists in the upstream directory.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"backend-taken@example.com\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-mvc-proxy-backend-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MinimalInvite_WithHxRequest_LocalInvalidRerendersHtml()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/minimal/local");
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
        Assert.Contains("id=\"validation-minimal-local-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", body, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"A\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-minimal-local-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MinimalInviteProxy_WithHxRequest_BackendValidationReturnsMappedErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/minimal/proxy");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "Riley Stone",
            ["email"] = "backend-taken@example.com"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-minimal-proxy-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Upstream directory rejected the invite.", body, StringComparison.Ordinal);
        Assert.Contains("Email already exists in the upstream directory.", body, StringComparison.Ordinal);
        Assert.Contains("value=\"backend-taken@example.com\"", body, StringComparison.Ordinal);
        Assert.Contains("id=\"hx-debug-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("validation-minimal-proxy-backend-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MixedValidation_WithInvalidInput_RerendersMixedFormErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mixed");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["environment"] = "production",
            ["seatCount"] = "0",
            ["notes"] = "short"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mixed-authoring-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Seat count must be between 1 and 50.", body, StringComparison.Ordinal);
        Assert.Contains("Notes must be at least 10 characters.", body, StringComparison.Ordinal);
        Assert.Contains("validation-mixed-invalid", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MixedValidation_SubmitPreservesLiveRuleErrorsAlongsideSubmitErrors()
    {
        using var client = CreateClient();
        var antiforgeryToken = await GetAntiforgeryTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/validation/mixed");
        request.Headers.Add(HtmxHeaderNames.Request, "true");
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["environment"] = "production",
            ["seatCount"] = "18",
            ["notes"] = "short"
        });

        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("id=\"validation-mixed-authoring-form-shell\"", body, StringComparison.Ordinal);
        Assert.Contains("Production rollouts above 10 seats require approval.", body, StringComparison.Ordinal);
        Assert.Contains("Approval is required before a production rollout can exceed 10 seats.", body, StringComparison.Ordinal);
        Assert.Contains("Notes must be at least 10 characters.", body, StringComparison.Ordinal);
        Assert.Contains("validation-mixed-invalid", body, StringComparison.Ordinal);
        Assert.DoesNotContain("Queued a <strong>production</strong> rollout", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SpyInviteValidationBackend : IInviteValidationBackend
    {
        public int InvocationCount { get; private set; }

        public void Reset()
        {
            InvocationCount = 0;
        }

        public Task<InviteValidationBackendResult> SubmitAsync(InviteUserInput input, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(new InviteValidationBackendResult(true, 999, null));
        }
    }
}
