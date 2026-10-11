using Novolis.Modeling;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Controls the host-neutral Visual-to-Rendering lowering boundary.
/// </summary>
public sealed record RenderingAdapterOptions
{
    /// <summary>
    /// Mesh attributes that a downstream appearance consumer expects.
    /// </summary>
    public MeshAttributeFlags RequiredAppearanceAttributes { get; init; } =
        MeshAttributeSet.AppearanceDefaults;

    /// <summary>
    /// Allows procedural appearance to report a bake requirement instead of
    /// being reported as unsupported. No image or texture is produced here.
    /// </summary>
    public bool AllowBakedFallback { get; init; }

    /// <summary>
    /// Capability declarations for the selected rendering target.
    /// </summary>
    public VisualTargetCapabilities TargetCapabilities { get; init; } =
        VisualTargetCapabilities.Open();

    /// <summary>
    /// Default adapter options.
    /// </summary>
    public static RenderingAdapterOptions Default { get; } = new();
}
