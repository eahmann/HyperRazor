using System.Text;

namespace HyperRazor.Rendering;

public sealed class HrzDefaultHtmlIdGenerator : IHrzHtmlIdGenerator
{
    public string GetFormId(string formName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);

        return Sanitize(formName);
    }

    public string GetFieldId(string formName, HrzFieldPath path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formName);
        ArgumentNullException.ThrowIfNull(path);

        return $"{GetFormId(formName)}-{Sanitize(path.Value)}";
    }

    public string GetFieldMessageId(string formName, HrzFieldPath path) =>
        $"{GetFieldId(formName, path)}-message";

    public string GetSummaryId(string formName) =>
        $"{GetFormId(formName)}-summary";

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        var lastWasDash = false;

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasDash = false;
                continue;
            }

            if (lastWasDash)
            {
                continue;
            }

            builder.Append('-');
            lastWasDash = true;
        }

        return builder
            .ToString()
            .Trim('-');
    }
}
