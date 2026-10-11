using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.CodeGen.Unit;

public sealed class CodeGenTests
{
    [Test]
    public async Task Generated_source_is_deterministic_and_closed()
    {
        var recipe = CreateRecipe();
        var options = new VisualCompileOptions(optimize: false);
        var compilation = VisualCompiler.Compile(recipe, options);
        var generator = new VisualCSharpGenerator();

        var first = generator.Generate(
            compilation,
            new VisualCodeGenerationOptions
            {
                Namespace = "Generated.Tests",
                TypeName = "SurfaceProgram",
                Mode = VisualCodeGenerationMode.Specialized,
            });
        var second = generator.Generate(
            VisualCompiler.Compile(recipe, options),
            new VisualCodeGenerationOptions
            {
                Namespace = "Generated.Tests",
                TypeName = "SurfaceProgram",
                Mode = VisualCodeGenerationMode.Specialized,
            });

        await Assert.That(first.Source).IsEqualTo(second.Source);
        await Assert.That(first.Source).Contains("value0");
        await Assert.That(first.Source).Contains("EvaluateScalar");
        await Assert.That(first.SourceSha256).IsEqualTo(second.SourceSha256);

        var closure = CodeOnlyClosureVerifier.Verify([first]);
        await Assert.That(closure.IsClosed).IsTrue();
        await Assert.That(closure.Violations).IsEmpty();
    }

    [Test]
    public async Task Embedded_mode_emits_a_runtime_independent_payload()
    {
        var compilation = VisualCompiler.Compile(
            CreateRecipe(),
            new VisualCompileOptions(optimize: false));
        var source = new VisualCSharpGenerator().Generate(
            compilation,
            new VisualCodeGenerationOptions
            {
                Mode = VisualCodeGenerationMode.EmbeddedPayload,
            });

        await Assert.That(source.Source).Contains("FingerprintBytes");
        await Assert.That(source.Source).Contains("global::System.Convert");
    }

    private static VisualRecipe CreateRecipe()
    {
        var left = NodeId.Parse("10000000000000000000000000000001");
        var right = NodeId.Parse("10000000000000000000000000000002");
        var add = NodeId.Parse("10000000000000000000000000000003");
        var scalar = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);

        return new VisualRecipe(
            "codegen",
            [
                VisualNode.Create(
                    left,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ScalarValue(2))]),
                VisualNode.Create(
                    right,
                    VisualBuiltInNodeTypes.ConstantScalar,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ScalarValue(3))]),
                VisualNode.Create(add, VisualBuiltInNodeTypes.AddScalar, 1),
            ],
            [
                new VisualConnection(
                    ConnectionId.Parse("10000000000000000000000000000001"),
                    new VisualPortReference(left, new PortId("value")),
                    new VisualPortReference(add, new PortId("left")),
                    scalar,
                    scalar),
                new VisualConnection(
                    ConnectionId.Parse("10000000000000000000000000000002"),
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
}
