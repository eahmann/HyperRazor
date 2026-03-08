using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Controllers;

public sealed class UsersController : HrController
{
    private static int _inviteCount = 5;

    [HttpPost("/users/invite")]
    public Task<IResult> Invite([FromForm] InviteUserInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TriggerInvalid(ModelState.ErrorCount);
            SetSubmitValidationState(UserInviteValidationRoots.MvcLocal);
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
        TriggerValid(input, count);
        DemoInspectorUpdates.Queue(
            HttpContext,
            action: "users-invite-valid",
            details: $"Invite validated locally and created {input.DisplayName} (#{count}).");

        if (HttpContext.HtmxRequest().IsHtmx)
        {
            return PartialView<UserInviteValidationForm>(
                new
                {
                    Form = UserInviteValidationDefinitions.MvcLocal(input, success: true, count: count)
                },
                cancellationToken);
        }

        return Task.FromResult<IResult>(Results.Redirect("/users"));
    }

    private void TriggerInvalid(int errorCount)
    {
        HttpContext.HtmxResponse().Trigger("form:invalid", new
        {
            errorCount
        });
    }

    private void TriggerValid(InviteUserInput input, int count)
    {
        HttpContext.HtmxResponse().Trigger("form:valid", new
        {
            name = input.DisplayName,
            email = input.Email,
            count
        });
    }

    private void SetSubmitValidationState(HrzValidationRootId rootId)
    {
        var resolver = HttpContext.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
        HttpContext.SetSubmitValidationState(ModelState.ToSubmitValidationState(
            rootId,
            resolver,
            HrzAttemptedValues.FromRequest(Request)));
    }
}
