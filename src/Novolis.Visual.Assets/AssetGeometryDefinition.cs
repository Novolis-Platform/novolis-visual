using System.Collections.Immutable;

namespace Novolis.Visual.Assets;

/// <summary>
/// Serializable geometry metadata for one asset component. It is deliberately
/// independent of Modeling and Rendering so recipes remain portable.
/// </summary>
public sealed record AssetGeometryDefinition
{
    public AssetGeometryDefinition(
        string geometryId,
        AssetGeometryPrimitive primitive,
        IEnumerable<double>? size = null,
        IEnumerable<double>? position = null,
        IEnumerable<double>? rotationDegrees = null,
        int segments = 24,
        IEnumerable<AssetGeometryParameterBinding>? sizeBindings = null,
        IEnumerable<AssetGeometryParameterBinding>? positionBindings = null,
        AssetGeometryRepeat? repeat = null,
        string? textParameterId = null,
        string? textSpacingParameterId = null)
    {
        if (string.IsNullOrWhiteSpace(geometryId))
        {
            throw new ArgumentException(
                "A geometry identifier is required.",
                nameof(geometryId));
        }

        if (segments < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(segments));
        }

        GeometryId = geometryId.Trim();
        Primitive = primitive;
        Size = Vector(size, nameof(size), [1, 1, 1]);
        Position = Vector(position, nameof(position), [0, 0, 0]);
        RotationDegrees = Vector(
            rotationDegrees,
            nameof(rotationDegrees),
            [0, 0, 0]);
        Segments = segments;
        SizeBindings = Bindings(sizeBindings);
        PositionBindings = Bindings(positionBindings);
        Repeat = repeat;
        TextParameterId = NormalizeOptional(textParameterId);
        TextSpacingParameterId = NormalizeOptional(textSpacingParameterId);

        if (Primitive == AssetGeometryPrimitive.Text &&
            TextParameterId is null)
        {
            throw new ArgumentException(
                "Text geometry requires a text parameter identifier.",
                nameof(textParameterId));
        }
    }

    public string GeometryId { get; }

    public AssetGeometryPrimitive Primitive { get; }

    public ImmutableArray<double> Size { get; }

    public ImmutableArray<double> Position { get; }

    public ImmutableArray<double> RotationDegrees { get; }

    public int Segments { get; }

    public ImmutableArray<AssetGeometryParameterBinding> SizeBindings { get; }

    public ImmutableArray<AssetGeometryParameterBinding> PositionBindings { get; }

    public AssetGeometryRepeat? Repeat { get; }

    public string? TextParameterId { get; }

    public string? TextSpacingParameterId { get; }

    private static ImmutableArray<double> Vector(
        IEnumerable<double>? values,
        string name,
        double[] fallback)
    {
        var vector = (values ?? fallback).ToImmutableArray();
        if (vector.Length != 3 ||
            vector.Any(value => !double.IsFinite(value)))
        {
            throw new ArgumentException(
                "A geometry vector must contain three finite values.",
                name);
        }

        return vector;
    }

    private static ImmutableArray<AssetGeometryParameterBinding> Bindings(
        IEnumerable<AssetGeometryParameterBinding>? bindings) =>
        (bindings ?? [])
            .OrderBy(binding => binding.Component)
            .ThenBy(binding => binding.ParameterId, StringComparer.Ordinal)
            .ToImmutableArray();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
