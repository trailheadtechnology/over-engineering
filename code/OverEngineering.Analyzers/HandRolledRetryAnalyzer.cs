using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OverEngineering.Analyzers;

/// <summary>
/// OE0009: flags a loop that both catches exceptions and waits (Task.Delay or Thread.Sleep).
/// That combination is a retry policy written by hand.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandRolledRetryAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.HandRolledRetry);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check,
            SyntaxKind.ForStatement, SyntaxKind.WhileStatement, SyntaxKind.DoStatement, SyntaxKind.ForEachStatement);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        var loop = (StatementSyntax)ctx.Node;
        var body = loop switch
        {
            ForStatementSyntax f => f.Statement,
            WhileStatementSyntax w => w.Statement,
            DoStatementSyntax d => d.Statement,
            ForEachStatementSyntax e => e.Statement,
            _ => null,
        };
        if (body is null || !body.DescendantNodes().OfType<CatchClauseSyntax>().Any())
            return;

        var waits = body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation =>
            ctx.SemanticModel.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is IMethodSymbol m
            && (m.Name, m.ContainingType.ToDisplayString()) is ("Delay", "System.Threading.Tasks.Task")
                                                             or ("Sleep", "System.Threading.Thread"));
        if (!waits)
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(Descriptors.HandRolledRetry, loop.GetFirstToken().GetLocation()));
    }
}
