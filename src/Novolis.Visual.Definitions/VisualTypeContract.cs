namespace Novolis.Visual.Definitions;

/// <summary>
/// Complete type metadata for a value flowing through a port.
/// </summary>
public sealed record VisualTypeContract
{
    public VisualTypeContract(
        VisualValueKind valueKind,
        VisualUnit unit,
        CoordinateSpace coordinateSpace,
        ColorSpace colorSpace,
        FieldFrequency frequency = FieldFrequency.Constant,
        FieldPurity purity = FieldPurity.Pure,
        VisualAttributeRequirement attributes = VisualAttributeRequirement.None)
    {
        if ((valueKind is VisualValueKind.Color or VisualValueKind.Image) &&
            colorSpace == ColorSpace.None)
        {
            throw new ArgumentException(
                "Color and image contracts require an explicit color space.",
                nameof(colorSpace));
        }

        ValueKind = valueKind;
        Unit = unit;
        CoordinateSpace = coordinateSpace;
        ColorSpace = colorSpace;
        Frequency = frequency;
        Purity = purity;
        Attributes = attributes;
    }

    public VisualValueKind ValueKind { get; }

    public VisualUnit Unit { get; }

    public CoordinateSpace CoordinateSpace { get; }

    public ColorSpace ColorSpace { get; }

    public FieldFrequency Frequency { get; }

    public FieldPurity Purity { get; }

    public VisualAttributeRequirement Attributes { get; }

    public bool IsValueCompatibleWith(VisualTypeContract other) =>
        other is not null &&
        ValueKind == other.ValueKind &&
        Unit == other.Unit &&
        CoordinateSpace == other.CoordinateSpace &&
        ColorSpace == other.ColorSpace;

    public bool IsCompatibleWith(VisualTypeContract other) =>
        IsValueCompatibleWith(other) &&
        Frequency == other.Frequency &&
        Purity == other.Purity &&
        Attributes == other.Attributes;

    public override string ToString() =>
        $"{ValueKind}/{Unit}/{CoordinateSpace}/{ColorSpace}/{Frequency}/{Purity}/{Attributes}";
}
