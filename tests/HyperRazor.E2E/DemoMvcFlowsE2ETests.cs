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
        await page.FillAsync("#validation-display-name", "A");
        await page.FillAsync("#validation-email", "invalid");
        var invalidResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#validation-form button[type='submit']"),
            response => response.Url.Contains("/users/invite", StringComparison.Ordinal));

        Assert.Equal(200, invalidResponse.Status);
        var invalidHtml = await invalidResponse.TextAsync();
        Assert.Contains("id=\"validation-form-shell\"", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", invalidHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-form-shell")).ToContainTextAsync("Display name must be at least 3 characters.");

        await page.FillAsync("#validation-display-name", "Riley Stone");
        await page.FillAsync("#validation-email", "riley@example.com");
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

        await page.FillAsync("#validation-minimal-proxy-display-name", "A");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-display-name-client"))
            .ToContainTextAsync("Display name must be at least 3 characters.");

        await page.ClickAsync("#validation-minimal-proxy-email");
        await page.PressAsync("#validation-minimal-proxy-email", "Control+A");
        await page.PressAsync("#validation-minimal-proxy-email", "Backspace");
        var liveResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.FillAsync("#validation-minimal-proxy-email", "backend-taken@example.com"),
            response => response.Url.Contains("/validation/live", StringComparison.Ordinal) && response.Status == 200);

        var liveHtml = await liveResponse.TextAsync();
        Assert.Contains("id=\"validation-minimal-proxy-email-server\"", liveHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("id=\"validation-minimal-proxy-form-shell\"", liveHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-display-name-client"))
            .ToContainTextAsync("Display name must be at least 3 characters.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-email-server"))
            .ToContainTextAsync("Email already exists in the upstream directory.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-server-summary"))
            .ToContainTextAsync("Backend would reject this invite on submit.");
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
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", emailHtml, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob=\"outerHTML\"", emailHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-display-name-server"))
            .ToContainTextAsync("Shared mailbox invites must use a team display name.");
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-server-summary"))
            .ToContainTextAsync("Shared mailbox invites need a team display name before the backend will accept them.");

        var displayNameResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.FillAsync("#validation-minimal-proxy-display-name", "Team Inbox"),
            response => response.Url.Contains("/validation/live", StringComparison.Ordinal) && response.Status == 200);

        var displayNameHtml = await displayNameResponse.TextAsync();
        Assert.Contains("id=\"validation-minimal-proxy-display-name-server\"", displayNameHtml, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-display-name-server"))
            .ToBeEmptyAsync();
        await Assertions.Expect(page.Locator("#validation-minimal-proxy-server-summary"))
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
    public async Task SseDemoPage_StreamsHtmlAndStopsAfterDone()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/sse");
        await WaitForHtmxAsync(page);

        var cards = page.Locator("#sse-live-feed .sse-event-card");

        await Assertions.Expect(cards).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("#sse-stream-status")).ToContainTextAsync("Stream connected");
        await Assertions.Expect(page.Locator("#sse-last-event-id")).ToContainTextAsync("Fresh Stream");

        await Assertions.Expect(cards).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("#sse-stream-status")).ToContainTextAsync("Secondary target updated");

        await Assertions.Expect(cards).ToHaveCountAsync(3);
        await Assertions.Expect(page.Locator("#sse-stream-status")).ToContainTextAsync("Closed cleanly");

        await page.WaitForTimeoutAsync(1800);

        Assert.Equal(3, await cards.CountAsync());
        await Assertions.Expect(page.Locator("#sse-live-feed")).ToContainTextAsync("Graceful shutdown prepared");
    }

    [SkippableFact]
    public async Task SseControlEventsPage_DispatchesNamedEventsIntoFollowUpFragmentRequests()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        var staleResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("/demos/sse/control-events/panels/stale", StringComparison.Ordinal));
        var rateLimitedResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("/demos/sse/control-events/panels/rate-limited", StringComparison.Ordinal));
        var resetResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("/demos/sse/control-events/panels/reset", StringComparison.Ordinal));
        var unauthorizedResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("/demos/sse/control-events/panels/unauthorized", StringComparison.Ordinal));

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/sse/control-events");
        await WaitForHtmxAsync(page);

        var staleResponse = await staleResponseTask;
        Assert.Equal(200, staleResponse.Status);
        await Assertions.Expect(page.Locator("#sse-control-stale")).ToContainTextAsync("Stale signal received");
        await Assertions.Expect(page.Locator("#sse-control-stale")).ToContainTextAsync("HTMX dispatched sse:stale");

        var rateLimitedResponse = await rateLimitedResponseTask;
        Assert.Equal(200, rateLimitedResponse.Status);
        await Assertions.Expect(page.Locator("#sse-control-rate-limited")).ToContainTextAsync("Rate-limited signal received");
        await Assertions.Expect(page.Locator("#sse-control-rate-limited")).ToContainTextAsync("slower reconnect cadence");

        var resetResponse = await resetResponseTask;
        Assert.Equal(200, resetResponse.Status);
        await Assertions.Expect(page.Locator("#sse-control-reset")).ToContainTextAsync("Reset signal received");
        await Assertions.Expect(page.Locator("#sse-control-reset")).ToContainTextAsync("fresh server snapshot");

        var unauthorizedResponse = await unauthorizedResponseTask;
        Assert.Equal(200, unauthorizedResponse.Status);
        await Assertions.Expect(page.Locator("#sse-control-unauthorized")).ToContainTextAsync("Unauthorized signal received");
        await Assertions.Expect(page.Locator("#sse-control-unauthorized")).ToContainTextAsync("reauthentication");

        await Assertions.Expect(page.Locator("#sse-control-grid .sse-control-event-panel")).ToHaveCountAsync(4);
        await page.WaitForTimeoutAsync(900);
    }

    [SkippableFact]
    public async Task SseReplayPage_ReconnectsAndAppendsBufferedFramesBeforeDone()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/sse/replay");
        await WaitForHtmxAsync(page);

        var cards = page.Locator("#sse-replay-feed .sse-event-card");

        await Assertions.Expect(cards).ToHaveCountAsync(1);
        await Assertions.Expect(cards).ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("#sse-replay-connection")).ToContainTextAsync("Disconnect after replay-demo-02");

        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#sse-replay-feed .sse-event-card').length === 5",
            new PageWaitForFunctionOptions
            {
                Timeout = 15000
            });

        await Assertions.Expect(page.Locator("#sse-replay-resume")).ToContainTextAsync("Resumed after replay-demo-02");
        await Assertions.Expect(page.Locator("#sse-replay-feed")).ToContainTextAsync("Buffered event recovered");
        await Assertions.Expect(page.Locator("#sse-replay-feed")).ToContainTextAsync("Replay buffer drained");
        await Assertions.Expect(cards.Last).ToContainTextAsync("Live streaming resumed");

        await page.WaitForTimeoutAsync(1200);

        Assert.Equal(5, await cards.CountAsync());
    }

    [SkippableFact]
    public async Task SsePages_InlineLinks_NavigateThroughHtmxRequests()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/sse");
        await WaitForHtmxAsync(page);

        var controlEventsResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("a[href='/demos/sse/control-events']"),
            response => response.Url.EndsWith("/demos/sse/control-events", StringComparison.Ordinal));

        Assert.Equal(200, controlEventsResponse.Status);
        Assert.Equal("xhr", controlEventsResponse.Request.ResourceType);
        Assert.Equal("true", controlEventsResponse.Request.Headers["hx-request"]);
        await ExpectHeadingAsync(page, "SSE Control Events");
        Assert.EndsWith("/demos/sse/control-events", page.Url, StringComparison.Ordinal);

        var replayResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("a[href='/demos/sse/replay']"),
            response => response.Url.EndsWith("/demos/sse/replay", StringComparison.Ordinal));

        Assert.Equal(200, replayResponse.Status);
        Assert.Equal("xhr", replayResponse.Request.ResourceType);
        Assert.Equal("true", replayResponse.Request.Headers["hx-request"]);
        await ExpectHeadingAsync(page, "SSE Replay");
        Assert.EndsWith("/demos/sse/replay", page.Url, StringComparison.Ordinal);

        var basicsResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("a[href='/demos/sse']"),
            response => response.Url.EndsWith("/demos/sse", StringComparison.Ordinal));

        Assert.Equal(200, basicsResponse.Status);
        Assert.Equal("xhr", basicsResponse.Request.ResourceType);
        Assert.Equal("true", basicsResponse.Request.Headers["hx-request"]);
        await ExpectHeadingAsync(page, "SSE Live Feed");
        Assert.EndsWith("/demos/sse", page.Url, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task NotificationsDemoPage_StreamsTenNotesAndStopsAfterDone()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/notifications");
        await WaitForHtmxAsync(page);

        var cards = page.Locator("#notifications-list .notification-card");

        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#notifications-list .notification-card').length >= 1");
        await Assertions.Expect(page.Locator("#notifications-unread-badge")).ToHaveTextAsync("1");

        await page.WaitForFunctionAsync(
            "() => document.querySelectorAll('#notifications-list .notification-card').length === 10",
            new PageWaitForFunctionOptions
            {
                Timeout = 15000
            });

        await Assertions.Expect(page.Locator("#notifications-unread-badge")).ToHaveTextAsync("10");
        await Assertions.Expect(page.Locator("#notifications-stream-state")).ToContainTextAsync("notif-10");
        await Assertions.Expect(page.Locator("#notifications-stream-state")).ToContainTextAsync("10 / 10");
        await Assertions.Expect(cards.First).ToContainTextAsync("EU auth incident resolved");

        await page.WaitForTimeoutAsync(1200);

        Assert.Equal(10, await cards.CountAsync());
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
