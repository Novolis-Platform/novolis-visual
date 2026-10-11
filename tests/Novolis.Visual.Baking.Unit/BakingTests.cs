using Novolis.Visual.Baking;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Baking.Unit;

public sealed class BakingTests
{
    [Test]
    public async Task Color_outputs_are_evaluated_into_deterministic_pixels()
    {
        var color = NodeId.Parse("20000000000000000000000000000001");
        var contract = new VisualTypeContract(
            VisualValueKind.Color,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.LinearSrgb);
        var recipe = new VisualRecipe(
            "bake",
            [
                VisualNode.Create(
                    color,
                    VisualBuiltInNodeTypes.ConstantColor,
                    1,
                    [new KeyValuePair<string, VisualValue>(
                        "value",
                        new ColorValue(1, 0.5, 0.25, 1))]),
            ],
            outputs:
            [
                new VisualOutput(
                    "color",
                    "Color",
                    new VisualPortReference(color, new PortId("value")),
                    contract),
            ]);
        var compilation = VisualCompiler.Compile(recipe);
        var baker = new VisualBaker();

        var first = baker.Bake(
            compilation,
            new VisualBakeOptions { Width = 2, Height = 2 });
        var second = baker.Bake(
            compilation,
            new VisualBakeOptions { Width = 2, Height = 2 });

        await Assert.That(first.Manifest.ContentSha256)
            .IsEqualTo(second.Manifest.ContentSha256);
        await Assert.That(first.Image.Pixels[0])
            .IsEqualTo(new VisualRgba32(255, 128, 64, 255));
        await Assert.That(first.PngBytes).IsNotEmpty();
    }
}
