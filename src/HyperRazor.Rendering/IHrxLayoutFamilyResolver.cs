namespace HyperRazor.Rendering;

public interface IHrxLayoutFamilyResolver
{
    string ResolveForPageComponent(Type pageComponentType);

    string ResolveForLayoutType(Type layoutType);
}
