using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
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
            HttpContext.HtmxResponse().Trigger("form:invalid", new
            {
                errorCount = ModelState.ErrorCount
            });

            if (HttpContext.HtmxRequest().IsHtmx)
            {
                return PartialView<UserInviteValidationForm>(
                    new
                    {
                        Input = input
                    },
                    cancellationToken,
                    UserInviteValidationForm.RootId);
            }

            return View<UsersPage>(
                new
                {
                    InviteInput = input
                },
                cancellationToken,
                UserInviteValidationForm.RootId);
        }

        var count = Interlocked.Increment(ref _inviteCount);
        HttpContext.HtmxResponse().Trigger("form:valid", new
        {
            name = input.DisplayName,
            email = input.Email,
            count
        });

        if (HttpContext.HtmxRequest().IsHtmx)
        {
            return PartialView<UserInviteValidationForm>(
                new
                {
                    Input = input,
                    Success = true,
                    Count = count
                },
                cancellationToken);
        }

        return Task.FromResult<IResult>(Results.Redirect("/users"));
    }
}
