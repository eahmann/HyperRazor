using HyperRazor.Demo.Mvc.Models;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public sealed class DemoInviteValidationBackend : IInviteValidationBackend
{
    private int _count = 40;
    private int _invocationCount;

    public int InvocationCount => _invocationCount;

    public void Reset()
    {
        Interlocked.Exchange(ref _invocationCount, 0);
    }

    public Task<InviteValidationBackendResult> SubmitAsync(InviteUserInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _invocationCount);

        if (string.Equals(input.Email, "backend-taken@example.com", StringComparison.OrdinalIgnoreCase))
        {
            var problemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["email"] = ["Email already exists in the upstream directory."],
                [string.Empty] = ["Upstream directory rejected the invite."]
            })
            {
                Title = "Invite rejected by backend policy.",
                Status = StatusCodes.Status422UnprocessableEntity
            };

            return Task.FromResult(new InviteValidationBackendResult(false, 0, problemDetails));
        }

        var count = Interlocked.Increment(ref _count);
        return Task.FromResult(new InviteValidationBackendResult(true, count, null));
    }
}
