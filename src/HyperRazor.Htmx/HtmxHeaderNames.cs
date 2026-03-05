namespace HyperRazor.Htmx;

public static class HtmxHeaderNames
{
    // Request headers
    public const string Request = "HX-Request";
    public const string RequestType = "HX-Request-Type";
    public const string Target = "HX-Target";
    public const string Source = "HX-Source";
    public const string Trigger = "HX-Trigger";
    public const string TriggerName = "HX-Trigger-Name";
    public const string CurrentUrl = "HX-Current-URL";
    public const string Boosted = "HX-Boosted";
    public const string HistoryRestoreRequest = "HX-History-Restore-Request";
    public const string LayoutFamily = "X-Hrx-Layout-Family";

    // Response headers
    public const string Redirect = "HX-Redirect";
    public const string Location = "HX-Location";
    public const string PushUrl = "HX-Push-Url";
    public const string ReplaceUrl = "HX-Replace-Url";
    public const string Refresh = "HX-Refresh";
    public const string Retarget = "HX-Retarget";
    public const string Reswap = "HX-Reswap";
    public const string Reselect = "HX-Reselect";
    public const string TriggerResponse = "HX-Trigger";
    public const string TriggerAfterSwap = "HX-Trigger-After-Swap";
    public const string TriggerAfterSettle = "HX-Trigger-After-Settle";
}
