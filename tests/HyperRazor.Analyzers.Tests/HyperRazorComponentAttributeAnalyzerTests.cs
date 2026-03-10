using System.Collections.Immutable;
using System.Reflection;
using HyperRazor.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HyperRazor.Analyzers.Tests;

public class HyperRazorComponentAttributeAnalyzerTests
{
    [Fact]
    public async Task ReportsClosestFormLiveIncludeOnControls()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzField<string>>(0);
                    builder.AddAttribute(1, "For", (System.Linq.Expressions.Expression<System.Func<string>>)(() => string.Empty));
                    builder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzInput>(0);
                        childBuilder.AddAttribute(1, "hx-include", "closest form");
                        childBuilder.CloseComponent();

                        childBuilder.OpenComponent<HrzValidationMessage>(2);
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(HyperRazorComponentAttributeAnalyzer.ScopeLiveIncludeDiagnosticId, diagnostic.Id);
        Assert.Contains("HrzInput", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsExplicitControlIdOverridesAndDuplicateIds()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzField<string>>(0);
                    builder.AddAttribute(1, "For", (System.Linq.Expressions.Expression<System.Func<string>>)(() => string.Empty));
                    builder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzTextArea>(0);
                        childBuilder.AddAttribute(1, "id", "custom-id");
                        childBuilder.CloseComponent();

                        childBuilder.OpenComponent<HrzInput>(2);
                        childBuilder.AddAttribute(3, "id", "custom-id");
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id).ThenBy(static diagnostic => diagnostic.Location.SourceSpan.Start),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.GeneratedIdOverrideDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.GeneratedIdOverrideDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.DuplicateControlIdDiagnosticId, diagnostic.Id));
    }

    [Fact]
    public async Task ReportsPasswordLiveValidationAndValueAttributes()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzField<string>>(0);
                    builder.AddAttribute(1, "For", (System.Linq.Expressions.Expression<System.Func<string>>)(() => string.Empty));
                    builder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzInput>(0);
                        childBuilder.AddAttribute(1, "Type", "password");
                        childBuilder.AddAttribute(2, "data-hrz-live-endpoint", "/validation/live");
                        childBuilder.AddAttribute(3, "value", "super-secret");
                        childBuilder.CloseComponent();

                        childBuilder.OpenComponent<HrzValidationMessage>(4);
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.PasswordLiveValidationDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.PasswordValueDiagnosticId, diagnostic.Id));
    }

    [Fact]
    public async Task ReportsMissingFormNameAndControlOutsideField()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzForm<string>>(0);
                    builder.AddAttribute(1, "Model", "demo");
                    builder.AddAttribute(2, "Action", "/users/invite");
                    builder.CloseComponent();

                    builder.OpenComponent<HrzInput>(3);
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.MissingFormNameDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.ControlOutsideFieldDiagnosticId, diagnostic.Id));
    }

    [Fact]
    public async Task ReportsLiveFieldMissingValidationMessage()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzField<string>>(0);
                    builder.AddAttribute(1, "For", (System.Linq.Expressions.Expression<System.Func<string>>)(() => string.Empty));
                    builder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzInput>(0);
                        childBuilder.AddAttribute(1, "hx-post", "/validation/live");
                        childBuilder.AddAttribute(2, "hx-target", "#email-message--server");
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(HyperRazorComponentAttributeAnalyzer.MissingValidationMessageDiagnosticId, diagnostic.Id);
    }

    [Fact]
    public async Task ReportsUnsupportedInputTypesAndNullableCheckboxFields()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;
            using System.Linq.Expressions;

            namespace HyperRazor.Rendering;

            public sealed class DemoModel
            {
                public bool? AcceptTerms { get; set; }
            }

            public sealed class DemoComponent
            {
                private readonly DemoModel _model = new();

                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzField<string>>(0);
                    builder.AddAttribute(1, "For", (Expression<System.Func<string>>)(() => string.Empty));
                    builder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzInput>(0);
                        childBuilder.AddAttribute(1, "Type", "date");
                        childBuilder.CloseComponent();

                        childBuilder.OpenComponent<HrzValidationMessage>(2);
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();

                    builder.OpenComponent<HrzField<bool?>>(3);
                    builder.AddAttribute(4, "For", (Expression<System.Func<bool?>>)(() => _model.AcceptTerms));
                    builder.AddAttribute(5, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                    {
                        childBuilder.OpenComponent<HrzCheckbox>(0);
                        childBuilder.CloseComponent();

                        childBuilder.OpenComponent<HrzValidationMessage>(1);
                        childBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Collection(
            diagnostics.OrderBy(static diagnostic => diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.UnsupportedInputTypeDiagnosticId, diagnostic.Id),
            diagnostic => Assert.Equal(HyperRazorComponentAttributeAnalyzer.NullableCheckboxDiagnosticId, diagnostic.Id));
    }

    [Fact]
    public async Task IgnoresValidHyperRazorComposition()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzForm<string>>(0);
                    builder.AddAttribute(1, "Model", "demo");
                    builder.AddAttribute(2, "Action", "/users/invite");
                    builder.AddAttribute(3, "FormName", "users-invite");
                    builder.AddAttribute(4, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(formBuilder =>
                    {
                        formBuilder.OpenComponent<HrzField<string>>(0);
                        formBuilder.AddAttribute(1, "For", (System.Linq.Expressions.Expression<System.Func<string>>)(() => string.Empty));
                        formBuilder.AddAttribute(2, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(childBuilder =>
                        {
                            childBuilder.OpenComponent<HrzInput>(0);
                            childBuilder.AddAttribute(1, "Type", "email");
                            childBuilder.AddAttribute(2, "data-hrz-live-endpoint", "/validation/live");
                            childBuilder.AddAttribute(3, "data-hrz-live-target", "#email-message--server");
                            childBuilder.AddAttribute(4, "data-hrz-live-state-id", "email-message--live-state");
                            childBuilder.CloseComponent();

                            childBuilder.OpenComponent<HrzValidationMessage>(5);
                            childBuilder.CloseComponent();
                        }));
                        formBuilder.CloseComponent();
                    }));
                    builder.CloseComponent();
                }
            }
            """);

        Assert.Empty(diagnostics);
    }

    private static async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: [syntaxTree, CSharpSyntaxTree.ParseText(StubFrameworkSource)],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzer = new HyperRazorComponentAttributeAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics.OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start).ToArray();
    }

    private static ImmutableArray<MetadataReference> GetMetadataReferences()
    {
        var trustedPlatformAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        Assert.False(string.IsNullOrWhiteSpace(trustedPlatformAssemblies));

        return ImmutableArray.CreateRange<MetadataReference>(trustedPlatformAssemblies!
            .Split(Path.PathSeparator)
            .Where(static path =>
            {
                var fileName = Path.GetFileName(path);
                return fileName is "System.Private.CoreLib.dll"
                    or "System.Runtime.dll"
                    or "netstandard.dll"
                    or "System.Collections.dll"
                    or "System.Linq.dll"
                    or "System.Linq.Expressions.dll";
            })
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)));
    }

    private const string StubFrameworkSource = """
        namespace Microsoft.AspNetCore.Components
        {
            public abstract class ComponentBase { }

            public delegate void RenderFragment(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder);
        }

        namespace Microsoft.AspNetCore.Components.Rendering
        {
            public sealed class RenderTreeBuilder
            {
                public void OpenComponent<T>(int sequence) { }
                public void AddAttribute(int sequence, string name, object value) { }
                public void CloseComponent() { }
            }
        }

        namespace HyperRazor.Rendering
        {
            public sealed class HrzForm<TModel> : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzField<TValue> : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzInput : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzTextArea : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzCheckbox : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzValidationMessage : Microsoft.AspNetCore.Components.ComponentBase { }
        }
        """;
}
