using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HyperRazor.Rendering;

internal sealed record HrzComponentHostRenderRequest(
    Type ComponentType,
    IReadOnlyDictionary<string, object?> ComponentParameters,
    Type? LayoutType,
    string? CurrentLayoutKey,
    HttpContext HttpContext,
    ModelStateDictionary ModelState,
    bool IsPartial,
    bool IsHtmxRequest,
    bool IsHistoryRestoreRequest,
    bool? RenderHeadContent,
    bool? RenderSwapContent);
