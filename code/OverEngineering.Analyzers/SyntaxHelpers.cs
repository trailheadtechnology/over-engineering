using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OverEngineering.Analyzers;

internal static class SyntaxHelpers
{
    // The one expression a method evaluates, for "=> x", "{ return x; }", or "{ x; }", with await unwrapped
    public static ExpressionSyntax? GetSingleExpression(MethodDeclarationSyntax method)
    {
        var expression = method.ExpressionBody?.Expression ?? (method.Body?.Statements is { Count: 1 } statements
            ? statements[0] switch
            {
                ReturnStatementSyntax r => r.Expression,
                ExpressionStatementSyntax e => e.Expression,
                _ => null,
            }
            : null);

        return expression is AwaitExpressionSyntax await ? await.Expression : expression;
    }
}
