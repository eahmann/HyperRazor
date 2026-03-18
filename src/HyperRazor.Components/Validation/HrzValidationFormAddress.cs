namespace HyperRazor.Components.Validation;

public readonly record struct HrzValidationFormAddress
{
    public static HrzValidationFormAddress CreateRequired(
        HrzValidationRootId? rootId,
        string? formName,
        string ownerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        if (rootId is not null)
        {
            return new HrzValidationFormAddress(rootId);
        }

        if (!string.IsNullOrWhiteSpace(formName))
        {
            return new HrzValidationFormAddress(formName);
        }

        throw new InvalidOperationException(
            $"{ownerName} requires either {nameof(RootId)} or {nameof(FormName)}. " +
            $"{nameof(RootId)} takes precedence when both are supplied.");
    }

    public HrzValidationFormAddress(string formName)
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(formName));
        }

        FormName = formName;
        RootId = null;
    }

    public HrzValidationFormAddress(HrzValidationRootId rootId)
    {
        RootId = rootId ?? throw new ArgumentNullException(nameof(rootId));
        FormName = null;
    }

    public string? FormName { get; }

    public HrzValidationRootId? RootId { get; }

    public HrzValidationRootId ResolveRequired(string ownerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);

        if (RootId is not null)
        {
            return RootId;
        }

        if (!string.IsNullOrWhiteSpace(FormName))
        {
            return new HrzValidationRootId(FormName);
        }

        throw new InvalidOperationException(
            $"{ownerName} requires either {nameof(RootId)} or {nameof(FormName)}. " +
            $"{nameof(RootId)} takes precedence when both are supplied.");
    }
}
