using System.Reflection;

namespace HyperRazor.Rendering;

public interface IHrzClientValidationMetadataProvider
{
    void AddValidationAttributes(
        PropertyInfo property,
        string displayName,
        IDictionary<string, string> attributes);
}
