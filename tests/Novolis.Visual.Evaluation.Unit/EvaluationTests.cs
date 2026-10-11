using System.Numerics;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;

namespace Novolis.Visual.Evaluation.Unit;

public sealed class EvaluationTests
{
    [Test]
    public async Task Evaluates_scalar_color_and_vector_constants()
    {
        var scalar = ScalarContract();
        var color = new VisualTypeContract(
            VisualValueKind.Color,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.LinearSrgb);
        var vector = new VisualTypeContract(
            VisualValueKind.Vector,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
        var scalarNode = NodeId.Parse("20000000000000000000000000000001");
        var colorNode = NodeId.Parse("20000000000000000000000000000002");
        var vectorNode = NodeId.Parse("20000000000000000000000000000003");
        var recipe = new VisualRecipe(
            "constants",
            [
                VisualNode.Create(
                    scalarNode,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ScalarValue(4))]),
                VisualNode.Create(
                    colorNode,
                    VisualBuiltInNodeTypes.ConstantColor,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ColorValue(1, 0.5, 0.25))]),
                VisualNode.Create(
                    vectorNode,
                    VisualCompileBuiltInNodeTypes.ConstantVector,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        VectorValue.Create3(1, 2, 3))]),
            ],
            outputs:
            [
                new VisualOutput(
                    "scalar",
                    "Scalar",
                    new VisualPortReference(scalarNode, new PortId("value")),
                    scalar),
                new VisualOutput(
                    "color",
                    "Color",
                    new VisualPortReference(colorNode, new PortId("value")),
                    color),
                new VisualOutput(
                    "vector",
                    "Vector",
                    new VisualPortReference(vectorNode, new PortId("value")),
                    vector),
            ]);

        var result = VisualEvaluator.Evaluate(VisualCompiler.Compile(recipe));

        await Assert.That(result.Success).IsTrue();
        await Assert.That(((ScalarValue)result.Outputs["scalar"]).Value)
            .IsEqualTo(4);
        await Assert.That(((ColorValue)result.Outputs["color"]).Green)
            .IsEqualTo(0.5);
        await Assert.That(((VectorValue)result.Outputs["vector"]).Components[2])
            .IsEqualTo(3);
        await Assert.That(result.SourceTrace.Length).IsEqualTo(3);
    }

    [Test]
    public async Task Evaluates_noise_and_multiply_with_a_stable_trace()
    {
        var scalar = ScalarContract();
        var noise = NodeId.Parse("30000000000000000000000000000001");
        var amount = NodeId.Parse("30000000000000000000000000000002");
        var multiply = NodeId.Parse("30000000000000000000000000000003");
        var recipe = new VisualRecipe(
            "noise",
            [
                VisualNode.Create(noise, VisualCompileBuiltInNodeTypes.NoiseScalar, 1),
                VisualNode.Create(
                    amount,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ScalarValue(2))]),
                VisualNode.Create(
                    multiply,
                    VisualCompileBuiltInNodeTypes.MultiplyScalar,
                    1),
            ],
            [
                new VisualConnection(
                    ConnectionId.Parse("30000000000000000000000000000001"),
                    new VisualPortReference(noise, new PortId("result")),
                    new VisualPortReference(multiply, new PortId("left")),
                    scalar,
                    scalar),
                new VisualConnection(
                    ConnectionId.Parse("30000000000000000000000000000002"),
                    new VisualPortReference(amount, new PortId("value")),
                    new VisualPortReference(multiply, new PortId("right")),
                    scalar,
                    scalar),
            ],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(multiply, new PortId("result")),
                    scalar),
            ]);

        var ir = VisualCompiler.Compile(recipe);
        var context = new VisualEvaluationContext
        {
            Position = new Vector3(1, 2, 3),
            Seed = 9,
        };
        var first = VisualEvaluator.Evaluate(ir, context);
        var second = VisualEvaluator.Evaluate(ir, context);

        var firstValue = ((ScalarValue)first.Outputs["result"]).Value;
        var secondValue = ((ScalarValue)second.Outputs["result"]).Value;
        await Assert.That(first.Success).IsTrue();
        await Assert.That(firstValue).IsEqualTo(secondValue);
        await Assert.That(firstValue).IsGreaterThanOrEqualTo(0);
        await Assert.That(firstValue).IsLessThanOrEqualTo(2);
        await Assert.That(first.SourceTrace.Any(trace =>
            trace.Operation == "noise.scalar")).IsTrue();
        await Assert.That(first.SourceTrace.Any(trace =>
            trace.Operation == "multiply.scalar")).IsTrue();
    }

    private static VisualTypeContract ScalarContract() =>
        new(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
}
