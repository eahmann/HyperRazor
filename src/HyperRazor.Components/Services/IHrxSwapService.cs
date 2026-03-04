namespace HyperRazor.Components.Services;

public interface IHrxSwapService
{
    void Queue(string target, string html, string swap = "innerHTML");

    IReadOnlyList<HrxSwapItem> GetAll();

    void Clear();
}
