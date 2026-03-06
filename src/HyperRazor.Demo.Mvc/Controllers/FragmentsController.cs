using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
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
    private readonly IHrzHeadService _headService;
    private readonly IHrzSwapService _swapService;

    public FragmentsController(IHrzSwapService swapService, IHrzHeadService headService)
    {
        _swapService = swapService ?? throw new ArgumentNullException(nameof(swapService));
        _headService = headService ?? throw new ArgumentNullException(nameof(headService));
    }

    [HttpPost("chrome/theme")]
    public IResult SetTheme([FromForm] string? theme, [FromForm] string? returnUrl)
    {
        var normalizedTheme = DemoChromeState.NormalizeTheme(theme);
        DemoChromeState.WriteThemeCookie(HttpContext, normalizedTheme);

        if (HttpContext.HtmxRequest().IsHtmx)
        {
            HttpContext.HtmxResponse().Refresh();
            return Results.Content(string.Empty, "text/html; charset=utf-8");
        }

        return Results.Redirect(DemoChromeState.NormalizeReturnUrl(returnUrl));
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

    [HttpGet("dashboard/sync-check")]
    public Task<IResult> DashboardSyncCheck(CancellationToken cancellationToken)
    {
        QueueDashboardEventLog("Saved successfully.");

        HttpContext.HtmxResponse().Trigger("toast:show", new
        {
            message = "Saved successfully."
        });

        QueueInspectorUpdate(
            action: "dashboard-sync-check",
            details: "Emitted an HX-Trigger workflow event from the dashboard health check.");

        return PartialView<DashboardCheckResult>(new
        {
            Title = "Sync check completed",
            Message = "Saved successfully.",
            TriggerSource = "action-body"
        }, cancellationToken);
    }

    [HttpGet("dashboard/banner-check")]
    [HtmxResponse(Trigger = "toast:show")]
    public Task<IResult> DashboardBannerCheck(CancellationToken cancellationToken)
    {
        QueueDashboardEventLog("Saved successfully (attribute trigger).");

        QueueInspectorUpdate(
            action: "dashboard-banner-check",
            details: "Trigger is configured via [HtmxResponse] for a dashboard broadcast event.");

        return PartialView<DashboardCheckResult>(new
        {
            Title = "Broadcast banner queued",
            Message = "Saved successfully (attribute trigger).",
            TriggerSource = "[HtmxResponse]"
        }, cancellationToken);
    }

    [HttpGet("incidents/drills/auth")]
    [HtmxRequest]
    public Task<IResult> IncidentAuthDrill(CancellationToken cancellationToken)
    {
        QueueInspectorUpdate(
            action: "incident-drill-auth",
            details: "Returned a 401 response fragment for the incident authentication drill.");

        return HrzResults.Unauthorized<ErrorStatusResult>(
            HttpContext,
            new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Title = "401 Unauthorized",
                Message = "Authentication is required before this action can proceed."
            },
            cancellationToken: cancellationToken);
    }

    [HttpGet("incidents/drills/permission")]
    [HtmxRequest]
    public Task<IResult> IncidentPermissionDrill(CancellationToken cancellationToken)
    {
        QueueInspectorUpdate(
            action: "incident-drill-permission",
            details: "Returned a 403 response fragment for the incident permission drill.");

        return HrzResults.Forbidden<ErrorStatusResult>(
            HttpContext,
            new
            {
                StatusCode = StatusCodes.Status403Forbidden,
                Title = "403 Forbidden",
                Message = "You are authenticated, but this operation is not allowed."
            },
            cancellationToken: cancellationToken);
    }

    [HttpGet("incidents/drills/playbook-missing")]
    [HtmxRequest]
    public Task<IResult> IncidentPlaybookMissingDrill(CancellationToken cancellationToken)
    {
        QueueInspectorUpdate(
            action: "incident-drill-playbook-missing",
            details: "Returned a 404 response fragment for the missing incident playbook drill.");

        return HrzResults.NotFound<ErrorStatusResult>(
            HttpContext,
            new
            {
                StatusCode = StatusCodes.Status404NotFound,
                Title = "404 Not Found",
                Message = "The requested resource could not be located."
            },
            cancellationToken: cancellationToken);
    }

    [HttpGet("incidents/drills/backend-failure")]
    [HtmxRequest]
    public Task<IResult> IncidentBackendFailureDrill(CancellationToken cancellationToken)
    {
        _swapService.QueueHtml(
            targetId: $"error-toast-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            html: "<div class=\"toast error\">Server-side failure demo (500) with OOB toast.</div>",
            swapStyle: SwapStyle.BeforeEnd,
            selector: "#toast-stack");

        QueueInspectorUpdate(
            action: "incident-drill-backend-failure",
            details: "Returned a 500 response fragment and appended an OOB incident toast.");

        return HrzResults.ServerError<ErrorStatusResult>(
            HttpContext,
            new
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Title = "500 Server Error",
                Message = "The server generated an error fragment while preserving shell/layout."
            },
            cancellationToken: cancellationToken);
    }

    [HttpPost("settings/branding")]
    [HtmxRequest]
    public Task<IResult> UpdateBrandingSettings(
        [FromForm] string? title,
        [FromForm] string? description,
        [FromForm] string? accent,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "HyperRazor • Head Update" : title.Trim();
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? "Head updated from HTMX partial response."
            : description.Trim();
        var accentPreset = NormalizeHeadAccent(accent);

        _headService.SetTitle(normalizedTitle);
        _headService.AddMeta("description", normalizedDescription, key: "description");
        _headService.AddStyle(BuildHeadDemoStyle(accentPreset.Hex), key: "head-demo-style");
        _headService.AddScript(
            "/head-demo.asset.js",
            new Dictionary<string, object?>
            {
                ["defer"] = true
            },
            key: "head-demo-script");

        QueueInspectorUpdate(
            action: "settings-branding",
            details: $"Queued title/meta/style/script via IHrzHeadService. Title=\"{normalizedTitle}\", accent={accentPreset.Name}.");

        return PartialView<HeadUpdateResult>(new
        {
            Title = normalizedTitle,
            Description = normalizedDescription,
            AccentName = accentPreset.Name,
            AccentHex = accentPreset.Hex
        }, cancellationToken);
    }

    [HttpPost("users/provision")]
    public Task<IResult> ProvisionUser([FromForm] string? displayName, CancellationToken cancellationToken)
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
            action: "users-provision",
            details: $"Created {normalizedName} (#{count}).");

        return PartialView<UserCreateResult>(new { DisplayName = normalizedName, Count = count }, cancellationToken);
    }

    [HttpPost("users/provision-rendered")]
    [HtmxRequest]
    public async Task<IResult> ProvisionUserRendered([FromForm] string? displayName, CancellationToken cancellationToken)
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
            action: "users-provision-rendered",
            details: $"Created {normalizedName} (#{count}) via IHrzSwapService.RenderToString(clear: true).");

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

    [HttpPost("users/invite")]
    [HtmxRequest]
    public Task<IResult> ValidateUserInvite(
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
                action: "users-invite",
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
            action: "users-invite",
            details: $"Valid submission for {normalizedName} ({normalizedEmail}).");

        return PartialView<UserCreateValidationResult>(new
        {
            Success = true,
            DisplayName = normalizedName,
            Email = normalizedEmail,
            Count = count
        }, cancellationToken);
    }

    [HttpPost("access-requests/{requestId:int}/review")]
    [HtmxRequest]
    public Task<IResult> ReviewAccessRequest(
        int requestId,
        [FromForm] string? ticketId,
        [FromForm] string? justification,
        CancellationToken cancellationToken)
    {
        var normalizedTicketId = ticketId?.Trim() ?? string.Empty;
        var normalizedJustification = justification?.Trim() ?? string.Empty;
        var errors = ValidateAccessReviewInput(normalizedTicketId, normalizedJustification);

        if (errors.Count > 0)
        {
            QueueInspectorUpdate(
                action: "review-access-request",
                details: $"Request #{requestId} failed validation with {errors.Count} error(s).");

            return PartialView<AccessRequestReviewResult>(new
            {
                Errors = errors
            }, cancellationToken);
        }

        HttpContext.HtmxResponse().Location(new
        {
            path = $"/access-requests?completed={requestId}",
            target = "#hrz-main-layout",
            swap = "innerHTML show:window:top"
        });

        return Task.FromResult<IResult>(TypedResults.NoContent());
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

    private static List<string> ValidateAccessReviewInput(string ticketId, string justification)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ticketId))
        {
            errors.Add("Ticket ID is required before approving access.");
        }
        else if (ticketId.Length < 5)
        {
            errors.Add("Ticket ID must include the system prefix and numeric identifier.");
        }

        if (string.IsNullOrWhiteSpace(justification))
        {
            errors.Add("A review justification is required.");
        }
        else if (justification.Length < 12)
        {
            errors.Add("Justification must be at least 12 characters.");
        }

        return errors;
    }

    private void QueueUserCreatedSwaps(string normalizedName, int count, bool includeUsersList)
    {
        if (includeUsersList)
        {
            _swapService.QueueComponent<UserCreateResult>(
                targetId: "users-list",
                parameters: new Dictionary<string, object?>
                {
                    [nameof(UserCreateResult.DisplayName)] = normalizedName,
                    [nameof(UserCreateResult.Count)] = count
                },
                swapStyle: SwapStyle.OuterHtml);
        }

        _swapService.QueueComponent<ToastSuccess>(
            targetId: $"toast-{count}",
            parameters: new Dictionary<string, object?>
            {
                [nameof(Components.Fragments.ToastSuccess.Message)] = $"Created {normalizedName}."
            },
            swapStyle: SwapStyle.BeforeEnd,
            selector: "#toast-stack");

        _swapService.QueueComponent<UserCountValue>(
            targetId: "user-count-shell",
            parameters: new Dictionary<string, object?>
            {
                [nameof(UserCountValue.Count)] = count
            },
            swapStyle: SwapStyle.InnerHtml);

        _swapService.QueueComponent<ActivityFeedItem>(
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
        _swapService.QueueComponent<HxRequestResponseInspector>(
            targetId: "hx-debug-shell",
            parameters: BuildInspectorParameters(HttpContext, action, details),
            swapStyle: SwapStyle.OuterHtml);
    }

    private void QueueDashboardEventLog(string message)
    {
        _swapService.QueueComponent<DashboardEventLog>(
            targetId: "dashboard-event-log",
            parameters: new
            {
                Message = message
            },
            swapStyle: SwapStyle.InnerHtml);
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
        var requestHeaders = context.Request.Headers;
        var response = context.Response.Headers;
        var parsedRequest = context.HtmxRequest();
        var route = $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}";

        return new Dictionary<string, object?>
        {
            [nameof(HxRequestResponseInspector.ActionName)] = action,
            [nameof(HxRequestResponseInspector.Details)] = details,
            [nameof(HxRequestResponseInspector.Route)] = route,
            [nameof(HxRequestResponseInspector.HxRequest)] = ReadHeader(requestHeaders, HtmxHeaderNames.Request),
            [nameof(HxRequestResponseInspector.HxRequestType)] = ReadHeader(requestHeaders, HtmxHeaderNames.RequestType),
            [nameof(HxRequestResponseInspector.HxTarget)] = ReadHeader(requestHeaders, HtmxHeaderNames.Target),
            [nameof(HxRequestResponseInspector.HxTrigger)] = ReadHeader(requestHeaders, HtmxHeaderNames.Trigger),
            [nameof(HxRequestResponseInspector.HxSource)] = ReadHeader(requestHeaders, HtmxHeaderNames.Source),
            [nameof(HxRequestResponseInspector.HxCurrentUrl)] = ReadHeader(requestHeaders, HtmxHeaderNames.CurrentUrl),
            [nameof(HxRequestResponseInspector.ParsedVersion)] = parsedRequest.Version.ToString(),
            [nameof(HxRequestResponseInspector.ParsedRequestType)] = parsedRequest.RequestType.ToString(),
            [nameof(HxRequestResponseInspector.HxTriggerResponse)] = ReadHeader(response, HtmxHeaderNames.TriggerResponse),
            [nameof(HxRequestResponseInspector.HxRedirect)] = ReadHeader(response, HtmxHeaderNames.Redirect),
            [nameof(HxRequestResponseInspector.HxLocation)] = ReadHeader(response, HtmxHeaderNames.Location),
            [nameof(HxRequestResponseInspector.HxPushUrl)] = ReadHeader(response, HtmxHeaderNames.PushUrl),
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

    private static (string Name, string Hex) NormalizeHeadAccent(string? accent)
    {
        return accent?.Trim().ToLowerInvariant() switch
        {
            "amber" => ("Amber", "#d97706"),
            "rose" => ("Rose", "#e11d48"),
            _ => ("Teal", "#0f766e")
        };
    }

    private static string BuildHeadDemoStyle(string accentHex)
    {
        return $$"""
        #head-demo-result .head-demo-style-preview {
            display: inline-flex;
            align-items: center;
            gap: 0.45rem;
            padding: 0.4rem 0.8rem;
            border: 1px solid {{accentHex}};
            border-radius: 999px;
            background: color-mix(in srgb, {{accentHex}} 14%, white);
            color: {{accentHex}};
            font-weight: 700;
            letter-spacing: 0.04em;
            text-transform: uppercase;
        }
        """;
    }
}
