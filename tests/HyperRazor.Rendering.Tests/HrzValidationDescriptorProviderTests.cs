using System.ComponentModel.DataAnnotations;

namespace HyperRazor.Rendering.Tests;

public class HrzValidationDescriptorProviderTests
{
    private readonly HrzFieldPathResolver _resolver = new();
    private readonly HrzDataAnnotationsValidationDescriptorProvider _provider;
    private readonly HrzDefaultHtmlIdGenerator _htmlIdGenerator = new();

    public HrzValidationDescriptorProviderTests()
    {
        _provider = new HrzDataAnnotationsValidationDescriptorProvider(_resolver);
    }

    [Fact]
    public void GetDescriptor_CachesDescriptorsPerModelType()
    {
        var first = _provider.GetDescriptor(typeof(DescriptorModel));
        var second = _provider.GetDescriptor(typeof(DescriptorModel));

        Assert.Same(first, second);
    }

    [Fact]
    public void GetDescriptor_MapsRulesForLeafNestedAndCollectionFields()
    {
        var descriptor = _provider.GetDescriptor(typeof(DescriptorModel));

        var emailField = Assert.Contains(_resolver.FromFieldName("Email"), descriptor.Fields);
        Assert.Equal("Email", emailField.HtmlName);
        Assert.Equal("Email Address", emailField.DisplayName);
        Assert.Contains("required", emailField.LocalRules.Keys);
        Assert.Contains("email", emailField.LocalRules.Keys);

        var displayNameField = Assert.Contains(_resolver.FromFieldName("DisplayName"), descriptor.Fields);
        Assert.Contains("length", displayNameField.LocalRules.Keys);
        Assert.Equal("3", displayNameField.LocalRules["length-min"]);
        Assert.Equal("50", displayNameField.LocalRules["length-max"]);

        var postalCodeField = Assert.Contains(_resolver.FromFieldName("PostalCode"), descriptor.Fields);
        Assert.Contains("regex", postalCodeField.LocalRules.Keys);
        Assert.Equal("^\\d{5}(-\\d{4})?$", postalCodeField.LocalRules["regex-pattern"]);

        _ = Assert.Contains(_resolver.FromFieldName("Address.Street"), descriptor.Fields);
        _ = Assert.Contains(_resolver.FromFieldName("Items.Name"), descriptor.Fields);
    }

    [Fact]
    public void HtmlIdGenerator_UsesStableFormAndFieldFormatting()
    {
        var path = _resolver.FromFieldName("Addresses[0].Street");

        Assert.Equal("users-invite", _htmlIdGenerator.GetFormId("users-invite"));
        Assert.Equal("users-invite-addresses-0-street", _htmlIdGenerator.GetFieldId("users-invite", path));
        Assert.Equal("users-invite-addresses-0-street-message", _htmlIdGenerator.GetFieldMessageId("users-invite", path));
        Assert.Equal("users-invite-summary", _htmlIdGenerator.GetSummaryId("users-invite"));
    }

    private sealed class DescriptorModel
    {
        [Required]
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(50, MinimumLength = 3)]
        public string DisplayName { get; set; } = string.Empty;

        [RegularExpression("^\\d{5}(-\\d{4})?$")]
        public string PostalCode { get; set; } = string.Empty;

        public DescriptorAddress Address { get; set; } = new();

        public List<DescriptorItem> Items { get; set; } = [];
    }

    private sealed class DescriptorAddress
    {
        [Required]
        public string Street { get; set; } = string.Empty;
    }

    private sealed class DescriptorItem
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
