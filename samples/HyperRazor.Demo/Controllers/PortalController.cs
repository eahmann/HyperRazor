using HyperRazor.Demo.Components.Fragments;
using HyperRazor.Demo.Components.Pages;
using HyperRazor.Demo.Infrastructure;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Controllers;

public sealed class PortalController : HrController
{
    private readonly AppViewModelFactory _models;
    private readonly WorkspaceCatalog _workspaces;

    public PortalController(AppViewModelFactory models, WorkspaceCatalog workspaces)
    {
        _models = models ?? throw new ArgumentNullException(nameof(models));
        _workspaces = workspaces ?? throw new ArgumentNullException(nameof(workspaces));
    }

    [HttpPost("/portal/enter")]
    public Task<IResult> Enter([FromForm] PortalEntryInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        Normalize(input);
        Validate(input, out var workspace);

        if (!ModelState.IsValid)
        {
            var model = _models.CreatePortalPage(input.Workspace, ModelState, input);
            if (HttpContext.HtmxRequest().IsHtmx)
            {
                ValidationFeedback.SetSubmitValidationState(HttpContext, ModelState, AppValidationRoots.PortalEntry);
                return HrzResults.Validation<PortalEntryCard>(
                    HttpContext,
                    new
                    {
                        Form = model.Form
                    },
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    cancellationToken: cancellationToken);
            }

            return Page<PortalPage>(new { Model = model }, cancellationToken, AppValidationRoots.PortalEntry);
        }

        return Task.FromResult(WorkflowResults.EnterConsole(HttpContext, workspace!.Key));
    }

    private void Validate(PortalEntryInput input, out WorkspaceInfo? workspace)
    {
        workspace = null;

        if (!_workspaces.TryResolve(input.Workspace, out var resolvedWorkspace))
        {
            ModelState.AddModelError(nameof(PortalEntryInput.Workspace), "Select a valid workspace to enter the console.");
            return;
        }

        workspace = resolvedWorkspace;
        input.Workspace = resolvedWorkspace.Key;

        if (!input.Email.EndsWith("@example.com", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(PortalEntryInput.Email), "Use an @example.com address for the demo portal.");
        }

        if (!_workspaces.MatchesAccessCode(resolvedWorkspace.Key, input.AccessCode))
        {
            ModelState.AddModelError(nameof(PortalEntryInput.AccessCode), $"Use the workspace access code for {resolvedWorkspace.Name}.");
        }
    }

    private static void Normalize(PortalEntryInput input)
    {
        input.Email = input.Email.Trim();
        input.AccessCode = input.AccessCode.Trim();
        input.Workspace = input.Workspace.Trim();
    }
}
