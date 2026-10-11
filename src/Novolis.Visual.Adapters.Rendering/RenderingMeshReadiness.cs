using Novolis.Modeling;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// The independently measured geometry and appearance readiness of one
/// evaluated mesh.
/// </summary>
public sealed record RenderingMeshReadiness
{
    internal RenderingMeshReadiness(
        MeshAttributeSet availableAttributes,
        MeshReadiness modelingReadiness,
        MeshAttributeFlags requiredAppearanceAttributes)
    {
        AvailableAttributes = availableAttributes;
        ModelingReadiness = modelingReadiness;
        RequiredAppearanceAttributes = requiredAppearanceAttributes;
    }

    /// <summary>
    /// Attributes found by inspecting the evaluated mesh data.
    /// </summary>
    public MeshAttributeSet AvailableAttributes { get; }

    /// <summary>
    /// Readiness calculated from the actual mesh contract, not from a caller
    /// supplied claim.
    /// </summary>
    public MeshReadiness ModelingReadiness { get; }

    /// <summary>
    /// Appearance attributes requested by the adapter caller.
    /// </summary>
    public MeshAttributeFlags RequiredAppearanceAttributes { get; }

    public MeshReadinessState Geometry => ModelingReadiness.Geometry;

    public MeshReadinessState Appearance => ModelingReadiness.Appearance;

    public MeshAttributeFlags MissingGeometryAttributes =>
        ModelingReadiness.MissingGeometryAttributes;

    public MeshAttributeFlags MissingAppearanceAttributes =>
        ModelingReadiness.MissingAppearanceAttributes;

    public bool IsGeometryReady => ModelingReadiness.IsGeometryReady;

    public bool IsAppearanceReady => ModelingReadiness.IsAppearanceReady;

    public bool IsReady => IsGeometryReady && IsAppearanceReady;
}
