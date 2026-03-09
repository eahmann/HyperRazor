using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace HyperRazor.Rendering;

public interface IHrzComponentViewService
{
    Task<IResult> View<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> View<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> PartialView<TComponent>(object? data = null, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> PartialView<TComponent>(IReadOnlyDictionary<string, object?> data, CancellationToken cancellationToken = default)
        where TComponent : IComponent;

    Task<IResult> PartialView(CancellationToken cancellationToken = default, params RenderFragment[] fragments);
}
