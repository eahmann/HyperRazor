using HyperRazor.Rendering;

namespace HyperRazor.Rendering.Tests;

public class HrzValidationPackageBoundaryTests
{
    [Fact]
    public void HyperRazorAssembly_DoesNotDeclareMovedValidationContractsUnderRenderingNamespace()
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
    public void HyperRazorRuntime_DoesNotContainValidationAuthoringFilesUnderRenderingFolder()
    {
        var repoRoot = GetRepoRoot();
        var currentAuthoringDirectory = Path.Combine(repoRoot, "src", "HyperRazor", "Rendering", "Validation", "Components");
        var retiredProjectDirectory = Path.Combine(repoRoot, "src", "HyperRazor.Rendering");

        Assert.False(
            Directory.Exists(currentAuthoringDirectory) && Directory.EnumerateFileSystemEntries(currentAuthoringDirectory).Any(),
            $"Expected '{currentAuthoringDirectory}' to be absent after the authoring move.");
        Assert.False(Directory.Exists(retiredProjectDirectory), $"Expected retired project directory '{retiredProjectDirectory}' to be removed.");
    }

    [Fact]
    public void ComponentsProject_ReferencesOnlyHtmx()
    {
        var projectText = ReadFile("src/HyperRazor.Components/HyperRazor.Components.csproj");

        Assert.Contains(@"..\HyperRazor.Htmx\HyperRazor.Htmx.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Client.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Htmx.Core.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Mvc.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Rendering.csproj", projectText, StringComparison.Ordinal);
    }

    [Fact]
    public void HyperRazorProject_ReferencesOnlyRemainingLeafPackages()
    {
        var projectText = ReadFile("src/HyperRazor/HyperRazor.csproj");

        Assert.Contains(@"..\HyperRazor.Components\HyperRazor.Components.csproj", projectText, StringComparison.Ordinal);
        Assert.Contains(@"..\HyperRazor.Htmx\HyperRazor.Htmx.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Mvc.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Rendering.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Client.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Htmx.Core.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Htmx.Components.csproj", projectText, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmxProject_DoesNotReferenceOtherHyperRazorLibraries()
    {
        var projectText = ReadFile("src/HyperRazor.Htmx/HyperRazor.Htmx.csproj");

        Assert.DoesNotContain("<ProjectReference", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.csproj", projectText, StringComparison.Ordinal);
        Assert.DoesNotContain("HyperRazor.Components.csproj", projectText, StringComparison.Ordinal);
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
    public void SamplesAndDocs_UseComponentsValidationNamespace()
    {
        var filesThatMustReferenceNewNamespace = new[]
        {
            "samples/HyperRazor.Demo.Mvc/GlobalUsings.cs",
            "samples/HyperRazor.Demo.Mvc/Components/_Imports.razor",
            "samples/HyperRazor.Demo.Mvc/Components/Fragments/UserInviteValidationForm.razor",
            "samples/HyperRazor.Demo.Mvc/Components/Fragments/MixedValidationAuthoringForm.razor",
            "README.md",
            "docs/nuget-readme.md",
            "docs/quickstart.md",
            "docs/adopting-hyperrazor.md",
            "docs/package-surface.md",
            "docs/validation-architecture.md"
        };

        foreach (var relativePath in filesThatMustReferenceNewNamespace)
        {
            Assert.Contains("HyperRazor.Components.Validation", ReadFile(relativePath), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SrcContainsOnlyThreeShippedLibraryProjects()
    {
        var repoRoot = GetRepoRoot();
        var actual = Directory
            .EnumerateFiles(Path.Combine(repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();

        var expected = new[]
        {
            "src/HyperRazor.Components/HyperRazor.Components.csproj",
            "src/HyperRazor.Htmx/HyperRazor.Htmx.csproj",
            "src/HyperRazor/HyperRazor.csproj"
        }.OrderBy(path => path).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DemoProjects_LiveUnderSamples()
    {
        Assert.True(File.Exists(GetPath("samples/HyperRazor.Demo.Api/HyperRazor.Demo.Api.csproj")));
        Assert.True(File.Exists(GetPath("samples/HyperRazor.Demo.Mvc/HyperRazor.Demo.Mvc.csproj")));
        Assert.False(File.Exists(GetPath("src/HyperRazor.Demo.Api/HyperRazor.Demo.Api.csproj")));
        Assert.False(File.Exists(GetPath("src/HyperRazor.Demo.Mvc/HyperRazor.Demo.Mvc.csproj")));
    }

    [Fact]
    public void ShippingAssemblies_DoNotExposeInternalsAcrossPackages()
    {
        var assemblyInfoFiles = Directory
            .EnumerateFiles(Path.Combine(GetRepoRoot(), "src"), "AssemblyInfo.cs", SearchOption.AllDirectories)
            .ToArray();

        Assert.DoesNotContain(assemblyInfoFiles, path => path.Replace('\\', '/').EndsWith("/src/HyperRazor.Components/Properties/AssemblyInfo.cs", StringComparison.Ordinal));

        foreach (var assemblyInfoPath in assemblyInfoFiles)
        {
            var text = File.ReadAllText(assemblyInfoPath);

            Assert.DoesNotContain(@"InternalsVisibleTo(""HyperRazor"")", text, StringComparison.Ordinal);
            Assert.DoesNotContain(@"InternalsVisibleTo(""HyperRazor.Components"")", text, StringComparison.Ordinal);
            Assert.DoesNotContain(@"InternalsVisibleTo(""HyperRazor.Htmx"")", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ComponentsProject_PreservesLegacyClientAssetBasePath()
    {
        var projectText = ReadFile("src/HyperRazor.Components/HyperRazor.Components.csproj");
        var layoutText = ReadFile("src/HyperRazor.Components/Layouts/HrzAppLayout.razor");
        var sampleLayoutText = ReadFile("samples/HyperRazor.Demo.Mvc/Components/Layouts/AppLayout.razor");

        Assert.Contains("<StaticWebAssetBasePath>_content/HyperRazor.Client</StaticWebAssetBasePath>", projectText, StringComparison.Ordinal);
        Assert.Contains("/_content/HyperRazor.Client/hyperrazor.validation.js", layoutText, StringComparison.Ordinal);
        Assert.Contains("/_content/HyperRazor.Client/hyperrazor.validation.js", sampleLayoutText, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmxPackage_ImportsMergedComponentNamespace()
    {
        var propsText = ReadFile("src/HyperRazor.Htmx/buildTransitive/HyperRazor.Htmx.props");

        Assert.Contains("HyperRazor.Htmx", propsText, StringComparison.Ordinal);
        Assert.Contains("HyperRazor.Htmx.Components", propsText, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstStopDocs_DoNotListRetiredPackagesAsInstallTargets()
    {
        var docs = new[]
        {
            "README.md",
            "docs/nuget-readme.md",
            "docs/quickstart.md"
        };
        var disallowedPatterns = new[]
        {
            "- `HyperRazor.Client`",
            "- `HyperRazor.Mvc`",
            "- `HyperRazor.Rendering`",
            "- `HyperRazor.Htmx.Core`",
            "- `HyperRazor.Htmx.Components`",
            "dotnet add package HyperRazor.Client",
            "dotnet add package HyperRazor.Mvc",
            "dotnet add package HyperRazor.Rendering",
            "dotnet add package HyperRazor.Htmx.Core",
            "dotnet add package HyperRazor.Htmx.Components"
        };

        foreach (var relativePath in docs)
        {
            var text = ReadFile(relativePath);

            foreach (var pattern in disallowedPatterns)
            {
                Assert.DoesNotContain(pattern, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void PackageSurface_AndAdoptionDocs_IncludeRetiredPackageMigrationMap()
    {
        var files = new[]
        {
            "docs/package-surface.md",
            "docs/adopting-hyperrazor.md"
        };
        var mappingLines = new[]
        {
            "`HyperRazor.Client` -> `HyperRazor.Components`",
            "`HyperRazor.Mvc` -> `HyperRazor`",
            "`HyperRazor.Rendering` -> `HyperRazor`",
            "`HyperRazor.Htmx.Core` -> `HyperRazor.Htmx`",
            "`HyperRazor.Htmx.Components` -> `HyperRazor.Htmx`"
        };

        foreach (var relativePath in files)
        {
            var text = ReadFile(relativePath);

            foreach (var mappingLine in mappingLines)
            {
                Assert.Contains(mappingLine, text, StringComparison.Ordinal);
            }
        }
    }

    private static string ReadFile(string relativePath)
    {
        return File.ReadAllText(GetPath(relativePath));
    }

    private static string GetPath(string relativePath)
    {
        return Path.Combine(GetRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
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
