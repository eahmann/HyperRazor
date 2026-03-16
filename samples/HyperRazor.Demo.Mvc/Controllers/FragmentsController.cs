using HyperRazor.Demo.Mvc.Components.Fragments;
using HyperRazor.Demo.Mvc.Infrastructure;
using HyperRazor.Components.Services;
using HyperRazor.Htmx;
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
            HttpContext.HtmxResponse().Trigger("chrome:theme-updated", new
            {
                theme = normalizedTheme,
                href = DemoChromeState.GetThemeHref(normalizedTheme)
            });
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

        return Fragment<UserSearchResults>(new
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

        return Fragment<DashboardCheckResult>(new
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

        return Fragment<DashboardCheckResult>(new
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
        var toastId = $"error-toast-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        _swapService.Append(
            HyperRazor.Demo.Mvc.Components.Pages.Workbench.IncidentsPage.ToastStackRegion,
            toastId,
            builder => builder.AddMarkupContent(0, "<div class=\"toast error\">Server-side failure demo (500) with OOB toast.</div>"));

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

        return Fragment<HeadUpdateResult>(new
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

        return Fragment<UserCreateResult>(new { DisplayName = normalizedName, Count = count }, cancellationToken);
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

        return await Fragment(
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

            return Fragment<AccessRequestReviewResult>(new
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
            _swapService.Replace<UserCreateResult>(
                HyperRazor.Demo.Mvc.Components.Pages.Admin.UsersPage.UsersListRegion,
                new Dictionary<string, object?>
                {
                    [nameof(UserCreateResult.DisplayName)] = normalizedName,
                    [nameof(UserCreateResult.Count)] = count
                });
        }

        _swapService.Append<ToastSuccess>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.UsersPage.ToastStackRegion,
            $"toast-{count}",
            new Dictionary<string, object?>
            {
                [nameof(Components.Fragments.ToastSuccess.Message)] = $"Created {normalizedName}."
            });

        _swapService.Replace<UserCountValue>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.UsersPage.UserCountRegion,
            new Dictionary<string, object?>
            {
                [nameof(UserCountValue.Count)] = count
            });

        _swapService.Append<ActivityFeedItem>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.UsersPage.ActivityFeedRegion,
            $"activity-{count}",
            new Dictionary<string, object?>
            {
                [nameof(ActivityFeedItem.DisplayName)] = normalizedName,
                [nameof(ActivityFeedItem.Count)] = count
            });
    }

    private void QueueInspectorUpdate(string action, string details)
    {
        DemoInspectorUpdates.Queue(HttpContext, action, details);
    }

    private void QueueDashboardEventLog(string message)
    {
        _swapService.Replace<DashboardEventLog>(
            HyperRazor.Demo.Mvc.Components.Pages.Admin.DashboardPage.EventLogRegion,
            new
            {
                Message = message
            });
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
