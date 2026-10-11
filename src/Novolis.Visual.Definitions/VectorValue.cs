using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// An immutable two-, three-, or four-component vector.
/// </summary>
public sealed record VectorValue : VisualValue
{
    public VectorValue(
        IEnumerable<double> components,
        VisualUnit unit = VisualUnit.Unitless,
        CoordinateSpace coordinateSpace = CoordinateSpace.None)
    {
        ArgumentNullException.ThrowIfNull(components);

        var values = components
            .Select((value, index) =>
                VisualValueValidation.Finite(value, $"components[{index}]"))
            .ToImmutableArray();

        if (values.Length is < 2 or > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(components),
                "A vector must contain two, three, or four components.");
        }

        Components = values;
        Unit = unit;
        CoordinateSpace = coordinateSpace;
    }

    public ImmutableArray<double> Components { get; }

    public VisualUnit Unit { get; }

    public CoordinateSpace CoordinateSpace { get; }

    public int Dimension => Components.Length;

    public override VisualValueKind Kind => VisualValueKind.Vector;

    public override VisualTypeContract Contract => new(
        Kind,
        Unit,
        CoordinateSpace,
        ColorSpace.None);

    public static VectorValue Create2(
        double x,
        double y,
        VisualUnit unit = VisualUnit.Unitless,
        CoordinateSpace coordinateSpace = CoordinateSpace.None) =>
        new([x, y], unit, coordinateSpace);

    public static VectorValue Create3(
        double x,
        double y,
        double z,
        VisualUnit unit = VisualUnit.Unitless,
        CoordinateSpace coordinateSpace = CoordinateSpace.None) =>
        new([x, y, z], unit, coordinateSpace);

    public static VectorValue Create4(
        double x,
        double y,
        double z,
        double w,
        VisualUnit unit = VisualUnit.Unitless,
        CoordinateSpace coordinateSpace = CoordinateSpace.None) =>
        new([x, y, z, w], unit, coordinateSpace);
}
