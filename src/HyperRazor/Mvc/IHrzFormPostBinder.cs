using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Mvc;

public interface IHrzFormPostBinder
{
    Task<HrzFormPostState<TModel>> BindAsync<TModel>(
        HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new();

    Task<HrzFormPostState<TModel>> BindAndValidateAsync<TModel>(
        HttpContext context,
        HrzValidationRootId rootId,
        CancellationToken cancellationToken = default)
        where TModel : class, new();
}
