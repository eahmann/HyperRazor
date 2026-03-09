using System.Net;
using HyperRazor.Htmx;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HyperRazor.Htmx.Tests;

public class HyperRazorHtmxDiagnosticsApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseHyperRazorDiagnostics_LogsLayoutPromotionMetadata_WhenPresent()
    {
        var loggerProvider = new TestLoggerProvider();

        await using var app = await BuildApp(async context =>
        {
            context.Items[typeof(HtmxLayoutPromotionDiagnostics)] = new HtmxLayoutPromotionDiagnostics(
                ClientLayoutFamily: "main",
                RouteLayoutFamily: "side",
                PromotionMode: "ShellSwap",
                PromotionApplied: true);
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<p>ok</p>");
        }, loggerProvider);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test");
        request.Headers.Add(HtmxHeaderNames.Request, "true");

        var client = app.GetTestClient();
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(
            loggerProvider.Messages,
            message =>
                message.Contains("clientLayoutFamily=main", StringComparison.Ordinal)
                && message.Contains("routeLayoutFamily=side", StringComparison.Ordinal)
                && message.Contains("promotionMode=ShellSwap", StringComparison.Ordinal)
                && message.Contains("promotionApplied=True", StringComparison.Ordinal));
    }

    private static async Task<WebApplication> BuildApp(RequestDelegate endpoint, ILoggerProvider loggerProvider)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(loggerProvider);
        builder.Services.AddHtmx();

        var app = builder.Build();
        app.UseHyperRazorDiagnostics();
        app.MapGet("/test", endpoint);

        await app.StartAsync();
        return app;
    }

    private sealed class TestLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(Messages);
        }

        public void Dispose()
        {
        }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly List<string> _messages;

        public TestLogger(List<string> messages)
        {
            _messages = messages;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
