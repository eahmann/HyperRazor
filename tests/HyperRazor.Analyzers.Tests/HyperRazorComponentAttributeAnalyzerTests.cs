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
                    builder.OpenComponent<HrzInput>(0);
                    builder.AddAttribute(1, "hx-include", "closest form");
                    builder.CloseComponent();
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(HyperRazorComponentAttributeAnalyzer.ScopeLiveIncludeDiagnosticId, diagnostic.Id);
        Assert.Contains("HrzInput", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReportsExplicitControlIdOverrides()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzTextArea>(0);
                    builder.AddAttribute(1, "id", "custom-id");
                    builder.CloseComponent();
                }
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(HyperRazorComponentAttributeAnalyzer.GeneratedIdOverrideDiagnosticId, diagnostic.Id);
        Assert.Contains("HrzTextArea", diagnostic.GetMessage(), StringComparison.Ordinal);
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
                    builder.OpenComponent<HrzInput>(0);
                    builder.AddAttribute(1, "Type", "password");
                    builder.AddAttribute(2, "hx-post", "/validation/live");
                    builder.AddAttribute(3, "value", "super-secret");
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
    public async Task IgnoresScopedIncludesAndNonPasswordControls()
    {
        var diagnostics = await GetDiagnosticsAsync("""
            using Microsoft.AspNetCore.Components.Rendering;

            namespace HyperRazor.Rendering;

            public sealed class DemoComponent
            {
                public void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.OpenComponent<HrzInput>(0);
                    builder.AddAttribute(1, "Type", "email");
                    builder.AddAttribute(2, "hx-include", "#users-invite-displayname");
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
                    or "System.Linq.dll";
            })
            .Select(static path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location)));
    }

    private const string StubFrameworkSource = """
        namespace Microsoft.AspNetCore.Components
        {
            public abstract class ComponentBase { }
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
            public sealed class HrzInput : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzTextArea : Microsoft.AspNetCore.Components.ComponentBase { }
            public sealed class HrzCheckbox : Microsoft.AspNetCore.Components.ComponentBase { }
        }
        """;
}
