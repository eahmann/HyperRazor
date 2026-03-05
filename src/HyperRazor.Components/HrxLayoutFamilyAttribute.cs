namespace HyperRazor.Components;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class HrxLayoutFamilyAttribute : Attribute
{
    public HrxLayoutFamilyAttribute(string family)
    {
        if (string.IsNullOrWhiteSpace(family))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(family));
        }

        Family = family.Trim();
    }

    public string Family { get; }
}
