using System.Numerics;
using Novolis.Rendering.FrameGraph;
using Novolis.Visual.Adapters.Rendering;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering.Unit;

public sealed class VisualFeatureAdapterTests
{
    [Test]
    public async Task Scene_features_keep_spot_approximation_explicit()
    {
        var result = VisualRenderingAdapter.LowerSceneFeatures(
        [
            new LightValue(
                LightKind.Spot,
                new ColorValue(1, 0.8, 0.6),
                4,
                VisualUnit.Unitless,
                TransformValue.Identity(CoordinateSpace.World)),
            new EmitterValue(
                "sparks",
                7,
                2,
                1,
                Vector3.UnitY,
                new ColorValue(1, 0.5, 0.1)),
        ]);

        await Assert.That(result.Status)
            .IsEqualTo(VisualCapabilityStatus.Approximate);
        await Assert.That(result.Lights.Length).IsEqualTo(1);
        await Assert.That(result.Emitters.Length).IsEqualTo(1);
        await Assert.That(result.Diagnostics.Select(diagnostic => diagnostic.Code))
            .Contains("VRA503");
    }

    [Test]
    public async Task Frame_effects_lower_to_the_renderer_owned_frame_graph()
    {
        var result = VisualRenderingAdapter.BuildFrameGraph(
        [
            new FrameEffectValue(FrameEffectKind.Bloom),
            new FrameEffectValue(FrameEffectKind.ToneMapping),
        ],
        new FrameGraphSize(32, 32));

        await Assert.That(result.IsUsable).IsTrue();
        await Assert.That(result.Description.Passes.Select(pass => pass.Name))
            .Contains("BloomExtract");
        await Assert.That(result.Description.Passes.Select(pass => pass.Name))
            .Contains("ToneMapping");
        await Assert.That(result.Description.Passes.Any(pass =>
            pass.Name.Contains("Workflow", StringComparison.OrdinalIgnoreCase)))
            .IsFalse();
    }
}
