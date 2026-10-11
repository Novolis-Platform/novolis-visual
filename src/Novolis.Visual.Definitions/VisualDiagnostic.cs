namespace Novolis.Visual.Definitions;

/// <summary>
/// A deterministic schema validation diagnostic.
/// </summary>
public sealed record VisualDiagnostic(
    VisualDiagnosticSeverity Severity,
    string Code,
    string Message,
    NodeId? NodeId = null,
    ConnectionId? ConnectionId = null);
