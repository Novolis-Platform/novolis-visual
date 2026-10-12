namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Stable identifiers for adapter fingerprints and cache keys.
/// </summary>
public static class RenderingAdapterContract
{
    /// <summary>
    /// Version of the Visual-to-Modeling/Rendering adapter contract.
    /// </summary>
    public const string Version = "visual-rendering-adapter-v1";

    /// <summary>
    /// Capability for an indexed evaluated mesh becoming a render mesh instance.
    /// </summary>
    public const string MeshInstanceCapability = "rendering.mesh-instance.v1";

    /// <summary>
    /// Capability for a fixed standard PBR material.
    /// </summary>
    public const string StandardSurfaceCapability = "rendering.surface.standard-pbr.v1";

    /// <summary>
    /// Capability for a constant surface appearance stack.
    /// </summary>
    public const string ConstantAppearanceCapability =
        "rendering.appearance.constant-surface.v1";

    /// <summary>
    /// Capability for a channelized material lowered to an IMaterial.
    /// </summary>
    public const string MaterialAuthoringCapability =
        "rendering.material.authoring.v2";

    /// <summary>
    /// Capability for transmission/refraction material lowering.
    /// </summary>
    public const string MaterialTransmissionCapability =
        "rendering.material.transmission.v1";

    /// <summary>
    /// Capability for emission material lowering.
    /// </summary>
    public const string MaterialEmissionCapability =
        "rendering.material.emission.v1";

    /// <summary>
    /// Capability for a procedural appearance stack that needs a bake.
    /// </summary>
    public const string AppearanceBakeCapability = "rendering.appearance.bake.v1";
}
