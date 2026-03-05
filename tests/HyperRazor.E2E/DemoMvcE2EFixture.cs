using System.Diagnostics;
using System.Text;
using Microsoft.Playwright;

namespace HyperRazor.E2E;

public sealed class DemoMvcE2EFixture : IAsyncLifetime
{
    private const string Host = "127.0.0.1";
    private const int Port = 5076;

    private readonly StringBuilder _serverOutput = new();
    private Process? _serverProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly string _browserCachePath = "/tmp/ms-playwright";

    public string BaseUrl => $"http://{Host}:{Port}";

    public string? SkipReason { get; private set; }

    public bool CanRun => string.IsNullOrWhiteSpace(SkipReason) && _browser is not null;

    public async Task InitializeAsync()
    {
        await StartDemoMvcServerAsync();
        await InitializeBrowserAsync();
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        if (_serverProcess is { HasExited: false })
        {
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }
    }

    public async Task<IBrowserContext> NewContextAsync()
    {
        if (!CanRun)
        {
            var reason = SkipReason ?? "Playwright browser was not initialized.";
            throw new InvalidOperationException($"E2E browser context unavailable: {reason}");
        }

        return await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl
        });
    }

    private async Task InitializeBrowserAsync()
    {
        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", _browserCachePath);
        _playwright = await Playwright.CreateAsync();

        try
        {
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = ["--no-sandbox"]
            });
        }
        catch (Exception launchException)
        {
            SkipReason = $"Playwright Chromium could not launch: {launchException.Message}";
        }
    }

    private async Task StartDemoMvcServerAsync()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "src", "HyperRazor.Demo.Mvc", "HyperRazor.Demo.Mvc.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" --urls {BaseUrl}",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _serverProcess = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Demo.Mvc server process.");
        _serverProcess.OutputDataReceived += OnServerDataReceived;
        _serverProcess.ErrorDataReceived += OnServerDataReceived;
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            if (_serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Demo.Mvc server exited before becoming ready.{Environment.NewLine}{_serverOutput}");
            }

            try
            {
                var response = await client.GetAsync($"{BaseUrl}/");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Server may still be starting.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out waiting for Demo.Mvc server at {BaseUrl}.{Environment.NewLine}{_serverOutput}");
    }

    private static string ResolveRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var solutionPath = Path.Combine(current.FullName, "HyperRazor.slnx");
            if (File.Exists(solutionPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing HyperRazor.slnx.");
    }

    private void OnServerDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.Data))
        {
            _serverOutput.AppendLine(e.Data);
        }
    }
}
