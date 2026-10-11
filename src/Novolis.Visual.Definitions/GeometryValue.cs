namespace Novolis.Visual.Definitions;

/// <summary>
/// A renderer-neutral geometry asset reference.
/// </summary>
public sealed record GeometryValue : VisualValue
{
    public GeometryValue(
        string assetId,
        GeometryKind geometryKind,
        CoordinateSpace coordinateSpace,
        VisualUnit unit = VisualUnit.Meters)
    {
        AssetId = VisualValueValidation.Required(assetId, nameof(assetId));
        GeometryKind = geometryKind;
        CoordinateSpace = coordinateSpace;
        Unit = unit;
    }

    public string AssetId { get; }

    public GeometryKind GeometryKind { get; }

    public CoordinateSpace CoordinateSpace { get; }

    public VisualUnit Unit { get; }

    public override VisualValueKind Kind => VisualValueKind.Geometry;

    public override VisualTypeContract Contract => new(
        Kind,
        Unit,
        CoordinateSpace,
        ColorSpace.None);
}
