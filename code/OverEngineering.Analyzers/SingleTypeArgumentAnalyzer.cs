using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OverEngineering.Analyzers;

/// <summary>
/// OE0002: flags generic types declared in this project that are only ever constructed with one
/// set of type arguments, e.g. <c>Entity&lt;TId&gt;</c> when every entity is <c>Entity&lt;Guid&gt;</c>.
/// Uses inside other generic code (where the argument is still a type parameter) and typeof(X&lt;&gt;) don't count.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleTypeArgumentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.SingleTypeArgument);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var uses = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentDictionary<string, byte>>(SymbolEqualityComparer.Default);

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.SemanticModel.GetSymbolInfo((GenericNameSyntax)ctx.Node, ctx.CancellationToken).Symbol
                    is not INamedTypeSymbol type)
                {
                    return;
                }

                var definition = type.OriginalDefinition;
                if (!definition.Locations.Any(l => l.IsInSource)
                    || type.IsUnboundGenericType
                    || type.TypeArguments.Any(ContainsTypeParameter))
                {
                    return;
                }

                uses.GetOrAdd(definition, _ => new ConcurrentDictionary<string, byte>())
                    .TryAdd(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat), 0);
            }, SyntaxKind.GenericName);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (var pair in uses)
                {
                    if (pair.Value.Count != 1)
                        continue;

                    var location = pair.Key.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location is null)
                        continue;

                    end.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.SingleTypeArgument, location,
                        pair.Key.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                        pair.Value.Keys.Single()));
                }
            });
        });
    }

    private static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
        _ => false,
    };
}
