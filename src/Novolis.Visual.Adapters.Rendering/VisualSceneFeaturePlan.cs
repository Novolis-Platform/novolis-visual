using System.Collections.Immutable;
using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Lowered scene-adjacent Visual values. Renderer capability gaps remain
/// explicit instead of being replaced by an unrelated material.
/// </summary>
public sealed class VisualSceneFeaturePlan
{
    internal VisualSceneFeaturePlan(
        IEnumerable<LightDefinition> lights,
        IEnumerable<VolumeValue> volumes,
        IEnumerable<EmitterValue> emitters,
        IEnumerable<DecalValue> decals,
        IEnumerable<EffectValue> effects,
        IEnumerable<VisualRenderingDiagnostic> diagnostics,
        VisualCapabilityStatus status,
        string fingerprint)
    {
        Lights = lights.ToImmutableArray();
        Volumes = volumes.ToImmutableArray();
        Emitters = emitters.ToImmutableArray();
        Decals = decals.ToImmutableArray();
        Effects = effects.ToImmutableArray();
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Status = status;
        Fingerprint = fingerprint;
    }

    public ImmutableArray<LightDefinition> Lights { get; }

    public ImmutableArray<VolumeValue> Volumes { get; }

    public ImmutableArray<EmitterValue> Emitters { get; }

    public ImmutableArray<DecalValue> Decals { get; }

    public ImmutableArray<EffectValue> Effects { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityStatus Status { get; }

    public string Fingerprint { get; }

    public bool IsUsable =>
        Status != VisualCapabilityStatus.Unsupported &&
        !Diagnostics.Any(diagnostic =>
            diagnostic.Severity == VisualCompileDiagnosticSeverity.Error);
}
