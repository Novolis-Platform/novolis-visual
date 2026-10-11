using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// A stable, host-neutral node in normalized visual IR.
/// </summary>
public sealed record VisualIRNode
{
    public VisualIRNode(
        NodeId id,
        string typeId,
        string operation,
        VisualTypeContract contract,
        IEnumerable<VisualIRInput>? inputs = null,
        VisualValue? constantValue = null)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("An IR node identifier is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(typeId))
        {
            throw new ArgumentException("An IR node type is required.", nameof(typeId));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("An IR node operation is required.", nameof(operation));
        }

        ArgumentNullException.ThrowIfNull(contract);
        if (constantValue is not null &&
            !contract.IsValueCompatibleWith(constantValue.Contract))
        {
            throw new ArgumentException(
                "The constant value does not satisfy the IR node contract.",
                nameof(constantValue));
        }

        Id = id;
        TypeId = typeId;
        Operation = operation;
        Contract = contract;
        Inputs = (inputs ?? [])
            .OrderBy(input => input.TargetPort.Value, StringComparer.Ordinal)
            .ThenBy(input => input.SourceNodeId)
            .ThenBy(input => input.SourcePort.Value, StringComparer.Ordinal)
            .ToImmutableArray();
        ConstantValue = constantValue;
    }

    public NodeId Id { get; }

    public string TypeId { get; }

    public string Operation { get; }

    public VisualTypeContract Contract { get; }

    public VisualTypeContract OutputContract => Contract;

    public ImmutableArray<VisualIRInput> Inputs { get; }

    public VisualValue? ConstantValue { get; }

    public bool IsConstant => ConstantValue is not null;

    public bool TryGetInput(PortId targetPort, out VisualIRInput? input)
    {
        input = Inputs.FirstOrDefault(candidate =>
            candidate.TargetPort == targetPort);
        return input is not null;
    }
}
