namespace HyperRazor.Components.Services;

public sealed class HrxSwapService : IHrxSwapService
{
    private readonly List<HrxSwapItem> _items = [];

    public void Queue(string target, string html, string swap = "innerHTML")
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(target));
        }

        _items.Add(new HrxSwapItem(target, html ?? string.Empty, string.IsNullOrWhiteSpace(swap) ? "innerHTML" : swap));
    }

    public IReadOnlyList<HrxSwapItem> GetAll() => _items;

    public void Clear() => _items.Clear();
}
