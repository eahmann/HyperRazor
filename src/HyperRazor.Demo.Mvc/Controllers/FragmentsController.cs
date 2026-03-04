using System.Text;
using HyperRazor.Htmx.AspNetCore;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
[Route("fragments")]
public sealed class FragmentsController : ControllerBase
{
    private static readonly (string UserName, string DisplayName)[] Users =
    [
        ("asmith", "Alex Smith"),
        ("bjohnson", "Bailey Johnson"),
        ("cnguyen", "Casey Nguyen"),
        ("dpatel", "Drew Patel"),
        ("emartinez", "Emerson Martinez")
    ];

    [HttpGet("users/search")]
    public IActionResult SearchUsers([FromQuery] string? query)
    {
        var term = query?.Trim();
        var results = string.IsNullOrWhiteSpace(term)
            ? Users
            : Users.Where(user =>
                user.UserName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || user.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));

        return Content(BuildUsersList(results), "text/html");
    }

    [HttpGet("toast/success")]
    public IActionResult ToastSuccess()
    {
        HttpContext.HtmxResponse().Trigger("toast:show", new { message = "Saved successfully." });
        return Content("<div class=\"toast success\">Saved successfully.</div>", "text/html");
    }

    [HttpPost("navigation/soft")]
    public IActionResult SoftRedirect()
    {
        HttpContext.HtmxResponse().Location("/");
        return NoContent();
    }

    [HttpPost("navigation/hard")]
    public IActionResult HardRedirect()
    {
        HttpContext.HtmxResponse().Redirect("/");
        return NoContent();
    }

    private static string BuildUsersList(IEnumerable<(string UserName, string DisplayName)> users)
    {
        var items = users.ToArray();
        if (items.Length == 0)
        {
            return "<p class=\"empty\">No users found.</p>";
        }

        var builder = new StringBuilder("<ul class=\"users\">");
        foreach (var user in items)
        {
            builder
                .Append("<li><strong>")
                .Append(System.Net.WebUtility.HtmlEncode(user.DisplayName))
                .Append("</strong> <span>@")
                .Append(System.Net.WebUtility.HtmlEncode(user.UserName))
                .Append("</span></li>");
        }

        builder.Append("</ul>");
        return builder.ToString();
    }
}
