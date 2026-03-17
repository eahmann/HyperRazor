using HyperRazor.Components;
using HyperRazor.Components.Validation;
using HyperRazor.Rendering;

namespace HyperRazor.Tests;

public class PackageStoryShapeTests
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
    public void SrcContainsOnlyThreeShippedLibraryProjects()
    {
        var actual = EnumerateProjectPaths("src");
        var expected = new[]
        {
            "src/HyperRazor.Components/HyperRazor.Components.csproj",
            "src/HyperRazor.Htmx/HyperRazor.Htmx.csproj",
            "src/HyperRazor/HyperRazor.csproj"
        };

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SamplesStayUnderSamples()
    {
        var actual = EnumerateProjectPaths("samples");
        var expected = new[]
        {
            "samples/HyperRazor.Demo.Api/HyperRazor.Demo.Api.csproj",
            "samples/HyperRazor.Demo.Mvc/HyperRazor.Demo.Mvc.csproj"
        };

        Assert.Equal(expected, actual);
        Assert.False(File.Exists(GetPath("src/HyperRazor.Demo.Api/HyperRazor.Demo.Api.csproj")));
        Assert.False(File.Exists(GetPath("src/HyperRazor.Demo.Mvc/HyperRazor.Demo.Mvc.csproj")));
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
    public void DocsIndex_SeparatesCurrentAndHistoricalDocs()
    {
        var text = ReadFile("docs/README.md");

        Assert.Contains("## Current docs", text, StringComparison.Ordinal);
        Assert.Contains("## Historical docs", text, StringComparison.Ordinal);
        Assert.Contains("package-surface.md", text, StringComparison.Ordinal);
        Assert.Contains("adopting-hyperrazor.md", text, StringComparison.Ordinal);
        Assert.Contains("archive/", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ArchivedDocs_AreBannered()
    {
        var archiveFiles = Directory.EnumerateFiles(GetPath("docs/archive"), "*.md", SearchOption.TopDirectoryOnly).ToArray();

        Assert.NotEmpty(archiveFiles);

        foreach (var archiveFile in archiveFiles)
        {
            var text = File.ReadAllText(archiveFile);
            Assert.StartsWith("> Historical document", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CanonicalAndCurrentDocs_DoNotListRetiredPackagesAsInstallTargets()
    {
        var files = new[]
        {
            "README.md",
            "docs/README.md",
            "docs/quickstart.md",
            "docs/adopting-hyperrazor.md",
            "docs/package-surface.md",
            "docs/release-policy.md",
            "docs/validation-architecture.md"
        };
        var disallowedPatterns = new[]
        {
            "install `HyperRazor.Client`",
            "install `HyperRazor.Mvc`",
            "install `HyperRazor.Rendering`",
            "install `HyperRazor.Htmx.Core`",
            "install `HyperRazor.Htmx.Components`",
            "dotnet add package HyperRazor.Client",
            "dotnet add package HyperRazor.Mvc",
            "dotnet add package HyperRazor.Rendering",
            "dotnet add package HyperRazor.Htmx.Core",
            "dotnet add package HyperRazor.Htmx.Components"
        };

        foreach (var relativePath in files)
        {
            var text = ReadFile(relativePath);

            foreach (var pattern in disallowedPatterns)
            {
                Assert.DoesNotContain(pattern, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void CurrentDocs_DoNotReferenceRetiredProjectPathsAsCurrentStructure()
    {
        var docs = Directory
            .EnumerateFiles(GetPath("docs"), "*.md", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetRelativePath(GetRepoRoot(), path).Replace('\\', '/'))
            .Concat(["README.md"])
            .Distinct()
            .ToArray();
        var disallowedPatterns = new[]
        {
            "src/HyperRazor.Mvc/",
            "src/HyperRazor.Rendering/",
            "src/HyperRazor.Demo.Mvc/",
            "src/HyperRazor.Demo.Api/"
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

    [Fact]
    public void WorkflowAndSolutionReferencesUseRenamedFastTestProjects()
    {
        var ciText = ReadFile(".github/workflows/ci.yml");
        var solutionText = ReadFile("HyperRazor.slnx");
        var releasePolicyText = ReadFile("docs/release-policy.md");
        var currentNames = new[]
        {
            "HyperRazor.Htmx.AspNetCore.Tests",
            "HyperRazor.Htmx.Primitives.Tests",
            "HyperRazor.Tests"
        };

        foreach (var name in currentNames)
        {
            Assert.Contains(name, ciText, StringComparison.Ordinal);
            Assert.Contains(name, solutionText, StringComparison.Ordinal);
            Assert.Contains(name, releasePolicyText, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void WorkflowAndScripts_DoNotReferenceRetiredFastTestProjectNames()
    {
        var files = new[]
        {
            ".github/workflows/ci.yml",
            "docs/release-policy.md",
            "scripts/check-package-story.sh",
            "HyperRazor.slnx"
        };
        var retiredNames = new[]
        {
            "HyperRazor.Htmx.Tests",
            "HyperRazor.Htmx.Core.Tests",
            "HyperRazor.Rendering.Tests",
            "tests/HyperRazor.Htmx.Tests/",
            "tests/HyperRazor.Htmx.Core.Tests/",
            "tests/HyperRazor.Rendering.Tests/"
        };

        foreach (var relativePath in files)
        {
            var text = ReadFile(relativePath);

            foreach (var retiredName in retiredNames)
            {
                Assert.DoesNotContain(retiredName, text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void ComponentsProject_KeepsRootNamespaceAuthoringFilesAtProjectRoot()
    {
        var repoRoot = GetRepoRoot();
        var movedFiles = new[]
        {
            "HrzField.razor",
            "HrzFieldMessages.razor",
            "HrzForm.razor",
            "HrzInputCheckbox.razor",
            "HrzInputComponentBase.cs",
            "HrzInputNumber.razor",
            "HrzInputSelect.razor",
            "HrzInputSelectOption.cs",
            "HrzInputText.razor",
            "HrzInputTextArea.razor",
            "HrzLabel.razor",
            "HrzLivePolicyRegion.razor",
            "HrzValidationLivePolicyCarrier.razor",
            "HrzValidationLivePolicyRegion.razor",
            "HrzValidationMessage.razor",
            "HrzValidationServerFieldSlot.razor",
            "HrzValidationServerSummarySlot.razor",
            "HrzSummaryMessages.razor",
            "HrzValidationSummary.razor"
        };

        foreach (var fileName in movedFiles)
        {
            Assert.True(File.Exists(Path.Combine(repoRoot, "src", "HyperRazor.Components", fileName)));
            Assert.False(File.Exists(Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", fileName)));
        }
    }

    [Fact]
    public void ComponentsProject_KeepsValidationScopesUnderValidationFolder()
    {
        var repoRoot = GetRepoRoot();
        var validationFiles = new[]
        {
            "HrzFieldScope.cs",
            "HrzFormScope.cs",
            "HrzForms.cs",
            "IHrzForms.cs",
            "HrzValidationScopeFactory.cs"
        };

        foreach (var fileName in validationFiles)
        {
            Assert.True(File.Exists(Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", fileName)));
        }

        var removedFiles = new[]
        {
            Path.Combine(repoRoot, "src", "HyperRazor.Components", "HrzValidationFieldContext.cs"),
            Path.Combine(repoRoot, "src", "HyperRazor.Components", "HrzValidationFormContext.cs"),
            Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", "HrzFieldView.cs"),
            Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", "HrzFormView.cs"),
            Path.Combine(repoRoot, "src", "HyperRazor.Components", "Validation", "HrzValidationViewFactory.cs")
        };

        foreach (var filePath in removedFiles)
        {
            Assert.False(File.Exists(filePath));
        }
    }

    [Fact]
    public void SamplesAndCurrentDocs_UseComponentsValidationNamespace()
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
    public void ShippingAssemblies_DoNotExposeInternalsAcrossPackages()
    {
        var assemblyInfoFiles = Directory
            .EnumerateFiles(GetPath("src"), "AssemblyInfo.cs", SearchOption.AllDirectories)
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
    public void ComponentsAssembly_ExposesValidationScopes_AndRetiresViewTypes()
    {
        var assembly = typeof(IHrzForms).Assembly;

        Assert.True(typeof(IHrzForms).IsPublic);
        Assert.True(typeof(HrzFormScope).IsPublic);
        Assert.True(typeof(HrzFormScope<>).IsPublic);
        Assert.True(typeof(HrzFieldScope).IsPublic);
        Assert.True(typeof(HrzFieldScope<>).IsPublic);

        Assert.Null(assembly.GetType("HyperRazor.Components.Validation.HrzFormView", throwOnError: false, ignoreCase: false));
        Assert.Null(assembly.GetType("HyperRazor.Components.Validation.HrzFormView`1", throwOnError: false, ignoreCase: false));
        Assert.Null(assembly.GetType("HyperRazor.Components.Validation.HrzFieldView", throwOnError: false, ignoreCase: false));
        Assert.Null(assembly.GetType("HyperRazor.Components.Validation.HrzFieldView`1", throwOnError: false, ignoreCase: false));

        Assert.Equal(typeof(HrzFormScope), typeof(HrzSummaryMessages).GetProperty(nameof(HrzSummaryMessages.Form))!.PropertyType);
        Assert.Equal(typeof(HrzFieldScope), typeof(HrzFieldMessages).GetProperty(nameof(HrzFieldMessages.Field))!.PropertyType);
        Assert.Equal(typeof(HrzFormScope), typeof(HrzLivePolicyRegion).GetProperty(nameof(HrzLivePolicyRegion.Form))!.PropertyType);
        Assert.Equal(typeof(HrzFormScope), typeof(HrzValidationLivePolicyRegion).GetProperty(nameof(HrzValidationLivePolicyRegion.Form))!.PropertyType);
    }

    [Fact]
    public void CurrentDocs_Describe_Component_And_Builder_ValidationAuthoringLanes()
    {
        var files = new[]
        {
            "README.md",
            "docs/adopting-hyperrazor.md",
            "docs/nuget-readme.md",
            "docs/package-surface.md",
            "docs/quickstart.md",
            "docs/validation-architecture.md"
        };

        foreach (var relativePath in files)
        {
            var text = ReadFile(relativePath);

            Assert.Contains("IHrzForms", text, StringComparison.Ordinal);
            Assert.Contains("HrzFormScope", text, StringComparison.Ordinal);
            Assert.Contains("HrzFieldScope", text, StringComparison.Ordinal);
        }
    }

    private static string[] EnumerateProjectPaths(string relativeRoot)
    {
        return Directory
            .EnumerateFiles(GetPath(relativeRoot), "*.csproj", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(GetRepoRoot(), path).Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();
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
