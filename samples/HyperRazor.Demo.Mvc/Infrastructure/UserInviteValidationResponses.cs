using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Components.Pages.Admin;
using HyperRazor.Demo.Mvc.Models;
using HyperRazor.Htmx;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Components;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public static class UserInviteValidationResponses
{
    public static Task<IResult> RenderUsersAsync(
        HttpContext context,
        string usersPageParameterName,
        InviteValidationFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        return RenderAsync<UsersPage>(
            context,
            new Dictionary<string, object?>
            {
                [usersPageParameterName] = form
            },
            form,
            cancellationToken);
    }

    public static Task<IResult> RenderValidationAsync(
        HttpContext context,
        string validationPageParameterName,
        InviteValidationFormViewModel form,
        CancellationToken cancellationToken = default)
    {
        return RenderAsync<ValidationPage>(
            context,
            new Dictionary<string, object?>
            {
                [validationPageParameterName] = form
            },
            form,
            cancellationToken);
    }

    private static Task<IResult> RenderAsync<TPage>(
        HttpContext context,
        IReadOnlyDictionary<string, object?> pageParameters,
        InviteValidationFormViewModel form,
        CancellationToken cancellationToken)
        where TPage : IComponent
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(pageParameters);
        ArgumentNullException.ThrowIfNull(form);

        if (context.HtmxRequest().IsHtmx)
        {
            return HrzResults.Validation<UserInviteValidationForm>(
                context,
                new
                {
                    Form = form
                },
                cancellationToken: cancellationToken);
        }

        return HrzResults.Page<TPage>(
            context,
            pageParameters,
            cancellationToken: cancellationToken);
    }
}
