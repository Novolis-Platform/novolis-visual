using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Evaluation;

/// <summary>
/// Deterministic values, diagnostics, and optional source trace for one sample.
/// </summary>
public sealed class VisualEvaluationResult
{
    public VisualEvaluationResult(
        IEnumerable<KeyValuePair<string, VisualValue>>? outputs = null,
        IEnumerable<VisualEvaluationTrace>? trace = null,
        IEnumerable<VisualDiagnostic>? diagnostics = null)
    {
        Outputs = (outputs ?? [])
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToImmutableSortedDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
        Trace = (trace ?? [])
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.NodeId)
            .ToImmutableArray();
        Diagnostics = (diagnostics ?? [])
            .OrderBy(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId?.ToString(), StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public ImmutableSortedDictionary<string, VisualValue> Outputs { get; }

    public ImmutableArray<VisualEvaluationTrace> Trace { get; }

    public ImmutableArray<VisualEvaluationTrace> SourceTrace => Trace;

    public ImmutableArray<VisualDiagnostic> Diagnostics { get; }

    public bool Success =>
        Diagnostics.All(diagnostic =>
            diagnostic.Severity != VisualDiagnosticSeverity.Error);

    public bool TryGetOutput(string id, out VisualValue? value) =>
        Outputs.TryGetValue(id, out value);
}
