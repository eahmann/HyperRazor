using System.Reflection;

namespace HyperRazor.Components.Validation;

public interface IHrzClientValidationMetadataProvider
{
    void AddValidationAttributes(
        PropertyInfo property,
        string displayName,
        IDictionary<string, string> attributes);
}
