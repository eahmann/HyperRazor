namespace HyperRazor.Rendering;

internal static class HrzLayoutKey
{
    public const string None = "__hrz:none__";

    public static string Create(Type? layoutType)
    {
        if (layoutType is null)
        {
            return None;
        }

        return layoutType.FullName
            ?? layoutType.AssemblyQualifiedName
            ?? layoutType.Name;
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Any(char.IsControl))
        {
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
