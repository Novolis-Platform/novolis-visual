namespace Novolis.Visual.Assets;

/// <summary>
/// Binds one geometry vector component to an exposed asset parameter.
/// </summary>
public sealed record AssetGeometryParameterBinding
{
    public AssetGeometryParameterBinding(
        int component,
        string parameterId,
        double multiplier = 1,
        double offset = 0)
    {
        if (component is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(component));
        }

        if (string.IsNullOrWhiteSpace(parameterId))
        {
            throw new ArgumentException(
                "A parameter identifier is required.",
                nameof(parameterId));
        }

        if (!double.IsFinite(multiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        if (!double.IsFinite(offset))
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        Component = component;
        ParameterId = parameterId.Trim();
        Multiplier = multiplier;
        Offset = offset;
    }

    public int Component { get; }

    public string ParameterId { get; }

    public double Multiplier { get; }

    public double Offset { get; }
}
