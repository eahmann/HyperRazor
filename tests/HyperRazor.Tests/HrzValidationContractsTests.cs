using System.Collections;
using HyperRazor.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace HyperRazor.Tests;

public class HrzValidationContractsTests
{
    private readonly HrzFieldPathResolver _resolver = new();
    private readonly ValidationModel Input = new();

    [Fact]
    public void HrzFieldPath_UsesOrdinalValueEquality()
    {
        var first = _resolver.FromFieldName("Input.Email");
        var second = _resolver.FromFieldName("Email");
        var third = _resolver.FromFieldName("email");

        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public void FromExpression_NormalizesInputPrefix()
    {
        var path = _resolver.FromExpression(() => Input.Email);

        Assert.Equal("Email", path.Value);
    }

    [Fact]
    public void AppendAndIndex_BuildCanonicalNestedPath()
    {
        var items = _resolver.FromFieldName("items");
        var indexed = _resolver.Index(items, 0);
        var nested = _resolver.Append(indexed, "name");

        Assert.Equal("Items[0].Name", nested.Value);
    }

    [Fact]
    public void Resolve_ReturnsNestedFieldIdentifier()
    {
        var model = new ValidationModel
        {
            Items =
            [
                new ValidationItem
                {
                    Name = "First"
                }
            ]
        };

        var fieldIdentifier = _resolver.Resolve(model, _resolver.FromFieldName("Items[0].Name"));

        Assert.Equal("Name", fieldIdentifier.FieldName);
        Assert.Same(model.Items[0], fieldIdentifier.Model);
    }

    [Fact]
    public void AttemptedValues_PreservesRepeatedValuesAndFileMetadata()
    {
        var services = new ServiceCollection()
            .AddSingleton<IHrzFieldPathResolver>(new HrzFieldPathResolver())
            .BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = services
        };
        var form = new FormCollection(
            new Dictionary<string, StringValues>
            {
                ["displayName"] = new StringValues(["Taylor", "Morgan"]),
                ["email"] = new StringValues("riley@example.com")
            },
            new FormFileCollection
            {
                new FormFile(Stream.Null, 0, 128, "avatar", "avatar.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                }
            });

        context.Features.Set<IFormFeature>(new FormFeature(form));

        var attempted = HrzAttemptedValues.FromRequest(context.Request);

        Assert.Collection(
            attempted[_resolver.FromFieldName("DisplayName")].Values,
            value => Assert.Equal("Taylor", value),
            value => Assert.Equal("Morgan", value));
        var file = Assert.Single(attempted[_resolver.FromFieldName("Avatar")].Files);
        Assert.Equal("avatar.png", file.FileName);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal(128, file.Length);
    }

    [Fact]
    public void SubmitValidationState_IsDerivedFromSummaryAndFieldErrors()
    {
        var valid = new HrzSubmitValidationState(
            new HrzValidationRootId("create-user"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>(),
            new Dictionary<HrzFieldPath, HrzAttemptedValue>());
        var invalid = new HrzSubmitValidationState(
            new HrzValidationRootId("create-user"),
            [],
            new Dictionary<HrzFieldPath, IReadOnlyList<string>>
            {
                [_resolver.FromFieldName("Email")] = ["Required"]
            },
            new Dictionary<HrzFieldPath, HrzAttemptedValue>());

        Assert.True(valid.IsValid);
        Assert.False(invalid.IsValid);
    }

    private sealed class ValidationModel
    {
        public string Email { get; set; } = string.Empty;
        public List<ValidationItem> Items { get; set; } = [];
    }

    private sealed class ValidationItem
    {
        public string Name { get; set; } = string.Empty;
    }
}
