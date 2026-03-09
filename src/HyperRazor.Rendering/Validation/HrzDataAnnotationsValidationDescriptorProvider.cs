using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace HyperRazor.Rendering;

public sealed class HrzDataAnnotationsValidationDescriptorProvider : IHrzValidationDescriptorProvider
{
    private static readonly Type[] SimpleTypes =
    [
        typeof(string),
        typeof(bool),
        typeof(byte),
        typeof(sbyte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(char),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(DateOnly),
        typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(Guid),
        typeof(Uri)
    ];

    private readonly IHrzFieldPathResolver _fieldPathResolver;
    private readonly ConcurrentDictionary<Type, HrzValidationDescriptor> _cache = new();

    public HrzDataAnnotationsValidationDescriptorProvider(IHrzFieldPathResolver fieldPathResolver)
    {
        _fieldPathResolver = fieldPathResolver ?? throw new ArgumentNullException(nameof(fieldPathResolver));
    }

    public HrzValidationDescriptor GetDescriptor(Type modelType)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        return _cache.GetOrAdd(modelType, BuildDescriptor);
    }

    private HrzValidationDescriptor BuildDescriptor(Type modelType)
    {
        var normalizedType = UnwrapNullable(modelType);
        var fields = new Dictionary<HrzFieldPath, HrzFieldDescriptor>();

        PopulateFields(
            modelType: normalizedType,
            parentPath: null,
            fields: fields,
            ancestry: new HashSet<Type>());

        return new HrzValidationDescriptor
        {
            ModelType = normalizedType,
            Fields = fields
        };
    }

    private void PopulateFields(
        Type modelType,
        HrzFieldPath? parentPath,
        IDictionary<HrzFieldPath, HrzFieldDescriptor> fields,
        ISet<Type> ancestry)
    {
        if (!ancestry.Add(modelType))
        {
            return;
        }

        foreach (var property in GetCandidateProperties(modelType))
        {
            var propertyType = UnwrapNullable(property.PropertyType);
            var path = parentPath is null
                ? _fieldPathResolver.FromFieldName(property.Name)
                : _fieldPathResolver.Append(parentPath, property.Name);

            if (TryGetCollectionElementType(propertyType, out var elementType))
            {
                if (IsLeafType(elementType))
                {
                    fields[path] = BuildFieldDescriptor(property, path);
                }
                else
                {
                    PopulateFields(elementType, path, fields, ancestry);
                }

                continue;
            }

            if (IsLeafType(propertyType))
            {
                fields[path] = BuildFieldDescriptor(property, path);
                continue;
            }

            PopulateFields(propertyType, path, fields, ancestry);
        }

        ancestry.Remove(modelType);
    }

    private HrzFieldDescriptor BuildFieldDescriptor(PropertyInfo property, HrzFieldPath path)
    {
        var displayName = ResolveDisplayName(property);

        return new HrzFieldDescriptor
        {
            Path = path,
            HtmlName = _fieldPathResolver.Format(path),
            DisplayName = displayName,
            LocalRules = BuildLocalRules(property, displayName),
            LiveRule = null
        };
    }

    private static IReadOnlyDictionary<string, string> BuildLocalRules(PropertyInfo property, string? displayName)
    {
        var rules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var effectiveDisplayName = string.IsNullOrWhiteSpace(displayName)
            ? property.Name
            : displayName;

        foreach (var attribute in property.GetCustomAttributes<ValidationAttribute>(inherit: true))
        {
            switch (attribute)
            {
                case RequiredAttribute:
                    rules["required"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    break;

                case EmailAddressAttribute:
                    rules["email"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    break;

                case PhoneAttribute:
                    rules["phone"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    break;

                case UrlAttribute:
                    rules["url"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    break;

                case CreditCardAttribute:
                    rules["creditcard"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    break;

                case CompareAttribute compareAttribute:
                    rules["equalto"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    rules["equalto-other"] = $"*.{compareAttribute.OtherProperty}";
                    break;

                case StringLengthAttribute stringLengthAttribute:
                    rules["length"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    rules["length-max"] = stringLengthAttribute.MaximumLength.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (stringLengthAttribute.MinimumLength > 0)
                    {
                        rules["length-min"] = stringLengthAttribute.MinimumLength.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    break;

                case MinLengthAttribute minLengthAttribute:
                    rules["minlength"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    rules["minlength-min"] = minLengthAttribute.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case MaxLengthAttribute maxLengthAttribute:
                    rules["maxlength"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    rules["maxlength-max"] = maxLengthAttribute.Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    break;

                case RangeAttribute rangeAttribute:
                    rules["range"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    if (rangeAttribute.Minimum is not null)
                    {
                        rules["range-min"] = Convert.ToString(rangeAttribute.Minimum, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                    }

                    if (rangeAttribute.Maximum is not null)
                    {
                        rules["range-max"] = Convert.ToString(rangeAttribute.Maximum, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                    }

                    break;

                case RegularExpressionAttribute regularExpressionAttribute:
                    rules["regex"] = attribute.FormatErrorMessage(effectiveDisplayName);
                    rules["regex-pattern"] = regularExpressionAttribute.Pattern;
                    break;

            }
        }

        return rules;
    }

    private static string? ResolveDisplayName(MemberInfo property)
    {
        var displayAttribute = property.GetCustomAttribute<DisplayAttribute>(inherit: true);
        if (displayAttribute is not null)
        {
            var displayName = displayAttribute.GetName();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        var displayNameAttribute = property.GetCustomAttribute<DisplayNameAttribute>(inherit: true);
        return string.IsNullOrWhiteSpace(displayNameAttribute?.DisplayName)
            ? null
            : displayNameAttribute.DisplayName;
    }

    private static IEnumerable<PropertyInfo> GetCandidateProperties(Type modelType) =>
        modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetMethod?.IsPublic == true)
            .OrderBy(static property => property.MetadataToken);

    private static bool TryGetCollectionElementType(Type type, out Type elementType)
    {
        if (type == typeof(string))
        {
            elementType = typeof(string);
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType() ?? typeof(object);
            return true;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(type))
        {
            elementType = typeof(object);
            return false;
        }

        elementType = type
            .GetInterfaces()
            .Append(type)
            .Where(static candidate => candidate.IsGenericType)
            .FirstOrDefault(static candidate => candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0]
            ?? typeof(object);

        return elementType != typeof(object);
    }

    private static bool IsLeafType(Type type) =>
        type.IsEnum
        || SimpleTypes.Contains(type)
        || type.IsPrimitive;

    private static Type UnwrapNullable(Type type) =>
        Nullable.GetUnderlyingType(type) ?? type;
}
