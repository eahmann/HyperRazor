using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

public sealed class UsersController : HrController
{
    private static int _inviteCount = 5;

    [HttpPost("/users/invite")]
    public Task<IResult> Invite([FromForm] InviteUserInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            DemoValidationFeedback.TriggerInvalid(HttpContext, ModelState.ErrorCount);
            DemoValidationFeedback.SetSubmitValidationState(HttpContext, ModelState, UserInviteValidationRoots.MvcLocal);
            DemoInspectorUpdates.Queue(
                HttpContext,
                action: "users-invite-invalid",
                details: $"Invite validation failed locally with {ModelState.ErrorCount} error(s).");
            return UserInviteValidationResponses.RenderUsersAsync(
                HttpContext,
                nameof(Components.Pages.Admin.UsersPage.MvcInviteForm),
                UserInviteValidationDefinitions.MvcLocal(input),
                cancellationToken);
        }

        var count = Interlocked.Increment(ref _inviteCount);
        DemoValidationFeedback.TriggerValid(HttpContext, input, count);
        DemoInspectorUpdates.Queue(
            HttpContext,
            action: "users-invite-valid",
            details: $"Invite validated locally and created {input.DisplayName} (#{count}).");

        if (HttpContext.HtmxRequest().IsHtmx)
        {
            return Partial<UserInviteValidationForm>(
                new
                {
                    Form = UserInviteValidationDefinitions.MvcLocal(input, success: true, count: count)
                },
                cancellationToken);
        }

        return Task.FromResult<IResult>(Results.Redirect("/users"));
    }
}
