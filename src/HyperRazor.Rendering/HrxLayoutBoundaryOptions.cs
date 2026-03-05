namespace HyperRazor.Rendering;

public sealed class HrxLayoutBoundaryOptions
{
    public bool Enabled { get; set; }

    public bool OnlyBoostedRequests { get; set; } = true;

    public HrxLayoutBoundaryPromotionMode PromotionMode { get; set; } = HrxLayoutBoundaryPromotionMode.ShellSwap;

    public string LayoutFamilyHeaderName { get; set; } = "X-Hrx-Layout-Family";

    public string DefaultLayoutFamily { get; set; } = "main";

    public string ShellTargetSelector { get; set; } = "#hrx-app-shell";

    public string ShellSwapStyle { get; set; } = "outerHTML";

    public string ShellReselectSelector { get; set; } = "#hrx-app-shell";

    public bool AddVaryHeader { get; set; } = true;
}
