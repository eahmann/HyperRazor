using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace HyperRazor.Demo.Mvc.Controllers;

public sealed class ValidationController : HrController
{
    private readonly IInviteValidationBackend _inviteValidationBackend;

    public ValidationController(IInviteValidationBackend inviteValidationBackend)
    {
        _inviteValidationBackend = inviteValidationBackend ?? throw new ArgumentNullException(nameof(inviteValidationBackend));
    }

    [HttpPost("/validation/mvc-proxy")]
    public async Task<IResult> InviteProxy([FromForm] InviteUserInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TriggerInvalid(ModelState.ErrorCount);
            SetSubmitValidationState(UserInviteValidationRoots.MvcProxy);
            return await UserInviteValidationResponses.RenderValidationAsync(
                HttpContext,
                nameof(Components.Pages.Admin.ValidationPage.MvcProxyInviteForm),
                UserInviteValidationDefinitions.MvcProxy(input),
                cancellationToken);
        }

        var backendResult = await _inviteValidationBackend.SubmitAsync(input, cancellationToken);
        if (!backendResult.IsSuccess)
        {
            var resolver = HttpContext.RequestServices.GetRequiredService<IHrzFieldPathResolver>();
            HttpContext.SetSubmitValidationState(backendResult.ProblemDetails!.ToSubmitValidationState(
                UserInviteValidationRoots.MvcProxy,
                resolver,
                HrzAttemptedValues.FromRequest(Request)));
            TriggerInvalid(HttpContext.GetSubmitValidationState(UserInviteValidationRoots.MvcProxy)?.FieldErrors.Count ?? 1);

            return await UserInviteValidationResponses.RenderValidationAsync(
                HttpContext,
                nameof(Components.Pages.Admin.ValidationPage.MvcProxyInviteForm),
                UserInviteValidationDefinitions.MvcProxy(input),
                cancellationToken);
        }

        TriggerValid(input, backendResult.Count);
        if (HttpContext.HtmxRequest().IsHtmx)
        {
            return await PartialView<UserInviteValidationForm>(
                new
                {
                    Form = UserInviteValidationDefinitions.MvcProxy(input, success: true, count: backendResult.Count)
                },
                cancellationToken);
        }

        return Results.Redirect("/validation");
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
