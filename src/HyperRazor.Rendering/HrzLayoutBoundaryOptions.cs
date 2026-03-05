namespace HyperRazor.Rendering;

public sealed class HrzLayoutBoundaryOptions
{
    public bool Enabled { get; set; }

    public bool OnlyBoostedRequests { get; set; } = true;

    public HrzLayoutBoundaryPromotionMode PromotionMode { get; set; } = HrzLayoutBoundaryPromotionMode.ShellSwap;

    public string LayoutFamilyHeaderName { get; set; } = "X-Hrz-Layout-Family";

    public string DefaultLayoutFamily { get; set; } = "main";

    public string ShellTargetSelector { get; set; } = "#hrz-app-shell";

    public string ShellSwapStyle { get; set; } = "outerHTML";

    public string ShellReselectSelector { get; set; } = "#hrz-app-shell";

    public bool AddVaryHeader { get; set; } = true;
}
