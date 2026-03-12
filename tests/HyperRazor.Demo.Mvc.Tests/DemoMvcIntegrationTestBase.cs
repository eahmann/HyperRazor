using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Tests;

public abstract class DemoMvcIntegrationTestBase
{
    private readonly WebApplicationFactory<Program> _factory;

    protected DemoMvcIntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    protected HttpClient CreateClient(Action<IServiceCollection>? configureServices = null)
    {
        var factory = configureServices is null
            ? _factory
            : _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(configureServices);
            });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected static async Task<string> GetAntiforgeryTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return ExtractMetaContent(html, "hrz-antiforgery");
    }

    protected static async Task<string> ReadEventBlockAsync(StreamReader reader)
    {
        var builder = new StringBuilder();

        while (true)
        {
            var line = await reader.ReadLineAsync();
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
