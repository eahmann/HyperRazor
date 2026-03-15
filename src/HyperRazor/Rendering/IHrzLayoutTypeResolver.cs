namespace HyperRazor.Rendering;

internal interface IHrzLayoutTypeResolver
{
    Type? ResolveForComponent(Type componentType);
}
