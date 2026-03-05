using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
using HyperRazor.Htmx.AspNetCore;
using HyperRazor.Mvc;
using HyperRazor.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
[Route("fragments")]
public sealed class FragmentsController : HrController
{
    private static int _userCount = 5;
    private readonly IHrxSwapService _swapService;

    public FragmentsController(IHrxSwapService swapService)
    {
        _swapService = swapService ?? throw new ArgumentNullException(nameof(swapService));
    }

    [HttpGet("users/search")]
    [HtmxRequest]
    public Task<IResult> SearchUsers(
        [FromQuery] string? query,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 4,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query?.Trim();
        var normalizedSort = string.IsNullOrWhiteSpace(sort) ? "name-asc" : sort.Trim();
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 10);

        QueueInspectorUpdate(
            action: "search-users",
            details: $"query=\"{normalizedQuery ?? "*"}\", sort={normalizedSort}, page={safePage}, pageSize={safePageSize}");

        return PartialView<UserSearchResults>(new
        {
            Query = normalizedQuery,
            Sort = normalizedSort,
            Page = safePage,
            PageSize = safePageSize
        }, cancellationToken);
    }

    [HttpGet("toast/success")]
    public Task<IResult> ToastSuccessFragment(CancellationToken cancellationToken)
    {
        HttpContext.HtmxResponse().Trigger("toast:show", new
        {
            message = "Saved successfully."
        });

        QueueInspectorUpdate(
            action: "toast-success",
            details: "Emitted HX-Trigger response event: toast:show");

        return PartialView<ToastSuccess>(new
        {
            Message = "Saved successfully."
        }, cancellationToken);
    }

    [HttpGet("toast/success-attribute")]
    [HtmxResponse(Trigger = "toast:show")]
    public Task<IResult> ToastSuccessAttributeFragment(CancellationToken cancellationToken)
    {
        QueueInspectorUpdate(
            action: "toast-success-attribute",
            details: "Trigger is configured via [HtmxResponse]; response headers are applied after action execution.");

        return PartialView<ToastSuccess>(new
        {
            Message = "Saved successfully (attribute trigger)."
        }, cancellationToken);
    }

    [HttpPost("users/create")]
    public Task<IResult> CreateUser([FromForm] string? displayName, CancellationToken cancellationToken)
    {
        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? "New User"
            : displayName.Trim();
        var count = Interlocked.Increment(ref _userCount);

        QueueUserCreatedSwaps(normalizedName, count, includeUsersList: false);

        HttpContext.HtmxResponse().Trigger("users:created", new
        {
            name = normalizedName,
            count
        });

        QueueInspectorUpdate(
            action: "create-user",
            details: $"Created {normalizedName} (#{count}).");

        return PartialView<UserCreateResult>(new { DisplayName = normalizedName, Count = count }, cancellationToken);
    }

    [HttpPost("users/create-rendered")]
    [HtmxRequest]
    public async Task<IResult> CreateUserRendered([FromForm] string? displayName, CancellationToken cancellationToken)
    {
        var normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? "New User"
            : displayName.Trim();
        var count = Interlocked.Increment(ref _userCount);

        QueueUserCreatedSwaps(normalizedName, count, includeUsersList: true);

        HttpContext.HtmxResponse().Trigger("users:created", new
        {
            name = normalizedName,
            count,
            source = "render-to-string"
        });

        QueueInspectorUpdate(
            action: "create-user-rendered",
            details: $"Created {normalizedName} (#{count}) via IHrxSwapService.RenderToString(clear: true).");

        var oobMarkup = await _swapService.RenderToString(clear: true, cancellationToken);
        var preview = BuildRenderToStringPreview(oobMarkup);

        return await PartialView(
            cancellationToken,
            builder =>
            {
                builder.OpenComponent<RenderToStringDemoResult>(0);
                builder.AddAttribute(1, nameof(RenderToStringDemoResult.DisplayName), normalizedName);
                builder.AddAttribute(2, nameof(RenderToStringDemoResult.Count), count);
                builder.AddAttribute(3, nameof(RenderToStringDemoResult.MarkupLength), oobMarkup.Length);
                builder.AddAttribute(4, nameof(RenderToStringDemoResult.MarkupPreview), preview);
                builder.CloseComponent();
            },
            builder => builder.AddMarkupContent(0, oobMarkup));
    }

    [HttpPost("users/create-validated")]
    [HtmxRequest]
    public Task<IResult> CreateUserValidated(
        [FromForm] string? displayName,
        [FromForm] string? email,
        CancellationToken cancellationToken)
    {
        var normalizedName = displayName?.Trim() ?? string.Empty;
        var normalizedEmail = email?.Trim() ?? string.Empty;
        var errors = ValidateCreateUserInput(normalizedName, normalizedEmail);

        if (errors.Count > 0)
        {
            HttpContext.HtmxResponse().Trigger("form:invalid", new
            {
                errorCount = errors.Count
            });

            QueueInspectorUpdate(
                action: "validate-user",
                details: $"Invalid submission with {errors.Count} error(s).");

            return PartialView<UserCreateValidationResult>(new
            {
                Success = false,
                DisplayName = normalizedName,
                Email = normalizedEmail,
                Errors = errors
            }, cancellationToken);
        }

        var count = Interlocked.Increment(ref _userCount);
        HttpContext.HtmxResponse().Trigger("form:valid", new
        {
            name = normalizedName,
            email = normalizedEmail,
            count
        });

        QueueInspectorUpdate(
            action: "validate-user",
            details: $"Valid submission for {normalizedName} ({normalizedEmail}).");

        return PartialView<UserCreateValidationResult>(new
        {
            Success = true,
            DisplayName = normalizedName,
            Email = normalizedEmail,
            Count = count
        }, cancellationToken);
    }

    [HttpPost("navigation/soft")]
    public IActionResult SoftRedirect()
    {
        HttpContext.HtmxResponse().Location(new
        {
            path = "/",
            target = "#hrx-main-layout",
            swap = "innerHTML"
        });
        return NoContent();
    }

    [HttpPost("navigation/hard")]
    public IActionResult HardRedirect()
    {
        HttpContext.HtmxResponse().Redirect("/");
        return NoContent();
    }

    private static List<string> ValidateCreateUserInput(string displayName, string email)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors.Add("Display name is required.");
        }
        else if (displayName.Length < 3)
        {
            errors.Add("Display name must be at least 3 characters.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add("Email is required.");
        }
        else
        {
            var atIndex = email.IndexOf('@');
            var dotIndex = email.LastIndexOf('.');
            var looksValid = atIndex > 0 && dotIndex > atIndex + 1 && dotIndex < email.Length - 1;

            if (!looksValid)
            {
                errors.Add("Email must be a valid address.");
            }
        }

        return errors;
    }

    private void QueueUserCreatedSwaps(string normalizedName, int count, bool includeUsersList)
    {
        if (includeUsersList)
        {
            _swapService.AddSwappableComponent<UserCreateResult>(
                targetId: "users-list",
                parameters: new Dictionary<string, object?>
                {
                    [nameof(UserCreateResult.DisplayName)] = normalizedName,
                    [nameof(UserCreateResult.Count)] = count
                },
                swapStyle: SwapStyle.OuterHtml);
        }

        _swapService.AddSwappableComponent<ToastSuccess>(
            targetId: $"toast-{count}",
            parameters: new Dictionary<string, object?>
            {
                [nameof(Components.Fragments.ToastSuccess.Message)] = $"Created {normalizedName}."
            },
            swapStyle: SwapStyle.BeforeEnd,
            selector: "#toast-stack");

        _swapService.AddSwappableComponent<UserCountValue>(
            targetId: "user-count-shell",
            parameters: new Dictionary<string, object?>
            {
                [nameof(UserCountValue.Count)] = count
            },
            swapStyle: SwapStyle.InnerHtml);

        _swapService.AddSwappableComponent<ActivityFeedItem>(
            targetId: $"activity-{count}",
            parameters: new Dictionary<string, object?>
            {
                [nameof(ActivityFeedItem.DisplayName)] = normalizedName,
                [nameof(ActivityFeedItem.Count)] = count
            },
            swapStyle: SwapStyle.BeforeEnd,
            selector: "#activity-feed");
    }

    private void QueueInspectorUpdate(string action, string details)
    {
        _swapService.AddSwappableComponent<HxRequestResponseInspector>(
            targetId: "hx-debug-shell",
            parameters: BuildInspectorParameters(HttpContext, action, details),
            swapStyle: SwapStyle.OuterHtml);
    }

    private static string BuildRenderToStringPreview(string markup)
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            return "(empty)";
        }

        const int maxLength = 480;
        var compact = markup
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace('\n', ' ');

        if (compact.Length <= maxLength)
        {
            return compact;
        }

        return $"{compact[..maxLength]}...";
    }

    private static IReadOnlyDictionary<string, object?> BuildInspectorParameters(
        HttpContext context,
        string action,
        string details)
    {
        var request = context.Request.Headers;
        var response = context.Response.Headers;
        var route = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";

        return new Dictionary<string, object?>
        {
            [nameof(HxRequestResponseInspector.ActionName)] = action,
            [nameof(HxRequestResponseInspector.Details)] = details,
            [nameof(HxRequestResponseInspector.Route)] = route,
            [nameof(HxRequestResponseInspector.HxRequest)] = ReadHeader(request, HtmxHeaderNames.Request),
            [nameof(HxRequestResponseInspector.HxTarget)] = ReadHeader(request, HtmxHeaderNames.Target),
            [nameof(HxRequestResponseInspector.HxTrigger)] = ReadHeader(request, HtmxHeaderNames.Trigger),
            [nameof(HxRequestResponseInspector.HxCurrentUrl)] = ReadHeader(request, HtmxHeaderNames.CurrentUrl),
            [nameof(HxRequestResponseInspector.HxTriggerResponse)] = ReadHeader(response, HtmxHeaderNames.TriggerResponse),
            [nameof(HxRequestResponseInspector.HxRedirect)] = ReadHeader(response, HtmxHeaderNames.Redirect),
            [nameof(HxRequestResponseInspector.HxLocation)] = ReadHeader(response, HtmxHeaderNames.Location),
            [nameof(HxRequestResponseInspector.StatusCode)] = context.Response.StatusCode
        };
    }

    private static string ReadHeader(IHeaderDictionary headers, string key)
    {
        if (!headers.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return "(none)";
        }

        return value.ToString();
    }
}
