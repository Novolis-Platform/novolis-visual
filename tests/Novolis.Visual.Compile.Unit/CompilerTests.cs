using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile.Unit;

public sealed class CompilerTests
{
    [Test]
    public async Task Compile_is_stable_and_removes_dead_nodes()
    {
        var recipe = CreateAddRecipe(includeDeadNode: true);
        var reversed = CreateAddRecipe(includeDeadNode: true, reverseNodes: true);

        var first = VisualCompiler.Compile(recipe);
        var second = VisualCompiler.Compile(reversed);

        await Assert.That(first.Success).IsTrue();
        await Assert.That(first.SemanticFingerprint)
            .IsEqualTo(second.SemanticFingerprint);
        await Assert.That(first.Nodes.Any(node =>
            node.Id == NodeId.Parse("00000000000000000000000000000004")))
            .IsFalse();
        await Assert.That(first.Nodes.Count).IsEqualTo(3);
        await Assert.That(first.Nodes.Last().IsConstant).IsTrue();
        await Assert.That(first.SourceMap.TryGetOutput("result", out _)).IsTrue();
    }

    [Test]
    public async Task Compile_reports_cycles()
    {
        var scalar = ScalarContract();
        var left = NodeId.Parse("10000000000000000000000000000001");
        var right = NodeId.Parse("10000000000000000000000000000002");
        var recipe = new VisualRecipe(
            "cycle",
            [
                VisualNode.Create(left, VisualBuiltInNodeTypes.AddScalar, 1),
                VisualNode.Create(right, VisualBuiltInNodeTypes.AddScalar, 1),
            ],
            [
                new VisualConnection(
                    ConnectionId.Parse("10000000000000000000000000000001"),
                    new VisualPortReference(left, new PortId("result")),
                    new VisualPortReference(right, new PortId("left")),
                    scalar,
                    scalar),
                new VisualConnection(
                    ConnectionId.Parse("10000000000000000000000000000002"),
                    new VisualPortReference(right, new PortId("result")),
                    new VisualPortReference(left, new PortId("left")),
                    scalar,
                    scalar),
            ],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(right, new PortId("result")),
                    scalar),
            ]);

        var compiled = VisualCompiler.Compile(recipe);

        await Assert.That(compiled.Diagnostics.Any(diagnostic =>
            diagnostic.Code == "VIS120")).IsTrue();
        await Assert.That(compiled.Success).IsFalse();
    }

    [Test]
    public async Task Capability_status_is_preserved()
    {
        var recipe = new VisualRecipe(
            "capabilities",
            capabilities:
            [
                new VisualCapabilityRequirement("visual.noise", required: true),
            ]);
        var options = new VisualCompileOptions(
            targetCapabilities: new VisualTargetCapabilities(
            [
                new VisualCapabilitySupport(
                    "visual.noise",
                    VisualCapabilityStatus.Approximate),
            ]));

        var compiled = VisualCompiler.Compile(recipe, options);

        await Assert.That(compiled.Capabilities.GetStatus("visual.noise"))
            .IsEqualTo(VisualCapabilityStatus.Approximate);
        await Assert.That(compiled.Diagnostics.Any(diagnostic =>
            diagnostic.Code == "VIS301")).IsTrue();
    }

    [Test]
    public async Task Incremental_analysis_follows_downstream_outputs()
    {
        var previous = CreateAddRecipe();
        var current = CreateAddRecipe(leftValue: 7);

        var analysis = VisualCompiler.AnalyzeIncremental(previous, current);

        await Assert.That(analysis.ChangedNodes.Length).IsEqualTo(1);
        await Assert.That(analysis.AffectedNodes.Length).IsEqualTo(2);
        await Assert.That(analysis.AffectedOutputIds).Contains("result");
    }

    private static VisualRecipe CreateAddRecipe(
        bool includeDeadNode = false,
        bool reverseNodes = false,
        double leftValue = 2)
    {
        var left = NodeId.Parse("00000000000000000000000000000001");
        var right = NodeId.Parse("00000000000000000000000000000002");
        var add = NodeId.Parse("00000000000000000000000000000003");
        var dead = NodeId.Parse("00000000000000000000000000000004");
        var nodes = new List<VisualNode>
        {
            VisualNode.Create(
                left,
                VisualBuiltInNodeTypes.ConstantScalar,
                1,
                [new KeyValuePair<string, VisualValue>(
                    "value",
                    new ScalarValue(leftValue))]),
            VisualNode.Create(
                right,
                VisualBuiltInNodeTypes.ConstantScalar,
                1,
                [new KeyValuePair<string, VisualValue>(
                    "value",
                    new ScalarValue(3))]),
            VisualNode.Create(add, VisualBuiltInNodeTypes.AddScalar, 1),
        };
        if (includeDeadNode)
        {
            nodes.Add(
                VisualNode.Create(
                    dead,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ScalarValue(99))]));
        }

        if (reverseNodes)
        {
            nodes.Reverse();
        }

        var scalar = ScalarContract();
        return new VisualRecipe(
            "add",
            nodes,
            [
                new VisualConnection(
                    ConnectionId.Parse("00000000000000000000000000000001"),
                    new VisualPortReference(left, new PortId("value")),
                    new VisualPortReference(add, new PortId("left")),
                    scalar,
                    scalar),
                new VisualConnection(
                    ConnectionId.Parse("00000000000000000000000000000002"),
                    new VisualPortReference(right, new PortId("value")),
                    new VisualPortReference(add, new PortId("right")),
                    scalar,
                    scalar),
            ],
            outputs:
            [
                new VisualOutput(
                    "result",
                    "Result",
                    new VisualPortReference(add, new PortId("result")),
                    scalar),
            ]);
    }

    private static VisualTypeContract ScalarContract() =>
        new(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
}
