namespace HyperRazor.Rendering;

public interface IHrxHtmlRendererAdapter
{
    Task<string> RenderAsync(Type componentType, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}
