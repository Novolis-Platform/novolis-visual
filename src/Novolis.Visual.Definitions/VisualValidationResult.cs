using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Immutable result of validating a recipe against an allowlisted registry.
/// </summary>
public sealed record VisualValidationResult
{
    public VisualValidationResult(IEnumerable<VisualDiagnostic>? diagnostics = null)
    {
        Diagnostics = (diagnostics ?? [])
            .Where(diagnostic => diagnostic is not null)
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId?.ToString(), StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.ConnectionId?.ToString(), StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public ImmutableArray<VisualDiagnostic> Diagnostics { get; }

    public bool IsValid =>
        Diagnostics.All(diagnostic => diagnostic.Severity != VisualDiagnosticSeverity.Error);
}
