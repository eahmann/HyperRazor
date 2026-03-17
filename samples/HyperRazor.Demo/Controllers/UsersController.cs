using HyperRazor.Components.Services;
using HyperRazor.Demo.Components.Fragments;
using HyperRazor.Demo.Components.Pages;
using HyperRazor.Demo.Infrastructure;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Controllers;

public sealed class UsersController : HrController
{
    private readonly PeopleDirectoryService _directory;
    private readonly AppViewModelFactory _models;
    private readonly ProvisioningStore _provisioning;
    private readonly IHrzSwapService _swapService;
    private readonly WorkspaceCatalog _workspaces;

    public UsersController(
        PeopleDirectoryService directory,
        AppViewModelFactory models,
        ProvisioningStore provisioning,
        IHrzSwapService swapService,
        WorkspaceCatalog workspaces)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _models = models ?? throw new ArgumentNullException(nameof(models));
        _provisioning = provisioning ?? throw new ArgumentNullException(nameof(provisioning));
        _swapService = swapService ?? throw new ArgumentNullException(nameof(swapService));
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
    }

    [HttpGet("/fragments/users/search")]
    [HtmxRequest]
    public Task<IResult> Search(
        [FromQuery] string? workspace,
        [FromQuery] string? query,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var resolvedWorkspace = _workspaces.Resolve(workspace);
        var result = _directory.Search(resolvedWorkspace.Key, query, status);

        return Fragment<DirectoryResults>(
            new
            {
                Result = result
            },
            cancellationToken);
    }

    [HttpPost("/fragments/users/invite")]
    public Task<IResult> Invite([FromForm] InviteUserInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        Normalize(input);
        var workspace = _workspaces.Resolve(input.Workspace);
        input.Workspace = workspace.Key;

        Validate(input, workspace);

        if (!ModelState.IsValid)
        {
            var model = _models.CreateUsersPage(workspace.Key, ModelState, input);
            if (HttpContext.HtmxRequest().IsHtmx)
            {
                ValidationFeedback.SetSubmitValidationState(HttpContext, ModelState, AppValidationRoots.UserInvite);
                return HrzResults.Validation<InviteFormCard>(
                    HttpContext,
                    new
                    {
                        Form = model.Form
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    cancellationToken: cancellationToken);
            }

            return Page<UsersPage>(new { Model = model }, cancellationToken, AppValidationRoots.UserInvite);
        }

        var operation = _provisioning.StartOperation(workspace, input);
        _swapService.Replace<StatusCard>(
            UsersPage.StatusRegion,
            new
            {
                Label = "workflow",
                Title = "Provisioning queued",
                Detail = $"{operation.DisplayName} is waiting for the deterministic provisioning stream to start.",
                Tone = "progress"
            });

        return WorkflowResults.StartProvisioningAsync(HttpContext, operation, cancellationToken);
    }

    private void Validate(InviteUserInput input, WorkspaceInfo workspace)
    {
        if (!input.Email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(InviteUserInput.Email), "Use an @example.com address for the internal demo directory.");
        }

        if (string.Equals(input.DisplayName, input.Manager, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(InviteUserInput.Manager), "Manager approval must come from someone else.");
        }

        if (input.StartDate is not null && input.StartDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(InviteUserInput.StartDate), "Choose a start date today or later.");
        }

        if (string.Equals(input.AccessTier, "Privileged", StringComparison.OrdinalIgnoreCase)
            && input.Justification.Length < 24)
        {
            ModelState.AddModelError(nameof(InviteUserInput.Justification), "Privileged access needs a more specific business justification.");
        }

        if (string.Equals(workspace.Key, "atlas", StringComparison.OrdinalIgnoreCase)
            && string.Equals(input.Team, "Platform", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(InviteUserInput.Team), "Atlas Finance demo identities should stay within finance-adjacent teams.");
        }
    }

    private static void Normalize(InviteUserInput input)
    {
        input.DisplayName = input.DisplayName.Trim();
        input.Email = input.Email.Trim();
        input.Team = input.Team.Trim();
        input.AccessTier = input.AccessTier.Trim();
        input.Manager = input.Manager.Trim();
        input.Justification = input.Justification.Trim();
        input.Workspace = input.Workspace.Trim();
    }
}
