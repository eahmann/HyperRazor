using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HyperRazor.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HyperRazorComponentAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string ScopeLiveIncludeDiagnosticId = "HRZ001";
    public const string GeneratedIdOverrideDiagnosticId = "HRZ002";
    public const string PasswordLiveValidationDiagnosticId = "HRZ003";
    public const string PasswordValueDiagnosticId = "HRZ004";
    public const string MissingFormNameDiagnosticId = "HRZ005";
    public const string ControlOutsideFieldDiagnosticId = "HRZ006";
    public const string MissingValidationMessageDiagnosticId = "HRZ007";
    public const string UnsupportedInputTypeDiagnosticId = "HRZ008";
    public const string NullableCheckboxDiagnosticId = "HRZ009";
    public const string DuplicateControlIdDiagnosticId = "HRZ010";

    private const string Category = "Usage";
    private const string RenderTreeBuilderMetadataName = "Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder";
    private const string HrzFormMetadataName = "HyperRazor.Rendering.HrzForm<TModel>";
    private const string HrzFieldMetadataName = "HyperRazor.Rendering.HrzField<TValue>";
    private const string HrzInputMetadataName = "HyperRazor.Rendering.HrzInput";
    private const string HrzTextAreaMetadataName = "HyperRazor.Rendering.HrzTextArea";
    private const string HrzCheckboxMetadataName = "HyperRazor.Rendering.HrzCheckbox";
    private const string HrzValidationMessageMetadataName = "HyperRazor.Rendering.HrzValidationMessage";

    private static readonly ImmutableHashSet<string> ControlMetadataNames =
        ImmutableHashSet.Create(StringComparer.Ordinal, HrzInputMetadataName, HrzTextAreaMetadataName, HrzCheckboxMetadataName);

    private static readonly ImmutableHashSet<string> LiveValidationAttributeNames =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "hx-post",
            "hx-trigger",
            "hx-target",
            "hx-swap",
            "hx-include",
            "hx-vals",
            "data-hrz-live-endpoint",
            "data-hrz-live-trigger",
            "data-hrz-live-target",
            "data-hrz-live-swap",
            "data-hrz-live-include",
            "data-hrz-live-vals");

    private static readonly ImmutableHashSet<string> SupportedInputTypes =
        ImmutableHashSet.Create(
            StringComparer.OrdinalIgnoreCase,
            "text",
            "email",
            "search",
            "tel",
            "url",
            "password",
            "number");

    private static readonly DiagnosticDescriptor ScopeLiveIncludeRule = new(
        ScopeLiveIncludeDiagnosticId,
        "Avoid whole-form live validation transport",
        "Avoid 'hx-include=\"closest form\"' on '{0}'. Scope live validation requests to dependent fields instead.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GeneratedIdOverrideRule = new(
        GeneratedIdOverrideDiagnosticId,
        "Avoid overriding generated HyperRazor control ids",
        "Avoid overriding the generated 'id' on '{0}'. Generated ids keep labels and validation message slots aligned.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PasswordLiveValidationRule = new(
        PasswordLiveValidationDiagnosticId,
        "Password inputs should not declare live validation transport",
        "'HrzInput Type=\"password\"' should not declare live-validation attribute '{0}'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PasswordValueRule = new(
        PasswordValueDiagnosticId,
        "Password inputs should not declare values",
        "'HrzInput Type=\"password\"' should not declare a 'value' attribute.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingFormNameRule = new(
        MissingFormNameDiagnosticId,
        "HrzForm requires FormName",
        "'HrzForm' should declare a non-empty 'FormName'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ControlOutsideFieldRule = new(
        ControlOutsideFieldDiagnosticId,
        "Controls should be placed inside HrzField",
        "'{0}' should be placed inside 'HrzField'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingValidationMessageRule = new(
        MissingValidationMessageDiagnosticId,
        "Live-enabled fields should render HrzValidationMessage",
        "Live-enabled field '{0}' should include 'HrzValidationMessage' in the same 'HrzField'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedInputTypeRule = new(
        UnsupportedInputTypeDiagnosticId,
        "Unsupported HrzInput type",
        "'HrzInput' does not support input type '{0}'.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NullableCheckboxRule = new(
        NullableCheckboxDiagnosticId,
        "Nullable checkbox fields are not supported in v1",
        "'HrzCheckbox' only supports non-nullable bool fields in v1.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateControlIdRule = new(
        DuplicateControlIdDiagnosticId,
        "Avoid duplicate explicit HyperRazor control ids",
        "Explicit control id '{0}' is already used by another HyperRazor control in this render tree.",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            ScopeLiveIncludeRule,
            GeneratedIdOverrideRule,
            PasswordLiveValidationRule,
            PasswordValueRule,
            MissingFormNameRule,
            ControlOutsideFieldRule,
            MissingValidationMessageRule,
            UnsupportedInputTypeRule,
            NullableCheckboxRule,
            DuplicateControlIdRule
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration, SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        SyntaxNode? scope = context.Node switch
        {
            MethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
            LocalFunctionStatementSyntax localFunction => (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
            _ => null
        };

        if (scope is null)
        {
            return;
        }

        var frames = new Stack<ComponentFrame>();
        var explicitControlIds = new Dictionary<string, CapturedAttribute>(StringComparer.Ordinal);
        var invocations = scope
            .DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .OrderBy(static invocation => invocation.SpanStart);

        foreach (var invocation in invocations)
        {
            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            if (!IsRenderTreeBuilderMethod(methodSymbol))
            {
                continue;
            }

            if (methodSymbol.Name == "OpenComponent" && methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length == 1)
            {
                frames.Push(new ComponentFrame(methodSymbol.TypeArguments[0], invocation.GetLocation()));
                continue;
            }

            if (methodSymbol.Name == "AddAttribute" && frames.Count > 0)
            {
                CaptureAttribute(frames.Peek(), invocation, context.SemanticModel, context.CancellationToken);
                continue;
            }

            if (methodSymbol.Name == "CloseComponent" && frames.Count > 0)
            {
                var frame = frames.Pop();
                AnalyzeClosedComponent(context, frame, frames, explicitControlIds);

                if (frames.Count > 0)
                {
                    PropagateToParent(frames.Peek(), frame);
                }
            }
        }
    }

    private static void CaptureAttribute(
        ComponentFrame frame,
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (invocation.ArgumentList.Arguments.Count < 3)
        {
            return;
        }

        var nameExpression = UnwrapExpression(invocation.ArgumentList.Arguments[1].Expression);
        var nameConstant = semanticModel.GetConstantValue(nameExpression, cancellationToken);
        if (!nameConstant.HasValue || nameConstant.Value is not string attributeName)
        {
            return;
        }

        var valueExpression = UnwrapExpression(invocation.ArgumentList.Arguments[2].Expression);
        var valueConstant = semanticModel.GetConstantValue(valueExpression, cancellationToken);
        frame.Attributes[attributeName] = new CapturedAttribute(
            attributeName,
            valueConstant.HasValue ? valueConstant.Value?.ToString() : null,
            invocation.GetLocation());

        if (string.Equals(attributeName, "For", StringComparison.Ordinal)
            && IsComponent(frame.ComponentType, HrzFieldMetadataName))
        {
            frame.FieldValueType = ResolveFieldValueType(semanticModel, valueExpression, cancellationToken);
        }
    }

    private static void AnalyzeClosedComponent(
        SyntaxNodeAnalysisContext context,
        ComponentFrame frame,
        IReadOnlyCollection<ComponentFrame> ancestors,
        IDictionary<string, CapturedAttribute> explicitControlIds)
    {
        var componentMetadataName = GetComponentMetadataName(frame.ComponentType);
        if (componentMetadataName is null)
        {
            return;
        }

        if (IsComponent(frame.ComponentType, HrzFormMetadataName))
        {
            if (!frame.Attributes.TryGetValue("FormName", out var formNameAttribute)
                || string.IsNullOrWhiteSpace(formNameAttribute.ConstantValue))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingFormNameRule,
                    formNameAttribute?.Location ?? frame.Location));
            }
        }

        if (ControlMetadataNames.Contains(componentMetadataName))
        {
            if (!HasFieldAncestor(ancestors))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ControlOutsideFieldRule,
                    frame.Location,
                    frame.ComponentType.Name));
            }

            if (frame.Attributes.TryGetValue("hx-include", out var includeAttribute)
                && string.Equals(includeAttribute.ConstantValue, "closest form", StringComparison.OrdinalIgnoreCase))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ScopeLiveIncludeRule,
                    includeAttribute.Location,
                    frame.ComponentType.Name));
            }

            if (frame.Attributes.TryGetValue("id", out var idAttribute))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    GeneratedIdOverrideRule,
                    idAttribute.Location,
                    frame.ComponentType.Name));

                if (!string.IsNullOrWhiteSpace(idAttribute.ConstantValue))
                {
                    if (explicitControlIds.TryGetValue(idAttribute.ConstantValue, out _))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DuplicateControlIdRule,
                            idAttribute.Location,
                            idAttribute.ConstantValue));
                    }
                    else
                    {
                        explicitControlIds[idAttribute.ConstantValue] = idAttribute;
                    }
                }
            }

            if (IsLiveEnabled(frame))
            {
                frame.HasLiveEnabledControl = true;
                frame.LiveEnabledControlName = frame.ComponentType.Name;
                frame.LiveEnabledLocation ??= GetLiveDiagnosticLocation(frame);
            }
        }

        if (IsComponent(frame.ComponentType, HrzValidationMessageMetadataName))
        {
            frame.HasValidationMessage = true;
        }

        if (IsComponent(frame.ComponentType, HrzCheckboxMetadataName))
        {
            frame.HasCheckboxControl = true;
        }

        if (IsComponent(frame.ComponentType, HrzFieldMetadataName)
            && frame.HasLiveEnabledControl
            && !frame.HasValidationMessage)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MissingValidationMessageRule,
                frame.LiveEnabledLocation ?? frame.Location,
                frame.LiveEnabledControlName ?? "HrzField"));
        }

        if (IsComponent(frame.ComponentType, HrzFieldMetadataName)
            && frame.HasCheckboxControl
            && IsNullableBool(frame.FieldValueType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                NullableCheckboxRule,
                GetFieldForLocation(frame) ?? frame.Location));
        }

        if (!IsComponent(frame.ComponentType, HrzInputMetadataName))
        {
            return;
        }

        if (!frame.Attributes.TryGetValue("Type", out var typeAttribute)
            && !frame.Attributes.TryGetValue("type", out typeAttribute))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(typeAttribute.ConstantValue)
            && !SupportedInputTypes.Contains(typeAttribute.ConstantValue))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UnsupportedInputTypeRule,
                typeAttribute.Location,
                typeAttribute.ConstantValue));
        }

        if (!string.Equals(typeAttribute.ConstantValue, "password", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var liveAttributeName in LiveValidationAttributeNames)
        {
            if (!frame.Attributes.TryGetValue(liveAttributeName, out var liveAttribute))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                PasswordLiveValidationRule,
                liveAttribute.Location,
                liveAttribute.Name));
            break;
        }

        if (frame.Attributes.TryGetValue("value", out var valueAttribute))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                PasswordValueRule,
                valueAttribute.Location));
        }
    }

    private static void PropagateToParent(ComponentFrame parent, ComponentFrame child)
    {
        if (child.HasLiveEnabledControl)
        {
            parent.HasLiveEnabledControl = true;
            parent.LiveEnabledControlName ??= child.LiveEnabledControlName;
            parent.LiveEnabledLocation ??= child.LiveEnabledLocation;
        }

        if (child.HasCheckboxControl)
        {
            parent.HasCheckboxControl = true;
        }

        if (child.HasValidationMessage)
        {
            parent.HasValidationMessage = true;
        }
    }

    private static bool IsLiveEnabled(ComponentFrame frame)
    {
        foreach (var liveAttributeName in LiveValidationAttributeNames)
        {
            if (frame.Attributes.TryGetValue(liveAttributeName, out var attribute)
                && !string.IsNullOrWhiteSpace(attribute.ConstantValue))
            {
                return true;
            }
        }

        return false;
    }

    private static Location GetLiveDiagnosticLocation(ComponentFrame frame)
    {
        foreach (var liveAttributeName in LiveValidationAttributeNames)
        {
            if (frame.Attributes.TryGetValue(liveAttributeName, out var attribute)
                && !string.IsNullOrWhiteSpace(attribute.ConstantValue))
            {
                return attribute.Location;
            }
        }

        return frame.Location;
    }

    private static bool HasFieldAncestor(IEnumerable<ComponentFrame> ancestors)
    {
        foreach (var ancestor in ancestors)
        {
            if (IsComponent(ancestor.ComponentType, HrzFieldMetadataName))
            {
                return true;
            }
        }

        return false;
    }

    private static ITypeSymbol? ResolveFieldValueType(
        SemanticModel semanticModel,
        ExpressionSyntax valueExpression,
        CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(valueExpression, cancellationToken).ConvertedType
            ?? semanticModel.GetTypeInfo(valueExpression, cancellationToken).Type;

        if (type is not INamedTypeSymbol namedType
            || namedType.TypeArguments.Length != 1)
        {
            return null;
        }

        var argumentType = namedType.TypeArguments[0];
        if (argumentType is not INamedTypeSymbol funcType
            || funcType.TypeArguments.Length != 1)
        {
            return null;
        }

        return funcType.TypeArguments[0];
    }

    private static bool IsNullableBool(ITypeSymbol? typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1
            && namedType.TypeArguments[0].SpecialType == SpecialType.System_Boolean;
    }

    private static Location? GetFieldForLocation(ComponentFrame frame)
    {
        if (frame.Attributes.TryGetValue("For", out var fieldAttribute))
        {
            return fieldAttribute.Location;
        }

        return null;
    }

    private static bool IsRenderTreeBuilderMethod(IMethodSymbol methodSymbol)
    {
        return string.Equals(methodSymbol.ContainingType.ToDisplayString(), RenderTreeBuilderMetadataName, StringComparison.Ordinal)
            && (methodSymbol.Name == "OpenComponent"
                || methodSymbol.Name == "AddAttribute"
                || methodSymbol.Name == "CloseComponent");
    }

    private static bool IsComponent(ITypeSymbol componentType, string metadataName)
    {
        return string.Equals(GetComponentMetadataName(componentType), metadataName, StringComparison.Ordinal);
    }

    private static string? GetComponentMetadataName(ITypeSymbol componentType)
    {
        return componentType is INamedTypeSymbol namedType
            ? namedType.ConstructedFrom.ToDisplayString()
            : componentType.ToDisplayString();
    }

    private static ExpressionSyntax UnwrapExpression(ExpressionSyntax expression)
    {
        while (true)
        {
            expression = expression switch
            {
                ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
                CastExpressionSyntax cast => cast.Expression,
                _ => expression
            };

            if (expression is not ParenthesizedExpressionSyntax
                && expression is not CastExpressionSyntax)
            {
                return expression;
            }
        }
    }

    private sealed class ComponentFrame
    {
        public ComponentFrame(ITypeSymbol componentType, Location location)
        {
            ComponentType = componentType;
            Location = location;
        }

        public ITypeSymbol ComponentType { get; }

        public Location Location { get; }

        public Dictionary<string, CapturedAttribute> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ITypeSymbol? FieldValueType { get; set; }

        public bool HasValidationMessage { get; set; }

        public bool HasLiveEnabledControl { get; set; }

        public string? LiveEnabledControlName { get; set; }

        public Location? LiveEnabledLocation { get; set; }

        public bool HasCheckboxControl { get; set; }
    }

    private sealed class CapturedAttribute
    {
        public CapturedAttribute(string name, string? constantValue, Location location)
        {
            Name = name;
            ConstantValue = constantValue;
            Location = location;
        }

        public string Name { get; }

        public string? ConstantValue { get; }

        public Location Location { get; }
    }
}
