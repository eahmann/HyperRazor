using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HyperRazor.E2E;

[Collection(DemoMvcE2ECollection.Name)]
public sealed class DemoMvcFlowsE2ETests
{
    private readonly DemoMvcE2EFixture _fixture;

    public DemoMvcFlowsE2ETests(DemoMvcE2EFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task UsersPage_ProvisionUser_UpdatesMainAndOobRegions()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/users");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#displayName", "Jordan Avery");
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("form[hx-post='/fragments/users/provision'] button[type='submit']"),
            r => r.Url.Contains("/fragments/users/provision", StringComparison.Ordinal));

        Assert.Equal(200, response.Status);
        var html = await response.TextAsync();
        Assert.Contains("Jordan Avery", html, StringComparison.Ordinal);
        Assert.Contains("beforeend:#toast-stack", html, StringComparison.Ordinal);
        Assert.Contains("beforeend:#activity-feed", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob", html, StringComparison.Ordinal);
        Assert.Contains("users-provision", html, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#users-list")).ToContainTextAsync("Jordan Avery");
        await Assertions.Expect(page.Locator("#activity-feed")).ToContainTextAsync("Jordan Avery");
    }

    [SkippableFact]
    public async Task UsersPage_Invite_InvalidThenValid_SubmitsAndSwapsExpectedMarkup()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/users");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#users-invite-displayname", "A");
        await page.FillAsync("#users-invite-email", "invalid");
        var invalidResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#validation-form button[type='submit']"),
            response => response.Url.Contains("/users/invite", StringComparison.Ordinal));

        Assert.Equal(200, invalidResponse.Status);
        var invalidHtml = await invalidResponse.TextAsync();
        Assert.Contains("id=\"validation-form-shell\"", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", invalidHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-form-shell")).ToContainTextAsync("Display name must be at least 3 characters.");

        await page.FillAsync("#users-invite-displayname", "Riley Stone");
        await page.FillAsync("#users-invite-email", "riley@example.com");
        var validResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#validation-form button[type='submit']"),
            response => response.Url.Contains("/users/invite", StringComparison.Ordinal));

        Assert.Equal(200, validResponse.Status);
        var validHtml = await validResponse.TextAsync();
        Assert.Contains("Created <strong>Riley Stone</strong>", validHtml, StringComparison.Ordinal);
        Assert.Contains("riley@example.com", validHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-form-shell")).ToContainTextAsync("Created Riley Stone");
    }

    [SkippableFact]
    public async Task ValidationPage_LiveServerValidation_UpdatesOnlyServerOwnedRegions()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/validation");
        await WaitForHtmxAsync(page);

        await page.FillAsync("#validation-minimal-proxy-displayname", "A");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--client"))
            .ToContainTextAsync("Display name must be at least 3 characters.");

        await page.ClickAsync("#validation-minimal-proxy-email");
        await page.PressAsync("#validation-minimal-proxy-email", "Control+A");
        await page.PressAsync("#validation-minimal-proxy-email", "Backspace");
        var liveResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.FillAsync("#validation-minimal-proxy-email", "backend-taken@example.com"),
            response => response.Url.Contains("/validation/live", StringComparison.Ordinal) && response.Status == 200);

        var liveHtml = await liveResponse.TextAsync();
        Assert.Contains("id=\"validation-minimal-proxy-email-message--server\"", liveHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"validation-minimal-proxy-form-shell\"", liveHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--client"))
            .ToContainTextAsync("Display name must be at least 3 characters.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-email-message--server"))
            .ToContainTextAsync("Email already exists in the upstream directory.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-summary"))
            .ToContainTextAsync("Backend would reject this invite on submit.");
    }

    [SkippableFact]
    public async Task ValidationPage_ClientValidation_ShowsRequiredOnBlurAndClearsInvalidStateWhenFixed()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/validation");
        await WaitForHtmxAsync(page);

        await page.FillAsync("#validation-minimal-proxy-displayname", string.Empty);
        await page.ClickAsync("#validation-minimal-proxy-email");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--client"))
            .ToContainTextAsync("Display name is required.");

        await page.FillAsync("#validation-minimal-proxy-email", string.Empty);
        await page.ClickAsync("#validation-minimal-proxy-displayname");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-email-message--client"))
            .ToContainTextAsync("Email is required.");

        await page.FillAsync("#validation-minimal-proxy-displayname", "A");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--client"))
            .ToContainTextAsync("Display name must be at least 3 characters.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname"))
            .ToHaveAttributeAsync("aria-invalid", "true");

        await page.FillAsync("#validation-minimal-proxy-displayname", "Alex");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--client"))
            .ToBeEmptyAsync();
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname"))
            .ToHaveAttributeAsync("aria-invalid", "false");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname"))
            .ToHaveClassAsync(new Regex(@"\binput-validation-valid\b"));
    }

    [SkippableFact]
    public async Task ValidationPage_LiveServerValidation_UpdatesDependentFieldSlotsOutOfBand()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/validation");
        await WaitForHtmxAsync(page);

        var emailResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.FillAsync("#validation-minimal-proxy-email", "shared-mailbox@example.com"),
            response => response.Url.Contains("/validation/live", StringComparison.Ordinal) && response.Status == 200);

        var emailHtml = await emailResponse.TextAsync();
        Assert.Contains("id=\"validation-minimal-proxy-displayname-message--server\"", emailHtml, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", emailHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--server"))
            .ToContainTextAsync("Shared mailbox invites must use a team display name.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-summary"))
            .ToContainTextAsync("Shared mailbox invites need a team display name before the backend will accept them.");

        var displayNameResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.FillAsync("#validation-minimal-proxy-displayname", "Team Inbox"),
            response => response.Url.Contains("/validation/live", StringComparison.Ordinal) && response.Status == 200);

        var displayNameHtml = await displayNameResponse.TextAsync();
        Assert.Contains("id=\"validation-minimal-proxy-displayname-message--server\"", displayNameHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-displayname-message--server"))
            .ToBeEmptyAsync();
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-summary"))
            .ToBeEmptyAsync();
    }

    [SkippableFact]
    public async Task AccessReview_TaskFlow_InvalidThenValid_ReturnsToWorkbenchViaHxLocation()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/access-requests");
        await WaitForHtmxAsync(page);

        var reviewPageResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("a[href='/access-requests/104/review']"),
            response => response.Url.Contains("/access-requests/104/review", StringComparison.Ordinal));

        Assert.Equal(200, reviewPageResponse.Status);
        await ExpectHeadingAsync(page, "Complete the workflow, then return to the queue.");

        await page.FillAsync("#review-ticket-id", "SEC");
        await page.FillAsync("#review-justification", "short");
        var invalidResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#review-request-form button[type='submit']"),
            response => response.Url.Contains("/fragments/access-requests/104/review", StringComparison.Ordinal));

        Assert.Equal(200, invalidResponse.Status);
        var invalidHtml = await invalidResponse.TextAsync();
        Assert.Contains("Review Validation Errors", invalidHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#review-request-result")).ToContainTextAsync("Review Validation Errors");

        await page.FillAsync("#review-ticket-id", "SEC-104");
        await page.FillAsync("#review-justification", "Approve temporary billing export access for the planned change window.");
        var validResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#review-request-form button[type='submit']"),
            response => response.Url.Contains("/fragments/access-requests/104/review", StringComparison.Ordinal));

        Assert.Equal(204, validResponse.Status);
        var headers = await validResponse.AllHeadersAsync();
        Assert.True(headers.ContainsKey("hx-location"));
        Assert.Contains("/access-requests?completed=104", headers["hx-location"], StringComparison.Ordinal);

        await page.WaitForFunctionAsync(
            "() => window.location.pathname + window.location.search === '/access-requests?completed=104'");
        await Assertions.Expect(page.Locator("main#hrz-main-layout")).ToContainTextAsync("Request processed");
        await Assertions.Expect(page.Locator("#workbench-layout-shell")).ToBeVisibleAsync();
    }

    [SkippableFact]
    public async Task IncidentsPage_BackendFailure_SwapsErrorPanelAndOobToast()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/incidents");
        await WaitForHtmxAsync(page);
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("button:has-text('Backend failure')"),
            r => r.Url.Contains("/fragments/incidents/drills/backend-failure", StringComparison.Ordinal));

        Assert.Equal(500, response.Status);
        var html = await response.TextAsync();
        Assert.Contains("500 Server Error", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob", html, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#status-result")).ToContainTextAsync("500 Server Error");
        await Assertions.Expect(page.Locator("#toast-stack")).ToContainTextAsync("Server-side failure demo (500) with OOB toast.");
    }

    [SkippableFact]
    public async Task BrandingPage_Submit_UpdatesDocumentTitleAndDedupesScript()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/settings/branding");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#head-title-input", "E2E Branding Title");
        await page.FillAsync("#head-description-input", "E2E branding description.");
        await page.SelectOptionAsync("#head-accent-input", "amber");
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#head-demo-form button[type='submit']"),
            r => r.Url.Contains("/fragments/settings/branding", StringComparison.Ordinal));

        Assert.Equal(200, response.Status);
        var html = await response.TextAsync();
        Assert.Contains("E2E Branding Title", html, StringComparison.Ordinal);
        Assert.Contains("<head", html, StringComparison.Ordinal);
        Assert.Contains("hx-head=\"merge\"", html, StringComparison.Ordinal);
        await Assertions.Expect(page).ToHaveTitleAsync("E2E Branding Title");
        await Assertions.Expect(page.Locator("#head-demo-result")).ToContainTextAsync("Head Updated");
        await Assertions.Expect(page.Locator("#head-demo-result")).ToContainTextAsync("Accent preset: Amber");
        await Assertions.Expect(page.Locator("#head-script-status")).ToContainTextAsync("Keyed asset count: 1");

        await page.FillAsync("#head-title-input", "E2E Branding Title 2");
        await page.SelectOptionAsync("#head-accent-input", "rose");
        var secondResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#head-demo-form button[type='submit']"),
            r => r.Url.Contains("/fragments/settings/branding", StringComparison.Ordinal));

        Assert.Equal(200, secondResponse.Status);
        await Assertions.Expect(page).ToHaveTitleAsync("E2E Branding Title 2");
        await Assertions.Expect(page.Locator("#head-demo-result")).ToContainTextAsync("Accent preset: Rose");
        await Assertions.Expect(page.Locator("#head-script-status")).ToContainTextAsync("Keyed asset count: 1");
    }

    [SkippableFact]
    public async Task Dashboard_QuickChecks_RenderResultAndUpdateEventLog()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/");
        await WaitForHtmxAsync(page);

        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("button:has-text('Run Sync Check')"),
            r => r.Url.Contains("/fragments/dashboard/sync-check", StringComparison.Ordinal));

        Assert.Equal(200, response.Status);
        await Assertions.Expect(page.Locator("#dashboard-sync-status")).ToContainTextAsync("Sync check completed");
        await Assertions.Expect(page.Locator("#dashboard-sync-status")).ToContainTextAsync("action-body");
        await Assertions.Expect(page.Locator("#dashboard-event-log")).ToContainTextAsync("Saved successfully.");
    }

    [SkippableFact]
    public async Task AppNav_BoostedLinks_SwapAcrossAdminAndWorkbenchLayouts()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();
        var consoleErrors = new List<string>();
        var pageErrors = new List<string>();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };
        page.PageError += (_, msg) => pageErrors.Add(msg);

        await page.GotoAsync($"{_fixture.BaseUrl}/");
        await WaitForHtmxAsync(page);

        var usersResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".app-nav a[href='/users']"),
            response => response.Url.Contains("/users", StringComparison.Ordinal));

        Assert.Equal(200, usersResponse.Status);
        Assert.Equal("xhr", usersResponse.Request.ResourceType);
        Assert.Equal("true", usersResponse.Request.Headers["hx-request"]);
        await ExpectHeadingAsync(page, "User Administration");
        Assert.EndsWith("/users", page.Url, StringComparison.Ordinal);
        Assert.Equal(1, await page.Locator("#app-shell").CountAsync());
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("/users");
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("admin");
        await Assertions.Expect(page.Locator(".app-nav a[href='/users']")).ToHaveAttributeAsync("aria-current", "page");
        Assert.Empty(pageErrors);
        Assert.Empty(consoleErrors);

        var accessResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".app-nav a[href='/access-requests']"),
            response => response.Url.Contains("/access-requests", StringComparison.Ordinal));

        Assert.Equal(200, accessResponse.Status);
        Assert.Equal("admin", accessResponse.Request.Headers["x-hrz-layout-family"]);
        var accessHeaders = await accessResponse.AllHeadersAsync();
        Assert.True(accessHeaders.TryGetValue("hx-retarget", out var accessRetarget));
        Assert.Equal("#hrz-app-shell", accessRetarget);
        await ExpectHeadingAsync(page, "Access Requests");
        await Assertions.Expect(page.Locator("#workbench-layout-shell")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("/access-requests");
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("workbench");
        await Assertions.Expect(page.Locator(".app-nav a[href='/access-requests']")).ToHaveAttributeAsync("aria-current", "page");
        Assert.EndsWith("/access-requests", page.Url, StringComparison.Ordinal);

        var incidentsResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".side-layout-nav a[href='/incidents']"),
            response => response.Url.Contains("/incidents", StringComparison.Ordinal));

        Assert.Equal(200, incidentsResponse.Status);
        Assert.Equal("workbench", incidentsResponse.Request.Headers["x-hrz-layout-family"]);
        var incidentsHeaders = await incidentsResponse.AllHeadersAsync();
        Assert.False(incidentsHeaders.ContainsKey("hx-retarget"));
        await ExpectHeadingAsync(page, "Incidents");
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("/incidents");
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("workbench");
        await Assertions.Expect(page.Locator(".app-nav a[href='/incidents']")).ToHaveAttributeAsync("aria-current", "page");
        Assert.EndsWith("/incidents", page.Url, StringComparison.Ordinal);

        var backResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".side-layout-nav a[href='/users']"),
            response => response.Url.Contains("/users", StringComparison.Ordinal));

        Assert.Equal(200, backResponse.Status);
        Assert.Equal("workbench", backResponse.Request.Headers["x-hrz-layout-family"]);
        var backHeaders = await backResponse.AllHeadersAsync();
        Assert.True(backHeaders.TryGetValue("hx-retarget", out var backRetarget));
        Assert.Equal("#hrz-app-shell", backRetarget);
        await ExpectHeadingAsync(page, "User Administration");
        Assert.EndsWith("/users", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator(".app-nav")).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("/users");
        await Assertions.Expect(page.Locator("#app-chrome-toolbar")).ToContainTextAsync("admin");
        await Assertions.Expect(page.Locator(".app-nav a[href='/users']")).ToHaveAttributeAsync("aria-current", "page");
    }

    private static async Task WaitForHtmxAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => typeof window.htmx !== 'undefined'");
    }

    private static async Task ExpectHeadingAsync(IPage page, string text)
    {
        var heading = page.Locator("main#hrz-main-layout h2").First;
        await heading.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assertions.Expect(heading).ToHaveTextAsync(text);
    }
}
