using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace HyperRazor.Htmx.AspNetCore.Tests;

public class HyperRazorHtmxApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseHyperRazorHtmxVary_AddsVaryHeader_ForHtmlResponses()
    {
        await using var app = await BuildApp(async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync("<p>ok</p>");
        });

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test");

        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UseHtmxVary_AddsVaryHeader_ForHtmlResponses()
    {
        await using var app = await BuildApp(async context =>
        {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<p>ok</p>");
        }, useAlias: true);

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test");

        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UseHyperRazorHtmxVary_AppendsToExistingVaryHeader()
    {
        await using var app = await BuildApp(async context =>
        {
            context.Response.Headers[HeaderNames.Vary] = "Accept-Encoding";
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<p>ok</p>");
        });

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test");

        Assert.Contains("Accept-Encoding", response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UseHyperRazorHtmxVary_DoesNotAddHeader_ForNonHtmlResponses()
    {
        await using var app = await BuildApp(async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"ok\":true}");
        });

        var client = app.GetTestClient();
        var response = await client.GetAsync("/test");

        Assert.DoesNotContain(HtmxHeaderNames.Request, response.Headers.Vary, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<WebApplication> BuildApp(RequestDelegate endpoint, bool useAlias = false)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        builder.WebHost.UseTestServer();

        var app = builder.Build();
        if (useAlias)
        {
            app.UseHtmxVary();
        }
        else
        {
            app.UseHyperRazorHtmxVary();
        }

        app.MapGet("/test", endpoint);

        await app.StartAsync();
        return app;
    }
}
