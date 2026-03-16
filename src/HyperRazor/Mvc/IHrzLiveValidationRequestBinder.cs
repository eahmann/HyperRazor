using HyperRazor.Components.Validation;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Mvc;

public interface IHrzLiveValidationRequestBinder
{
    Task<HrzLiveValidationRequest?> BindAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}
