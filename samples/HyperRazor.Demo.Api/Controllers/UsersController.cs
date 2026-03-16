using HyperRazor.Demo.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private static readonly UserDto[] Users =
    [
        new(1, "asmith", "Alex Smith"),
        new(2, "bjohnson", "Bailey Johnson"),
        new(3, "cnguyen", "Casey Nguyen"),
        new(4, "dpatel", "Drew Patel"),
        new(5, "emartinez", "Emerson Martinez")
    ];

    [HttpGet]
    public ActionResult<IReadOnlyList<UserDto>> Get([FromQuery] string? query = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Users);
        }

        var term = query.Trim();
        var matches = Users
            .Where(user =>
                user.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || user.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Ok(matches);
    }

    [HttpPost("validate")]
    public ActionResult<UsernameValidationResponse> Validate([FromBody] UsernameValidationRequest request)
    {
        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
        {
            return Ok(new UsernameValidationResponse(false, "Username is required."));
        }

        if (userName.Length < 3)
        {
            return Ok(new UsernameValidationResponse(false, "Username must be at least 3 characters."));
        }

        var exists = Users.Any(user => user.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
        return exists
            ? Ok(new UsernameValidationResponse(false, "Username is already taken."))
            : Ok(new UsernameValidationResponse(true, "Username is available."));
    }
}
