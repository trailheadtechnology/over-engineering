using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace OverEngineering.Analyzers;

/// <summary>
/// OE0006: flags a method on a *Factory class whose whole body is <c>new X(...)</c> with only its own
/// parameters or literals as arguments, e.g. <c>public Query Create(string code) =&gt; new(code);</c>
/// A factory that maps or decides something (any other expression in the arguments) is left alone.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TrivialFactoryAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(Descriptors.TrivialFactory);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(Check, SyntaxKind.MethodDeclaration);
    }

    private static void Check(SyntaxNodeAnalysisContext ctx)
    {
        var method = (MethodDeclarationSyntax)ctx.Node;
        if (method.Parent is not TypeDeclarationSyntax type || !type.Identifier.Text.EndsWith("Factory"))
            return;

        if (SyntaxHelpers.GetSingleExpression(method) is not BaseObjectCreationExpressionSyntax creation
            || creation.Initializer is not null)
        {
            return;
        }

        if (creation.ArgumentList is { } arguments)
        {
            foreach (var argument in arguments.Arguments)
            {
                if (argument.Expression is not (IdentifierNameSyntax or LiteralExpressionSyntax))
                    return;
            }
        }

        var created = ctx.SemanticModel.GetTypeInfo(creation, ctx.CancellationToken).Type?.Name ?? "?";
        ctx.ReportDiagnostic(Diagnostic.Create(
            Descriptors.TrivialFactory, method.Identifier.GetLocation(),
            $"{type.Identifier.Text}.{method.Identifier.Text}", created));
    }
}
