namespace HyperRazor.Rendering;

public interface IHrzLayoutFamilyResolver
{
    string ResolveForPageComponent(Type pageComponentType);

    string ResolveForLayoutType(Type layoutType);
}
