#if false
using System.Collections.Immutable;

namespace Novolis.Visual.Compile;

/// <summary>
/// Complete compiler result shared by previews, exports, and diagnostics.
/// </summary>
public sealed record VisualCompilationResult
{
    public required VisualIrRecipe? Ir { get; init; }

    public required ImmutableArray<VisualCompileDiagnostic> Diagnostics { get; init; }

    public required VisualCapabilityReport Capabilities { get; init; }

    public bool IsValid =>
        Ir is not null &&
        Diagnostics.All(diagnostic =>
            diagnostic.Severity != VisualCompileDiagnosticSeverity.Error) &&
        !Capabilities.HasUnsupportedRequired;
}
#endif
