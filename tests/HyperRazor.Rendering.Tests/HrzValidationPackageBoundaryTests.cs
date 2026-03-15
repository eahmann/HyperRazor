using HyperRazor.Rendering;

namespace HyperRazor.Rendering.Tests;

public class HrzValidationPackageBoundaryTests
{
    [Fact]
    public void RenderingAssembly_DoesNotDeclareMovedValidationContracts()
    {
        var assembly = typeof(HrzRenderService).Assembly;
        var legacyTypeNames = new[]
        {
            "HyperRazor.Rendering.HrzValidationRootId",
            "HyperRazor.Rendering.HrzFieldPath",
            "HyperRazor.Rendering.HrzFieldPaths",
            "HyperRazor.Rendering.HrzAttemptedFile",
            "HyperRazor.Rendering.HrzAttemptedValue",
            "HyperRazor.Rendering.HrzAttemptedValues",
            "HyperRazor.Rendering.HrzSubmitValidationState",
            "HyperRazor.Rendering.HrzLiveValidationPatch",
            "HyperRazor.Rendering.HrzLiveValidationPolicy",
            "HyperRazor.Rendering.HrzValidationScope",
            "HyperRazor.Rendering.HrzFormPostState`1",
            "HyperRazor.Rendering.HrzValidationHttpContextExtensions",
            "HyperRazor.Rendering.HrzSubmitValidationStateExtensions",
            "HyperRazor.Rendering.HrzFormRendering",
            "HyperRazor.Rendering.IHrzFieldPathResolver",
            "HyperRazor.Rendering.IHrzLiveValidationPolicyResolver",
            "HyperRazor.Rendering.IHrzClientValidationMetadataProvider",
            "HyperRazor.Rendering.IHrzModelValidator"
        };

        foreach (var typeName in legacyTypeNames)
        {
            Assert.Null(assembly.GetType(typeName, throwOnError: false, ignoreCase: false));
        }
    }

    [Fact]
    public void RenderingProject_DoesNotContainValidationAuthoringFiles()
    {
        var authoringDirectory = Path.Combine(GetRepoRoot(), "src", "HyperRazor.Rendering", "Validation", "Components");

        Assert.False(
            Directory.Exists(authoringDirectory) && Directory.EnumerateFileSystemEntries(authoringDirectory).Any(),
            $"Expected '{authoringDirectory}' to be absent after the authoring move.");
    }

    [Fact]
    public void ComponentsProject_DoesNotReferenceRendering()
    {
        var projectText = File.ReadAllText(Path.Combine(
            GetRepoRoot(),
            "src",
            "HyperRazor.Components",
            "HyperRazor.Components.csproj"));

        Assert.DoesNotContain("HyperRazor.Rendering.csproj", projectText, StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentsProject_KeepsRootNamespaceAuthoringFilesAtProjectRoot()
    {
        var repoRoot = GetRepoRoot();
        var movedFiles = new[]
        {
            "HrzField.razor",
            "HrzForm.razor",
            "HrzInputCheckbox.razor",
            "HrzInputComponentBase.cs",
            "HrzInputNumber.razor",
            "HrzInputSelect.razor",
            "HrzInputSelectOption.cs",
            "HrzInputText.razor",
            "HrzInputTextArea.razor",
            "HrzLabel.razor",
            "HrzValidationFieldContext.cs",
            "HrzValidationFormContext.cs",
            "HrzValidationLivePolicyCarrier.razor",
            "HrzValidationLivePolicyRegion.razor",
            "HrzValidationMessage.razor",
            "HrzValidationServerFieldSlot.razor",
            "HrzValidationServerSummarySlot.razor",
            "HrzValidationSummary.razor"
        };

        foreach (var fileName in movedFiles)
        {
            Assert.True(File.Exists(Path.Combine(repoRoot, "src", "HyperRazor.Components", fileName)));
            Assert.False(File.Exists(Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", fileName)));
        }
    }

    [Fact]
    public void DemoAndDocs_UseComponentsValidationNamespace()
    {
        var repoRoot = GetRepoRoot();
        var filesThatMustReferenceNewNamespace = new[]
        {
            "src/HyperRazor.Demo.Mvc/GlobalUsings.cs",
            "src/HyperRazor.Demo.Mvc/Components/_Imports.razor",
            "src/HyperRazor.Demo.Mvc/Components/Fragments/UserInviteValidationForm.razor",
            "src/HyperRazor.Demo.Mvc/Components/Fragments/MixedValidationAuthoringForm.razor",
            "README.md",
            "docs/nuget-readme.md",
            "docs/quickstart.md",
            "docs/adopting-hyperrazor.md",
            "docs/package-surface.md",
            "docs/validation-architecture.md"
        };

        foreach (var relativePath in filesThatMustReferenceNewNamespace)
        {
            var text = File.ReadAllText(Path.Combine(repoRoot, relativePath));
            Assert.Contains("HyperRazor.Components.Validation", text, StringComparison.Ordinal);
        }
    }

    private static string GetRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.GetFiles("HyperRazor.slnx").Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the HyperRazor repository root.");
    }
}
