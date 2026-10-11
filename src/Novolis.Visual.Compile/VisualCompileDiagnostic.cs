using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// A compiler diagnostic mapped back to recipe semantics.
/// </summary>
public sealed record VisualCompileDiagnostic(
    VisualCompileDiagnosticSeverity Severity,
    string Code,
    string Message,
    NodeId? NodeId = null,
    ConnectionId? ConnectionId = null,
    string? SourcePath = null);
