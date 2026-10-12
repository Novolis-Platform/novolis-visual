using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// Provides the schema contract for each material channel.
/// </summary>
public static class MaterialChannelContract
{
    public static VisualTypeContract For(MaterialChannel channel) =>
        channel switch
        {
            MaterialChannel.BaseColor or MaterialChannel.EmissionColor =>
                new VisualTypeContract(
                    VisualValueKind.Color,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.LinearSrgb),
            MaterialChannel.Normal =>
                new VisualTypeContract(
                    VisualValueKind.Vector,
                    VisualUnit.Unitless,
                    CoordinateSpace.Object,
                    ColorSpace.None),
            MaterialChannel.Material =>
                new VisualTypeContract(
                    VisualValueKind.Material,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None),
            _ =>
                new VisualTypeContract(
                    VisualValueKind.Scalar,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None),
        };
}
