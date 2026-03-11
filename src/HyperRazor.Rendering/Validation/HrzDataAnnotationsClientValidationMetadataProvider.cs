using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace HyperRazor.Rendering;

public sealed class HrzDataAnnotationsClientValidationMetadataProvider : IHrzClientValidationMetadataProvider
{
    public void AddValidationAttributes(
        PropertyInfo property,
        string displayName,
        IDictionary<string, string> attributes)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(attributes);

        AddValidationAttribute(attributes, "required", property.GetCustomAttribute<RequiredAttribute>(), displayName);
        AddValidationAttribute(attributes, "email", property.GetCustomAttribute<EmailAddressAttribute>(), displayName);
        AddValidationAttribute(attributes, "creditcard", property.GetCustomAttribute<CreditCardAttribute>(), displayName);
        AddValidationAttribute(attributes, "url", property.GetCustomAttribute<UrlAttribute>(), displayName);
        AddValidationAttribute(attributes, "phone", property.GetCustomAttribute<PhoneAttribute>(), displayName);

        var stringLength = property.GetCustomAttribute<StringLengthAttribute>();
        if (stringLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-length"] = stringLength.FormatErrorMessage(displayName);
            attributes["data-val-length-max"] = stringLength.MaximumLength.ToString(CultureInfo.InvariantCulture);
            if (stringLength.MinimumLength > 0)
            {
                attributes["data-val-length-min"] = stringLength.MinimumLength.ToString(CultureInfo.InvariantCulture);
            }
        }

        var minLength = property.GetCustomAttribute<MinLengthAttribute>();
        if (minLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-minlength"] = minLength.FormatErrorMessage(displayName);
            attributes["data-val-minlength-min"] = minLength.Length.ToString(CultureInfo.InvariantCulture);
        }

        var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLength is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-maxlength"] = maxLength.FormatErrorMessage(displayName);
            attributes["data-val-maxlength-max"] = maxLength.Length.ToString(CultureInfo.InvariantCulture);
        }

        var range = property.GetCustomAttribute<RangeAttribute>();
        if (range is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-range"] = range.FormatErrorMessage(displayName);
            attributes["data-val-range-min"] = Convert.ToString(range.Minimum, CultureInfo.InvariantCulture) ?? string.Empty;
            attributes["data-val-range-max"] = Convert.ToString(range.Maximum, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        var regex = property.GetCustomAttribute<RegularExpressionAttribute>();
        if (regex is not null)
        {
            attributes["data-val"] = "true";
            attributes["data-val-regex"] = regex.FormatErrorMessage(displayName);
            attributes["data-val-regex-pattern"] = regex.Pattern;
        }
    }

    private static void AddValidationAttribute(
        IDictionary<string, string> attributes,
        string ruleName,
        ValidationAttribute? attribute,
        string displayName)
    {
        if (attribute is null)
        {
            return;
        }

        attributes["data-val"] = "true";
        attributes[$"data-val-{ruleName}"] = attribute.FormatErrorMessage(displayName);
    }
}
