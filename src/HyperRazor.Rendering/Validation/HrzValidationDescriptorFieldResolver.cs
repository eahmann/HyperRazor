using System.Text;

namespace HyperRazor.Rendering;

internal static class HrzValidationDescriptorFieldResolver
{
    public static HrzFieldDescriptor Resolve(
        HrzValidationDescriptor descriptor,
        HrzFieldPath path,
        IHrzFieldPathResolver fieldPathResolver)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(fieldPathResolver);

        if (descriptor.Fields.TryGetValue(path, out var exactDescriptor))
        {
            return exactDescriptor;
        }

        var templatePath = fieldPathResolver.FromFieldName(RemoveIndices(path.Value));
        if (!descriptor.Fields.TryGetValue(templatePath, out var templateDescriptor))
        {
            throw new InvalidOperationException(
                $"No validation descriptor exists for field path '{path.Value}' on model '{descriptor.ModelType.Name}'.");
        }

        return new HrzFieldDescriptor
        {
            Path = path,
            HtmlName = fieldPathResolver.Format(path),
            DisplayName = templateDescriptor.DisplayName,
            LocalRules = templateDescriptor.LocalRules,
            LiveRule = templateDescriptor.LiveRule
        };
    }

    private static string RemoveIndices(string value)
    {
        var builder = new StringBuilder(value.Length);
        var depth = 0;

        foreach (var character in value)
        {
            if (character == '[')
            {
                depth++;
                continue;
            }

            if (character == ']')
            {
                depth = Math.Max(0, depth - 1);
                continue;
            }

            if (depth == 0)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
