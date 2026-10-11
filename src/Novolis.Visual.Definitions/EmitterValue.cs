using System.Numerics;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Deterministic renderer-neutral particle emitter definition.
/// </summary>
public sealed record EmitterValue : VisualValue
{
    public EmitterValue(
        string emitterId,
        uint seed,
        double rate,
        double lifetimeSeconds,
        Vector3 initialVelocity,
        ColorValue color,
        EmitterRenderMode renderMode = EmitterRenderMode.Billboard,
        string? spriteAssetId = null)
    {
        EmitterId = VisualValueValidation.Required(emitterId, nameof(emitterId));
        if (!double.IsFinite(rate) || rate < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rate));
        }

        if (!double.IsFinite(lifetimeSeconds) || lifetimeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));
        }

        ArgumentNullException.ThrowIfNull(color);
        if (spriteAssetId is not null)
        {
            VisualValueValidation.Required(spriteAssetId, nameof(spriteAssetId));
        }

        Seed = seed;
        Rate = rate;
        LifetimeSeconds = lifetimeSeconds;
        InitialVelocity = initialVelocity;
        Color = color;
        RenderMode = renderMode;
        SpriteAssetId = spriteAssetId;
    }

    public string EmitterId { get; }

    public uint Seed { get; }

    public double Rate { get; }

    public double LifetimeSeconds { get; }

    public Vector3 InitialVelocity { get; }

    public ColorValue Color { get; }

    public EmitterRenderMode RenderMode { get; }

    public string? SpriteAssetId { get; }

    public override VisualValueKind Kind => VisualValueKind.Emitter;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.World,
        ColorSpace.None,
        FieldFrequency.Frame,
        FieldPurity.ContextDependent);
}
