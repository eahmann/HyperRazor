using Microsoft.AspNetCore.Http;

namespace HyperRazor.Demo.Mvc.Infrastructure;

public sealed record DemoChromeState(
    string RouteLabel,
    string ActiveSection,
    string LayoutFamily,
    string Theme)
{
    public const string ThemeCookieName = "hrz-demo-theme";
    public const string DefaultTheme = "dark";

    public string ThemeHref => GetThemeHref(Theme);

    public static DemoChromeState Create(HttpContext? context, string? layoutFamily = null)
    {
        var path = context?.Request.Path ?? "/";
        var resolvedLayoutFamily = NormalizeLayoutFamily(layoutFamily) ?? ResolveLayoutFamily(path);

        return new DemoChromeState(
            RouteLabel: BuildRouteLabel(context),
            ActiveSection: ResolveActiveSection(path),
            LayoutFamily: resolvedLayoutFamily,
            Theme: GetTheme(context));
    }

    public static bool IsPageChromeRoute(PathString path)
    {
        var value = NormalizePath(path);
        return value == "/"
            || value == "/users"
            || value == "/validation"
            || value == "/demos/sse"
            || value == "/access-requests"
            || value.StartsWith("/access-requests/", StringComparison.OrdinalIgnoreCase)
            || value == "/incidents"
            || value.StartsWith("/incidents/", StringComparison.OrdinalIgnoreCase)
            || value == "/settings/branding";
    }

    public static string GetTheme(HttpContext? context)
    {
        if (context?.Request.Cookies.TryGetValue(ThemeCookieName, out var value) == true)
        {
            return NormalizeTheme(value);
        }

        return DefaultTheme;
    }

    public static string NormalizeTheme(string? theme)
    {
        return string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase) ? "light" : DefaultTheme;
    }

    public static string GetThemeHref(string theme)
    {
        return NormalizeTheme(theme) == "light"
            ? "/vendor/bootswatch/flatly.min.css"
            : "/vendor/bootswatch/slate.min.css";
    }

    public static void WriteThemeCookie(HttpContext context, string theme)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.Cookies.Append(
            ThemeCookieName,
            NormalizeTheme(theme),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }

    public static string NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        return returnUrl[0] == '/'
            && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? returnUrl
            : "/";
    }

    private static string BuildRouteLabel(HttpContext? context)
    {
        if (context is null)
        {
            return "/";
        }

        var path = string.IsNullOrWhiteSpace(context.Request.Path.Value)
            ? "/"
            : context.Request.Path.Value!;

        var query = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value
            : string.Empty;

        return string.Concat(path, query);
    }

    private static string ResolveActiveSection(PathString path)
    {
        var value = NormalizePath(path);

        if (value.StartsWith("/access-requests", StringComparison.OrdinalIgnoreCase))
        {
            return "/access-requests";
        }

        if (value.StartsWith("/incidents", StringComparison.OrdinalIgnoreCase))
        {
            return "/incidents";
        }

        if (value.StartsWith("/settings/", StringComparison.OrdinalIgnoreCase))
        {
            return "/settings/branding";
        }

        if (value == "/validation" || value.StartsWith("/validation/", StringComparison.OrdinalIgnoreCase))
        {
            return "/validation";
        }

        if (value == "/demos/sse" || value.StartsWith("/demos/sse/", StringComparison.OrdinalIgnoreCase))
        {
            return "/demos/sse";
        }

        return value == "/users" ? "/users" : "/";
    }

    private static string ResolveLayoutFamily(PathString path)
    {
        var value = NormalizePath(path);

        if ((value.StartsWith("/access-requests/", StringComparison.OrdinalIgnoreCase)
                && value.EndsWith("/review", StringComparison.OrdinalIgnoreCase))
            || (value.StartsWith("/incidents/", StringComparison.OrdinalIgnoreCase)
                && value.EndsWith("/triage", StringComparison.OrdinalIgnoreCase)))
        {
            return "task";
        }

        if (value.StartsWith("/access-requests", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/incidents", StringComparison.OrdinalIgnoreCase))
        {
            return "workbench";
        }

        return "admin";
    }

    private static string? NormalizeLayoutFamily(string? layoutFamily)
    {
        return string.IsNullOrWhiteSpace(layoutFamily)
            ? null
            : layoutFamily.Trim().ToLowerInvariant();
    }

    private static string NormalizePath(PathString path)
    {
        return string.IsNullOrWhiteSpace(path.Value)
            ? "/"
            : path.Value!;
    }
}
