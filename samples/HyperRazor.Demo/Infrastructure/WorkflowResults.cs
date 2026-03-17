using HyperRazor.Demo.Components.Fragments;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Demo.Infrastructure;

public static class WorkflowResults
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string AppShellSelector = "#hrz-app-shell";

    public static IResult EnterConsole(HttpContext context, string workspaceKey)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = BuildUsersPath(workspaceKey);
        if (context.HtmxRequest().IsHtmx)
        {
            context.HtmxResponse().Location(new
            {
                path,
                target = AppShellSelector,
                swap = "outerHTML",
                select = AppShellSelector,
                headers = new Dictionary<string, string>
                {
                    [HtmxHeaderNames.RequestType] = "full"
                }
            });

            return Results.Content(string.Empty, HtmlContentType);
        }

        context.Response.Headers.Location = path;
        return Results.StatusCode(StatusCodes.Status303SeeOther);
    }

    public static Task<IResult> StartProvisioningAsync(
        HttpContext context,
        ProvisioningOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);

        if (context.HtmxRequest().IsHtmx)
        {
            return HrzResults.Fragment<ProvisioningShellCard>(
                context,
                new
                {
                    Operation = operation
                },
                cancellationToken: cancellationToken);
        }

        context.Response.Headers.Location = BuildUsersPath(operation.WorkspaceKey, operation.OperationId);
        return Task.FromResult<IResult>(Results.StatusCode(StatusCodes.Status303SeeOther));
    }

    public static string BuildUsersPath(string workspaceKey, string? operationId = null)
    {
        var values = new List<KeyValuePair<string, string?>>
        {
            new("workspace", workspaceKey)
        };

        if (!string.IsNullOrWhiteSpace(operationId))
        {
            values.Add(new KeyValuePair<string, string?>("operation", operationId));
        }

        return $"/users{QueryString.Create(values)}";
    }
}
