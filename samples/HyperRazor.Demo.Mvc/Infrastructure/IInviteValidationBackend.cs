using HyperRazor.Demo.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public interface IInviteValidationBackend
{
    int InvocationCount { get; }

    void Reset();

    Task<InviteValidationBackendResult> SubmitAsync(InviteUserInput input, CancellationToken cancellationToken);
}

public sealed record InviteValidationBackendResult(
    bool IsSuccess,
    int Count,
    ValidationProblemDetails? ProblemDetails);
