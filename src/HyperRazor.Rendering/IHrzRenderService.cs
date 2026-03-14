using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public interface IHrzRenderService
{
    Task<IResult> Page<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> Fragment<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> Fragment(CancellationToken cancellationToken = default, params RenderFragment[] fragments);

    Task<IResult> RootSwap<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;
}
