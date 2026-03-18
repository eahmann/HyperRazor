using Microsoft.AspNetCore.Http;

namespace HyperRazor.Mvc;

internal sealed class HrzStatusCodeResult : IResult
{
    private readonly int _statusCode;
    private readonly IResult _inner;

    public HrzStatusCodeResult(int statusCode, IResult inner)
    {
        _statusCode = statusCode;
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = _statusCode;
        await _inner.ExecuteAsync(httpContext);
    }
}
