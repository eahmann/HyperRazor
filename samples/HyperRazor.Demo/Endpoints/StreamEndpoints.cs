using HyperRazor.Components.Services;
using HyperRazor.Demo.Components.Fragments;
using HyperRazor.Demo.Components.Pages;
using HyperRazor.Demo.Infrastructure;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace HyperRazor.Demo.Endpoints;

public static class StreamEndpoints
{
    public static IEndpointRouteBuilder MapAppStreams(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/streams/users/provision/{operationId}", StreamProvisioning);
        return endpoints;
    }

    private static IResult StreamProvisioning(
        string operationId,
        ProvisioningStore provisioning,
        WorkspaceCatalog workspaces,
        IHrzSseRenderer renderer,
        IHrzSwapService swapService,
        CancellationToken cancellationToken)
    {
        ProvisioningOperation? operation = null;
        foreach (var workspace in workspaces.All)
        {
            if (provisioning.TryGetOperation(operationId, workspace.Key, out operation))
            {
                break;
            }
        }

        if (operation is null)
        {
            return TypedResults.NotFound();
        }

        return HrzResults.ServerSentEvents(
            StreamProvisioningAsync(operation, provisioning, renderer, swapService, cancellationToken));
    }

    private static async IAsyncEnumerable<SseItem<string>> StreamProvisioningAsync(
        ProvisioningOperation operation,
        ProvisioningStore provisioning,
        IHrzSseRenderer renderer,
        IHrzSwapService swapService,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (operation.IsCompleted)
        {
            var completedDashboard = provisioning.GetDashboard(operation.WorkspaceKey);
            swapService.Replace<CountBadge>(
                UsersPage.UserCountRegion,
                new
                {
                    Count = completedDashboard.UserCount,
                    Label = "active identities"
                });

            swapService.Replace<StatusCard>(
                UsersPage.StatusRegion,
                new
                {
                    Label = "workflow",
                    Title = "Provisioning complete",
                    Detail = $"{operation.DisplayName} is already in the completed state.",
                    Tone = "success"
                });

            yield return await renderer.RenderComponent<ProvisionStepCard>(
                new
                {
                    StepNumber = 6,
                    Title = "Provisioning already complete",
                    Detail = $"{operation.DisplayName} already reached the stable completed state.",
                    Tone = "success",
                    Stamp = FormatStamp(DateTimeOffset.UtcNow)
                },
                id: $"{operation.OperationId}-complete",
                cancellationToken: cancellationToken);

            yield return HrzSse.Done();
            yield break;
        }

        var steps = new[]
        {
            new Step("Directory entry reserved", "Reserved the identity record and pinned the workspace routing key.", "info"),
            new Step("Account record created", "Created the account shell and attached baseline workspace metadata.", "info"),
            new Step("Default groups assigned", "Applied the starter access bundle and manager review scope.", "progress"),
            new Step("Welcome message queued", "Queued onboarding comms and first-day access instructions.", "progress"),
            new Step("Audit record written", "Stamped the request, reviewer, and environment context into the audit log.", "warning"),
            new Step("Provisioning complete", "All deterministic steps finished and the user summary is now stable.", "success")
        };

        for (var index = 0; index < steps.Length; index++)
        {
            var step = steps[index];
            QueueOobUpdates(index, operation, provisioning, swapService);

            yield return await renderer.RenderComponent<ProvisionStepCard>(
                new
                {
                    StepNumber = index + 1,
                    Title = step.Title,
                    Detail = step.Detail,
                    Tone = step.Tone,
                    Stamp = FormatStamp(DateTimeOffset.UtcNow)
                },
                id: $"{operation.OperationId}-{index + 1}",
                cancellationToken: cancellationToken);

            if (index < steps.Length - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(325), cancellationToken);
            }
        }

        yield return HrzSse.Done();
    }

    private static void QueueOobUpdates(
        int stepIndex,
        ProvisioningOperation operation,
        ProvisioningStore provisioning,
        IHrzSwapService swapService)
    {
        switch (stepIndex)
        {
            case 0:
                swapService.Replace<StatusCard>(
                    UsersPage.StatusRegion,
                    new
                    {
                        Label = "workflow",
                        Title = "Creating baseline account",
                        Detail = $"Preparing {operation.DisplayName} in {operation.WorkspaceName}.",
                        Tone = "progress"
                    });
                break;
            case 1:
                swapService.Replace<LatestInviteCard>(
                    UsersPage.LatestInviteRegion,
                    new
                    {
                        Summary = new InviteSummary(
                            operation.DisplayName,
                            operation.Email,
                            operation.AccessTier,
                            operation.Team,
                            operation.Manager,
                            "Provisioning",
                            FormatStamp(DateTimeOffset.UtcNow),
                            "info"),
                        WorkspaceName = operation.WorkspaceName
                    });
                break;
            case 2:
                swapService.Prepend<ActivityFeedItem>(
                    UsersPage.ActivityRegion,
                    $"{operation.OperationId}-queued",
                    new
                    {
                        Entry = new ActivityEntry(
                            $"Queued {operation.DisplayName}",
                            $"Assigned the {operation.AccessTier} starter bundle and manager review chain.",
                            FormatStamp(DateTimeOffset.UtcNow),
                            "info")
                    });
                break;
            case 4:
                swapService.Replace<StatusCard>(
                    UsersPage.StatusRegion,
                    new
                    {
                        Label = "workflow",
                        Title = "Finalizing audit trail",
                        Detail = "Recording the request context before the completed state is published.",
                        Tone = "warning"
                    });
                break;
            case 5:
                var dashboard = provisioning.CompleteOperation(operation.OperationId);

                swapService.Replace<CountBadge>(
                    UsersPage.UserCountRegion,
                    new
                    {
                        Count = dashboard.UserCount,
                        Label = "active identities"
                    });

                swapService.Replace<LatestInviteCard>(
                    UsersPage.LatestInviteRegion,
                    new
                    {
                        Summary = dashboard.LatestInvite,
                        WorkspaceName = operation.WorkspaceName
                    });

                if (dashboard.Activity.Count > 0)
                {
                    swapService.Prepend<ActivityFeedItem>(
                        UsersPage.ActivityRegion,
                        $"{operation.OperationId}-complete",
                        new
                        {
                            Entry = dashboard.Activity[0]
                        });
                }

                swapService.Replace<StatusCard>(
                    UsersPage.StatusRegion,
                    new
                    {
                        Label = "workflow",
                        Title = "Stable completed state",
                        Detail = $"{operation.DisplayName} is fully provisioned and the stream will close cleanly.",
                        Tone = "success"
                    });
                break;
        }
    }

    private static string FormatStamp(DateTimeOffset value)
    {
        return value.ToLocalTime().ToString("MMM d, h:mm:ss tt");
    }

    private sealed record Step(string Title, string Detail, string Tone);
}
