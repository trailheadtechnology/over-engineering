using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OverEngineering.Analyzers;

/// <summary>
/// OE0003: flags interfaces declared in this project that exactly one type implements.
/// This needs the whole compilation, so in an IDE it only runs with full-solution analysis on.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SingleImplementationInterfaceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.SingleImplementationInterface);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var interfaces = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            var implementations = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

            start.RegisterSymbolAction(ctx =>
            {
                var type = (INamedTypeSymbol)ctx.Symbol;
                if (type.TypeKind == TypeKind.Interface)
                {
                    interfaces.TryAdd(type, 0);
                    return;
                }

                foreach (var implemented in type.AllInterfaces)
                {
                    implementations
                        .GetOrAdd(implemented.OriginalDefinition, _ => new ConcurrentBag<INamedTypeSymbol>())
                        .Add(type);
                }
            }, SymbolKind.NamedType);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (var iface in interfaces.Keys)
                {
                    if (!implementations.TryGetValue(iface, out var impls))
                        continue;

                    var distinct = impls.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default).ToList();
                    if (distinct.Count != 1)
                        continue;

                    var location = iface.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location is null)
                        continue;

                    end.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.SingleImplementationInterface, location, iface.Name, distinct[0].Name));
                }
            });
        });
    }
}
