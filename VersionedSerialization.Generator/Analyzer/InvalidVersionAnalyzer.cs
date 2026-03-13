using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using VersionedSerialization.Generator.Utils;

namespace VersionedSerialization.Generator.Analyzer;

#pragma warning disable RS1038
[DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1038
public class InvalidVersionAnalyzer : DiagnosticAnalyzer
{
    private const string Identifier = "VS0001";

    private const string Category = "Usage";
    private const string Title = "Invalid version string in attribute";
    private const string MessageFormat = "Invalid version string";
    private const string Description =
        "The version needs to be specified in the following format: <major>.<minor>. Tags are not supported.";

    private static readonly DiagnosticDescriptor Descriptor = new(Identifier, Title, MessageFormat,
        Category, DiagnosticSeverity.Error, true, Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (context.ContainingSymbol == null)
            return;

        var compilation = context.Compilation;
        var versionConditionAttribute = compilation.GetTypeByMetadataName(Constants.VersionConditionAttribute);

        foreach (var attribute in context.ContainingSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, versionConditionAttribute))
            {
                if (attribute.ApplicationSyntaxReference == null)
                    continue;

                foreach (var argument in attribute.NamedArguments)
                {
                    var name = argument.Key;
                    if (name is Constants.LessThan or Constants.GreaterThan or Constants.EqualTo
                        or Constants.LessThanOrEqual or Constants.GreaterThanOrEqual)
                    {
                        var value = (string)argument.Value.Value!;

                        if (!StructVersion.TryParse(value, out var ver) || ver.Tag != null)
                        {
                            var span = attribute.ApplicationSyntaxReference.Span;
                            var location = attribute.ApplicationSyntaxReference.SyntaxTree.GetLocation(span);
                            var diagnostic = Diagnostic.Create(Descriptor, location);
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}