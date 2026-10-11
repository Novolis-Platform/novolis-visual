namespace Novolis.Visual.Definitions;

/// <summary>
/// A transform with translation, quaternion rotation (x, y, z, w), and scale.
/// </summary>
public sealed record TransformValue : VisualValue
{
    public TransformValue(
        VectorValue translation,
        VectorValue rotation,
        VectorValue scale,
        CoordinateSpace coordinateSpace = CoordinateSpace.Object)
    {
        ArgumentNullException.ThrowIfNull(translation);
        ArgumentNullException.ThrowIfNull(rotation);
        ArgumentNullException.ThrowIfNull(scale);

        if (translation.Dimension != 3)
        {
            throw new ArgumentException(
                "Transform translation must have three components.",
                nameof(translation));
        }

        if (rotation.Dimension != 4)
        {
            throw new ArgumentException(
                "Transform rotation must be a four-component quaternion.",
                nameof(rotation));
        }

        if (scale.Dimension != 3)
        {
            throw new ArgumentException(
                "Transform scale must have three components.",
                nameof(scale));
        }

        Translation = translation;
        Rotation = rotation;
        Scale = scale;
        CoordinateSpace = coordinateSpace;
    }

    public VectorValue Translation { get; }

    public VectorValue Rotation { get; }

    public VectorValue Scale { get; }

    public CoordinateSpace CoordinateSpace { get; }

    public override VisualValueKind Kind => VisualValueKind.Transform;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace,
        ColorSpace.None);

    public static TransformValue Identity(
        CoordinateSpace coordinateSpace = CoordinateSpace.Object) =>
        new(
            VectorValue.Create3(0, 0, 0),
            VectorValue.Create4(0, 0, 0, 1),
            VectorValue.Create3(1, 1, 1),
            coordinateSpace);
}
