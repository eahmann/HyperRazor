using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HyperRazor.Demo.Tests;

public abstract class IntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    protected HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected static async Task<string> GetAntiforgeryTokenAsync(HttpClient client, string path = "/portal")
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractMetaContent(html, "hrz-antiforgery");
    }

    protected static async Task<string> ReadEventBlockAsync(StreamReader reader, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var builder = new StringBuilder();

        while (true)
        {
            var line = await reader.ReadLineAsync(linkedCts.Token);
            Assert.NotNull(line);

            if (line.Length == 0)
            {
                return builder.ToString();
            }

            builder.AppendLine(line);
        }
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
