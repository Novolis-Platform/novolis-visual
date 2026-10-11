using System.Collections.Immutable;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Evaluation;

/// <summary>
/// An inspectable result for one evaluated IR node.
/// </summary>
public sealed record VisualEvaluationTrace
{
    public VisualEvaluationTrace(
        int sequence,
        NodeId nodeId,
        string operation,
        IEnumerable<KeyValuePair<string, VisualValue>>? inputs,
        VisualValue? output,
        string? sourcePath = null)
    {
        Sequence = sequence;
        NodeId = nodeId;
        Operation = operation;
        Inputs = (inputs ?? [])
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToImmutableSortedDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal);
        Output = output;
        SourcePath = sourcePath;
    }

    public int Sequence { get; }

    public NodeId NodeId { get; }

    public string Operation { get; }

    public ImmutableSortedDictionary<string, VisualValue> Inputs { get; }

    public VisualValue? Output { get; }

    public VisualValue? Value => Output;

    public string? SourcePath { get; }
}
