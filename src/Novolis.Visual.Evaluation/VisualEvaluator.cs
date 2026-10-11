using System.Numerics;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Evaluation;

/// <summary>
/// Deterministic CPU reference evaluator for the normalized visual IR.
/// </summary>
public static class VisualEvaluator
{
    public static VisualEvaluationResult Evaluate(
        VisualIR ir,
        VisualEvaluationContext? context = null,
        VisualEvaluationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(ir);
        var effectiveContext = context ?? VisualEvaluationContext.Default;
        var effectiveOptions = options ?? VisualEvaluationOptions.Default;
        var diagnostics = ir.Diagnostics.ToList();

        if (!ir.Success && !effectiveOptions.EvaluateWhenCompilationHasErrors)
        {
            return new VisualEvaluationResult(diagnostics: diagnostics);
        }

        var nodes = ir.Nodes.ToDictionary(node => node.Id);
        var values = new Dictionary<NodeId, VisualValue>();
        var visiting = new HashSet<NodeId>();
        var traces = new List<VisualEvaluationTrace>();

        foreach (var node in ir.Nodes)
        {
            EvaluateNode(node.Id);
        }

        var outputs = new List<KeyValuePair<string, VisualValue>>();
        foreach (var output in ir.Outputs)
        {
            if (!nodes.ContainsKey(output.SourceNodeId))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "EVAL010",
                    $"Output '{output.Id}' points to a missing IR node."));
                continue;
            }

            outputs.Add(
                new(
                    output.Id,
                    EvaluateNode(output.SourceNodeId)));
        }

        return new VisualEvaluationResult(
            outputs,
            effectiveOptions.IncludeTrace ? traces : [],
            diagnostics);

        VisualValue EvaluateNode(NodeId nodeId)
        {
            if (values.TryGetValue(nodeId, out var cached))
            {
                return cached;
            }

            if (!nodes.TryGetValue(nodeId, out var node))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "EVAL003",
                    $"IR node '{nodeId}' is missing.",
                    nodeId));
                return new ScalarValue(0);
            }

            if (!visiting.Add(nodeId))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "EVAL002",
                    "The IR contains a cycle and cannot be evaluated.",
                    nodeId));
                return new ScalarValue(0);
            }

            var inputs = new Dictionary<string, VisualValue>(StringComparer.Ordinal);
            foreach (var input in node.Inputs)
            {
                inputs[input.TargetPort.Value] = EvaluateNode(input.SourceNodeId);
            }

            var value = EvaluateOperation(node, inputs, effectiveContext, diagnostics);
            visiting.Remove(nodeId);
            values[nodeId] = value;

            if (effectiveOptions.IncludeTrace)
            {
                var sourcePath = ir.SourceMap.TryGetNode(nodeId, out var entry)
                    ? entry?.Path
                    : null;
                traces.Add(
                    new VisualEvaluationTrace(
                        traces.Count,
                        nodeId,
                        node.Operation,
                        inputs,
                        value,
                        sourcePath));
            }

            return value;
        }
    }

    public static VisualValue EvaluateOutput(
        VisualIR ir,
        string outputId,
        VisualEvaluationContext? context = null,
        VisualEvaluationOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputId);
        var result = Evaluate(ir, context, options);
        if (!result.TryGetOutput(outputId, out var value) || value is null)
        {
            throw new InvalidOperationException(
                $"Evaluation did not produce output '{outputId}'.");
        }

        return value;
    }

    private static VisualValue EvaluateOperation(
        VisualIRNode node,
        IReadOnlyDictionary<string, VisualValue> inputs,
        VisualEvaluationContext context,
        ICollection<VisualDiagnostic> diagnostics)
    {
        if (node.ConstantValue is not null)
        {
            return node.ConstantValue;
        }

        if (node.Operation is "constant.scalar"
            or "constant.color"
            or "constant.vector")
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "EVAL004",
                $"Constant node '{node.Id}' has no value.",
                node.Id));
            return DefaultValue(node.Contract);
        }

        return node.Operation switch
        {
            "add.scalar" => Add(
                ReadInput(inputs, "left", 0),
                ReadInput(inputs, "right", 1),
                node,
                diagnostics),
            "multiply.scalar" => Multiply(
                ReadInput(inputs, "left", 0),
                ReadInput(inputs, "right", 1),
                node,
                diagnostics),
            "noise.scalar" => Noise(context),
            _ => Unknown(node, diagnostics),
        };
    }

    private static VisualValue Add(
        VisualValue left,
        VisualValue right,
        VisualIRNode node,
        ICollection<VisualDiagnostic> diagnostics)
    {
        if (left is ScalarValue leftScalar &&
            right is ScalarValue rightScalar &&
            leftScalar.Unit == rightScalar.Unit)
        {
            return new ScalarValue(
                leftScalar.Value + rightScalar.Value,
                leftScalar.Unit);
        }

        if (left is ColorValue leftColor && right is ColorValue rightColor &&
            leftColor.ColorSpace == rightColor.ColorSpace)
        {
            return new ColorValue(
                leftColor.Red + rightColor.Red,
                leftColor.Green + rightColor.Green,
                leftColor.Blue + rightColor.Blue,
                leftColor.Alpha + rightColor.Alpha,
                leftColor.ColorSpace);
        }

        if (left is VectorValue leftVector &&
            right is VectorValue rightVector &&
            leftVector.Dimension == rightVector.Dimension &&
            leftVector.Unit == rightVector.Unit &&
            leftVector.CoordinateSpace == rightVector.CoordinateSpace)
        {
            return new VectorValue(
                leftVector.Components
                    .Zip(rightVector.Components, (a, b) => a + b),
                leftVector.Unit,
                leftVector.CoordinateSpace);
        }

        diagnostics.Add(new(
            VisualDiagnosticSeverity.Error,
            "EVAL020",
            $"Inputs to '{node.Operation}' do not have compatible value kinds.",
            node.Id));
        return DefaultValue(node.Contract);
    }

    private static VisualValue Multiply(
        VisualValue left,
        VisualValue right,
        VisualIRNode node,
        ICollection<VisualDiagnostic> diagnostics)
    {
        if (left is ScalarValue leftScalar &&
            right is ScalarValue rightScalar &&
            leftScalar.Unit == rightScalar.Unit)
        {
            return new ScalarValue(
                leftScalar.Value * rightScalar.Value,
                leftScalar.Unit);
        }

        if (left is ColorValue color && right is ScalarValue scalar)
        {
            return new ColorValue(
                color.Red * scalar.Value,
                color.Green * scalar.Value,
                color.Blue * scalar.Value,
                color.Alpha,
                color.ColorSpace);
        }

        if (left is ScalarValue scale && right is ColorValue colorRight)
        {
            return new ColorValue(
                colorRight.Red * scale.Value,
                colorRight.Green * scale.Value,
                colorRight.Blue * scale.Value,
                colorRight.Alpha,
                colorRight.ColorSpace);
        }

        if (left is VectorValue vector && right is ScalarValue vectorScale)
        {
            return new VectorValue(
                vector.Components.Select(component => component * vectorScale.Value),
                vector.Unit,
                vector.CoordinateSpace);
        }

        if (left is ScalarValue scalarScale && right is VectorValue rightVector)
        {
            return new VectorValue(
                rightVector.Components.Select(component => component * scalarScale.Value),
                rightVector.Unit,
                rightVector.CoordinateSpace);
        }

        diagnostics.Add(new(
            VisualDiagnosticSeverity.Error,
            "EVAL021",
            $"Inputs to '{node.Operation}' cannot be multiplied.",
            node.Id));
        return DefaultValue(node.Contract);
    }

    private static VisualValue Noise(VisualEvaluationContext context)
    {
        var position = context.Position;
        var n = Math.Sin(
                    position.X * 12.9898 +
                    position.Y * 78.233 +
                    position.Z * 37.719 +
                    context.Seed * 19.193 +
                    context.Time * 0.017)
                * 43758.5453;
        return new ScalarValue(n - Math.Floor(n));
    }

    private static VisualValue Unknown(
        VisualIRNode node,
        ICollection<VisualDiagnostic> diagnostics)
    {
        diagnostics.Add(new(
            VisualDiagnosticSeverity.Error,
            "EVAL001",
            $"Operation '{node.Operation}' is not in the CPU reference allowlist.",
            node.Id));
        return DefaultValue(node.Contract);
    }

    private static VisualValue ReadInput(
        IReadOnlyDictionary<string, VisualValue> inputs,
        string preferredName,
        int index)
    {
        if (inputs.TryGetValue(preferredName, out var preferred))
        {
            return preferred;
        }

        return inputs.Values
            .OrderBy(value => value.Kind)
            .ElementAtOrDefault(index)
            ?? new ScalarValue(0);
    }

    private static VisualValue DefaultValue(VisualTypeContract contract) =>
        contract.ValueKind switch
        {
            VisualValueKind.Color => new ColorValue(
                0,
                0,
                0,
                1,
                contract.ColorSpace == ColorSpace.None
                    ? ColorSpace.LinearSrgb
                    : contract.ColorSpace),
            VisualValueKind.Vector => VectorValue.Create3(
                0,
                0,
                0,
                contract.Unit,
                contract.CoordinateSpace),
            VisualValueKind.Integer => new IntegerValue(0, contract.Unit),
            VisualValueKind.Boolean => new BooleanValue(false),
            _ => new ScalarValue(contract.Unit == VisualUnit.Unitless ? 0 : 0, contract.Unit),
        };
}
