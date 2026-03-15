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
            DemoValidationFeedback.TriggerInvalid(HttpContext, ModelState.ErrorCount);
            DemoValidationFeedback.SetSubmitValidationState(HttpContext, ModelState, UserInviteValidationRoots.MvcProxy);
            DemoInspectorUpdates.Queue(
                HttpContext,
                action: "validation-mvc-proxy-invalid",
                details: $"MVC proxy validation failed locally with {ModelState.ErrorCount} error(s) before the backend call.");
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
            DemoValidationFeedback.TriggerInvalid(HttpContext, HttpContext.GetSubmitValidationState(UserInviteValidationRoots.MvcProxy)?.FieldErrors.Count ?? 1);
            DemoInspectorUpdates.Queue(
                HttpContext,
                action: "validation-mvc-proxy-backend-invalid",
                details: "MVC proxy mapped backend validation JSON back into the server-rendered form fragment.");

            return await UserInviteValidationResponses.RenderValidationAsync(
                HttpContext,
                nameof(Components.Pages.Admin.ValidationPage.MvcProxyInviteForm),
                UserInviteValidationDefinitions.MvcProxy(input),
                cancellationToken);
        }

        DemoValidationFeedback.TriggerValid(HttpContext, input, backendResult.Count);
        DemoInspectorUpdates.Queue(
            HttpContext,
            action: "validation-mvc-proxy-valid",
            details: $"MVC proxy validated successfully and the backend accepted {input.DisplayName} (#{backendResult.Count}).");
        if (HttpContext.HtmxRequest().IsHtmx)
        {
            return await Fragment<UserInviteValidationForm>(
                new
                {
                    Form = UserInviteValidationDefinitions.MvcProxy(input, success: true, count: backendResult.Count)
                },
                cancellationToken);
        }

        return Results.Redirect("/validation");
    }
}
