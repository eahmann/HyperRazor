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
    public async Task OobDemo_CreateUser_UpdatesMainAndOobRegions()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/oob");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#displayName", "Jordan Avery");
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("form[hx-post='/fragments/users/create'] button[type='submit']"),
            r => r.Url.Contains("/fragments/users/create", StringComparison.Ordinal));

        Assert.Equal(200, response.Status);
        var html = await response.TextAsync();
        Assert.Contains("Jordan Avery", html, StringComparison.Ordinal);
        Assert.Contains("beforeend:#toast-stack", html, StringComparison.Ordinal);
        Assert.Contains("beforeend:#activity-feed", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob", html, StringComparison.Ordinal);
        Assert.Contains("create-user", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task ValidationDemo_InvalidThenValid_SubmitsAndSwapsExpectedMarkup()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/validation");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#validation-display-name", "A");
        await page.FillAsync("#validation-email", "invalid");
        var invalidResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#validation-form button[type='submit']"),
            response => response.Url.Contains("/fragments/users/create-validated", StringComparison.Ordinal));

        Assert.Equal(200, invalidResponse.Status);
        var invalidHtml = await invalidResponse.TextAsync();
        Assert.Contains("Validation Errors", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Display name must be at least 3 characters.", invalidHtml, StringComparison.Ordinal);
        Assert.Contains("Email must be a valid address.", invalidHtml, StringComparison.Ordinal);

        await page.FillAsync("#validation-display-name", "Riley Stone");
        await page.FillAsync("#validation-email", "riley@example.com");
        var validResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#validation-form button[type='submit']"),
            response => response.Url.Contains("/fragments/users/create-validated", StringComparison.Ordinal));

        Assert.Equal(200, validResponse.Status);
        var validHtml = await validResponse.TextAsync();
        Assert.Contains("Validation Passed", validHtml, StringComparison.Ordinal);
        Assert.Contains("riley@example.com", validHtml, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task RedirectDemo_SoftAndHardRedirects_NavigateToHome()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/redirects");
        await WaitForHtmxAsync(page);
        var softResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("button:has-text('Soft redirect (HX-Location)')"),
            response => response.Url.Contains("/fragments/navigation/soft", StringComparison.Ordinal));

        Assert.Equal(204, softResponse.Status);
        var softHeaders = await softResponse.AllHeadersAsync();
        Assert.True(softHeaders.ContainsKey("hx-location"));
        Assert.Contains("\"path\":\"/\"", softHeaders["hx-location"], StringComparison.Ordinal);

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/redirects");
        var hardResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("button:has-text('Hard redirect (HX-Redirect)')"),
            response => response.Url.Contains("/fragments/navigation/hard", StringComparison.Ordinal));

        Assert.Equal(204, hardResponse.Status);
        var hardHeaders = await hardResponse.AllHeadersAsync();
        Assert.True(hardHeaders.ContainsKey("hx-redirect"));
        Assert.Equal("/", hardHeaders["hx-redirect"]);
    }

    [SkippableFact]
    public async Task StatusDemo_500_SwapsErrorPanelAndOobToast()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/errors");
        await WaitForHtmxAsync(page);
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("button:has-text('Simulate 500')"),
            r => r.Url.Contains("/fragments/errors/500", StringComparison.Ordinal));

        Assert.Equal(500, response.Status);
        var html = await response.TextAsync();
        Assert.Contains("500 Server Error", html, StringComparison.Ordinal);
        Assert.Contains("Server-side failure demo", html, StringComparison.Ordinal);
        Assert.Contains("hx-swap-oob", html, StringComparison.Ordinal);
        await Assertions.Expect(page.Locator("#status-result")).ToContainTextAsync("500 Server Error");
        await Assertions.Expect(page.Locator("#toast-stack")).ToContainTextAsync("Server-side failure demo (500) with OOB toast.");
    }

    [SkippableFact]
    public async Task HeadDemo_Submit_UpdatesDocumentTitle()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/head");
        await WaitForHtmxAsync(page);
        await page.FillAsync("#head-title-input", "E2E Head Title");
        await page.FillAsync("#head-description-input", "E2E head description.");
        var response = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync("#head-demo-form button[type='submit']"),
            r => r.Url.Contains("/fragments/head/update", StringComparison.Ordinal));

        Assert.Equal(200, response.Status);
        Assert.Equal("xhr", response.Request.ResourceType);
        Assert.Equal("true", response.Request.Headers["hx-request"]);
        if (response.Request.Headers.TryGetValue("hx-request-type", out var requestTypeHeader))
        {
            Assert.NotEqual("full", requestTypeHeader);
        }
        var html = await response.TextAsync();
        Assert.Contains("E2E Head Title", html, StringComparison.Ordinal);
        Assert.Contains("<head", html, StringComparison.Ordinal);
        Assert.Contains("hx-head=\"merge\"", html, StringComparison.Ordinal);
        Assert.Contains("<title>E2E Head Title</title>", html, StringComparison.Ordinal);
        await Assertions.Expect(page).ToHaveTitleAsync("E2E Head Title");
        await Assertions.Expect(page.Locator("#head-demo-result")).ToContainTextAsync("Head Updated");
    }

    [SkippableFact]
    public async Task AppNav_BoostedLinks_SwapMainRegionAndPushUrl()
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

        var searchResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".app-nav a[href='/demos/search']"),
            response => response.Url.Contains("/demos/search", StringComparison.Ordinal));

        Assert.Equal(200, searchResponse.Status);
        Assert.Equal("xhr", searchResponse.Request.ResourceType);
        Assert.Equal("true", searchResponse.Request.Headers["hx-request"]);
        var searchHtml = await searchResponse.TextAsync();
        Assert.Contains("Live Search", searchHtml, StringComparison.Ordinal);
        await ExpectHeadingAsync(page, "Live Search");
        Assert.EndsWith("/demos/search", page.Url, StringComparison.Ordinal);
        Assert.Equal(1, await page.Locator("#app-shell").CountAsync());
        Assert.Empty(pageErrors);
        Assert.Empty(consoleErrors);

        var validationResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".app-nav a[href='/demos/validation']"),
            response => response.Url.Contains("/demos/validation", StringComparison.Ordinal));

        Assert.Equal(200, validationResponse.Status);
        await ExpectHeadingAsync(page, "Form Validation");
        Assert.EndsWith("/demos/validation", page.Url, StringComparison.Ordinal);
        Assert.Equal(1, await page.Locator("#app-shell").CountAsync());
    }

    [SkippableFact]
    public async Task LayoutSwapDemo_NavigatesAcrossTopNavAndSideNavLayouts()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/basic");
        await WaitForHtmxAsync(page);

        var layoutIntroResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".app-nav a[href='/demos/layout-swap']"),
            response => response.Url.Contains("/demos/layout-swap", StringComparison.Ordinal));

        Assert.Equal(200, layoutIntroResponse.Status);
        await ExpectHeadingAsync(page, "Layout Swap Demo");
        await Assertions.Expect(page.Locator("#side-nav-layout-shell")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".app-nav")).ToHaveCountAsync(0);
        Assert.EndsWith("/demos/layout-swap", page.Url, StringComparison.Ordinal);

        var layoutDetailsResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".side-layout-nav a[href='/demos/layout-swap/details']"),
            response => response.Url.Contains("/demos/layout-swap/details", StringComparison.Ordinal));

        Assert.Equal(200, layoutDetailsResponse.Status);
        await ExpectHeadingAsync(page, "Layout Swap Details");
        Assert.EndsWith("/demos/layout-swap/details", page.Url, StringComparison.Ordinal);

        var backToTopNavResponse = await page.RunAndWaitForResponseAsync(
            async () => await page.ClickAsync(".side-layout-nav a[href='/demos/basic']"),
            response => response.Url.Contains("/demos/basic", StringComparison.Ordinal));

        Assert.Equal(200, backToTopNavResponse.Status);
        await ExpectHeadingAsync(page, "Server Trigger");
        Assert.EndsWith("/demos/basic", page.Url, StringComparison.Ordinal);
        Assert.Equal(1, await page.Locator("#app-shell").CountAsync());
        await Assertions.Expect(page.Locator(".app-nav")).ToHaveCountAsync(1);
    }

    private static async Task WaitForHtmxAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => typeof window.htmx !== 'undefined'");
    }

    private static async Task ExpectHeadingAsync(IPage page, string text)
    {
        var heading = page.Locator("main#hrx-main-layout h2").First;
        await heading.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Assertions.Expect(heading).ToHaveTextAsync(text);
    }
}
