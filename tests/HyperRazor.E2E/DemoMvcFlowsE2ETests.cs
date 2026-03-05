using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

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
        await page.FillAsync("#displayName", "Jordan Avery");
        await page.ClickAsync("form[hx-post='/fragments/users/create'] button[type='submit']");

        await Expect(page.Locator("#users-list")).ToContainTextAsync("Jordan Avery");
        await Expect(page.Locator("#toast-stack")).ToContainTextAsync("Created Jordan Avery.");
        await Expect(page.Locator("#activity-feed")).ToContainTextAsync("Jordan Avery");
        await Expect(page.Locator("#hx-debug-shell")).ToContainTextAsync("create-user");

        var countText = (await page.Locator("#user-count-shell").InnerTextAsync()).Trim();
        Assert.True(int.TryParse(countText, out var count), $"Expected numeric count, received '{countText}'.");
        Assert.True(count >= 6, $"Expected count >= 6, received {count}.");
    }

    [SkippableFact]
    public async Task ValidationDemo_InvalidThenValid_SubmitsAndSwapsExpectedMarkup()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/validation");
        await page.FillAsync("#validation-display-name", "A");
        await page.FillAsync("#validation-email", "invalid");
        await page.ClickAsync("#validation-form button[type='submit']");

        await Expect(page.Locator("#validation-result")).ToContainTextAsync("Validation Errors");
        await Expect(page.Locator("#validation-result")).ToContainTextAsync("Display name must be at least 3 characters.");
        await Expect(page.Locator("#validation-result")).ToContainTextAsync("Email must be a valid address.");

        await page.FillAsync("#validation-display-name", "Riley Stone");
        await page.FillAsync("#validation-email", "riley@example.com");
        await page.ClickAsync("#validation-form button[type='submit']");

        await Expect(page.Locator("#validation-result")).ToContainTextAsync("Validation Passed");
        await Expect(page.Locator("#validation-result")).ToContainTextAsync("riley@example.com");
    }

    [SkippableFact]
    public async Task RedirectDemo_SoftAndHardRedirects_NavigateToHome()
    {
        Skip.IfNot(_fixture.CanRun, _fixture.SkipReason ?? "Playwright browser runtime unavailable.");

        await using var context = await _fixture.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/redirects");
        await page.ClickAsync("button:has-text('Soft redirect (HX-Location)')");

        await Expect(page.Locator("#demo-server-trigger")).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex(@"/$"));

        await page.GotoAsync($"{_fixture.BaseUrl}/demos/redirects");
        await page.ClickAsync("button:has-text('Hard redirect (HX-Redirect)')");

        await Expect(page.Locator("#demo-server-trigger")).ToBeVisibleAsync();
        await Expect(page).ToHaveURLAsync(new Regex(@"/$"));
    }
}
