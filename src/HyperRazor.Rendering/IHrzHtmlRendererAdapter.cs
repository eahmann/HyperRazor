namespace HyperRazor.Rendering;

public interface IHrzHtmlRendererAdapter
{
    Task<string> RenderAsync(Type componentType, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
}
