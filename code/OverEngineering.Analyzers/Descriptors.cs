using Microsoft.CodeAnalysis;

namespace OverEngineering.Analyzers;

/// <summary>
/// Diagnostic IDs follow the talk's over-engineering type numbers: OE0003 is Type 3, and so on.
/// Every one of these is a judgment call, so they ship as Info. Teams raise them in .editorconfig.
/// </summary>
internal static class Descriptors
{
    private const string Category = "OverEngineering";

    public static readonly DiagnosticDescriptor SingleTypeArgument = new(
        id: "OE0002",
        title: "Generic type is only ever used with one type argument",
        messageFormat: "'{0}' is only ever used as '{1}'; replace the type parameter with the concrete type (Type 2: OO Gymnastics)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A type parameter that only ever receives one type argument is flexibility nobody uses.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor SingleImplementationInterface = new(
        id: "OE0003",
        title: "Interface has only one implementation",
        messageFormat: "Interface '{0}' has one implementation ('{1}'); use the class directly until a second one exists (Type 3: Over-Abstraction)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "An interface with one implementation adds indirection without adding flexibility. Extracting one later is cheap.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor TrivialFactory = new(
        id: "OE0006",
        title: "Factory method only calls a constructor",
        messageFormat: "'{0}' only passes its arguments to 'new {1}'; call the constructor directly (Type 6: Overuse of Design Patterns)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A factory earns its keep when creation needs logic. One that only calls new is ceremony.");

    public static readonly DiagnosticDescriptor PassThroughMethod = new(
        id: "OE0007",
        title: "Method only forwards its arguments",
        messageFormat: "'{0}' only forwards its arguments to '{1}' (Type 7: Lasagna Architecture)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A method that only forwards its arguments is a layer with no behavior of its own.");

    public static readonly DiagnosticDescriptor HandRolledRetry = new(
        id: "OE0009",
        title: "Hand-rolled retry loop",
        messageFormat: "Hand-rolled retry loop; use a proven resilience library such as Polly or Microsoft.Extensions.Resilience (Type 9: Rolling Your Own)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Retry logic has subtle edge cases (backoff, jitter, cancellation, which errors are transient). Proven libraries already handle them.");
}
