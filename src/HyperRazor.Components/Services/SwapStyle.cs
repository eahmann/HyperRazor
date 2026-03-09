namespace HyperRazor.Components.Services;

public enum SwapStyle
{
    True,
    InnerHtml,
    OuterHtml,
    BeforeBegin,
    AfterBegin,
    BeforeEnd,
    AfterEnd,
    Delete,
    None
}

public static class SwapStyleExtensions
{
    public static string ToHtmxString(this SwapStyle style)
    {
        return style switch
        {
            SwapStyle.True => "true",
            SwapStyle.InnerHtml => "innerHTML",
            SwapStyle.OuterHtml => "outerHTML",
            SwapStyle.BeforeBegin => "beforebegin",
            SwapStyle.AfterBegin => "afterbegin",
            SwapStyle.BeforeEnd => "beforeend",
            SwapStyle.AfterEnd => "afterend",
            SwapStyle.Delete => "delete",
            SwapStyle.None => "none",
            _ => "outerHTML"
        };
    }
}
