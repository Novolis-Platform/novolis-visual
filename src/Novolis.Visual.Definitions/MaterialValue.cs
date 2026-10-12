namespace Novolis.Visual.Definitions;

/// <summary>
/// A renderer-neutral constant material snapshot.
/// </summary>
public sealed record MaterialValue : VisualValue
{
    public MaterialValue(
        string materialId,
        VisualMaterialModel model,
        ColorValue baseColor,
        double roughness = 0.5,
        double metallic = 0,
        ColorValue? emissionColor = null,
        double emissionStrength = 0,
        double transmission = 0,
        double ior = 1.5,
        double absorption = 0,
        double clearcoat = 0,
        double anisotropy = 0,
        double alpha = 1,
        bool thinWall = false)
    {
        MaterialId = VisualValueValidation.Required(materialId, nameof(materialId));
        ArgumentNullException.ThrowIfNull(baseColor);
        emissionColor ??= new ColorValue(0, 0, 0);

        ValidateRange(roughness, nameof(roughness));
        ValidateRange(metallic, nameof(metallic));
        ValidateNonNegative(emissionStrength, nameof(emissionStrength));
        ValidateRange(transmission, nameof(transmission));
        if (!double.IsFinite(ior) || ior < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ior));
        }

        ValidateNonNegative(absorption, nameof(absorption));
        ValidateRange(clearcoat, nameof(clearcoat));
        if (!double.IsFinite(anisotropy) || anisotropy is < -1 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(anisotropy));
        }

        ValidateRange(alpha, nameof(alpha));

        Model = model;
        BaseColor = baseColor;
        Roughness = roughness;
        Metallic = metallic;
        EmissionColor = emissionColor;
        EmissionStrength = emissionStrength;
        Transmission = transmission;
        Ior = ior;
        Absorption = absorption;
        Clearcoat = clearcoat;
        Anisotropy = anisotropy;
        Alpha = alpha;
        ThinWall = thinWall;
    }

    public string MaterialId { get; }

    public VisualMaterialModel Model { get; }

    public ColorValue BaseColor { get; }

    public double Roughness { get; }

    public double Metallic { get; }

    public ColorValue EmissionColor { get; }

    public double EmissionStrength { get; }

    public double Transmission { get; }

    public double Ior { get; }

    public double Absorption { get; }

    public double Clearcoat { get; }

    public double Anisotropy { get; }

    public double Alpha { get; }

    public bool ThinWall { get; }

    public override VisualValueKind Kind => VisualValueKind.Material;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);

    private static void ValidateRange(double value, string name)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }

    private static void ValidateNonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
