using System.Numerics;
using Novolis.Rendering.Scene;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Lowers evaluated scene, light, volume, emitter, decal, and effect values
/// into existing rendering-owned definitions and explicit feature buckets.
/// </summary>
public static class VisualSceneFeatureAdapter
{
    public static VisualSceneFeaturePlan Lower(
        IEnumerable<VisualValue> values,
        string? sourcePath = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var lights = new List<LightDefinition>();
        var volumes = new List<VolumeValue>();
        var emitters = new List<EmitterValue>();
        var decals = new List<DecalValue>();
        var effects = new List<EffectValue>();
        var diagnostics = new List<VisualRenderingDiagnostic>();

        foreach (var value in values)
        {
            ArgumentNullException.ThrowIfNull(value);
            switch (value)
            {
                case LightValue light:
                    lights.Add(ToLight(light));
                    if (light.LightKind is
                        Novolis.Visual.Definitions.LightKind.Spot or
                        Novolis.Visual.Definitions.LightKind.Area)
                    {
                        diagnostics.Add(
                            new VisualRenderingDiagnostic(
                                VisualCompileDiagnosticSeverity.Warning,
                                "VRA503",
                                $"Light kind '{light.LightKind}' is approximated as a point light by the current scene contract.",
                                sourcePath,
                                "rendering.light.spot-area"));
                    }

                    break;
                case VolumeValue volume:
                    volumes.Add(volume);
                    break;
                case EmitterValue emitter:
                    emitters.Add(emitter);
                    break;
                case DecalValue decal:
                    decals.Add(decal);
                    break;
                case EffectValue effect:
                    effects.Add(effect);
                    break;
                case FrameEffectValue frameEffect:
                    diagnostics.Add(
                        new VisualRenderingDiagnostic(
                            VisualCompileDiagnosticSeverity.Info,
                            "VRA501",
                            $"Frame effect '{frameEffect.EffectKind}' is lowered by FrameGraph.",
                            sourcePath,
                            $"frame-effect.{frameEffect.EffectKind}"));
                    break;
                case SceneValue:
                    break;
                default:
                    diagnostics.Add(
                        new VisualRenderingDiagnostic(
                            VisualCompileDiagnosticSeverity.Warning,
                            "VRA502",
                            $"Visual value '{value.Kind}' is not a scene feature.",
                            sourcePath,
                            "visual.scene-feature"));
                    break;
            }
        }

        var status = diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Error)
            ? VisualCapabilityStatus.Unsupported
            : diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Warning)
                ? VisualCapabilityStatus.Approximate
                : VisualCapabilityStatus.Exact;
        var fingerprint = RenderingAdapterFingerprint.CreateCacheKey(
            "visual-scene-features",
            string.Join(
                "|",
                lights.Select(light => light.ToString())
                    .Concat(volumes.Select(volume => volume.ToString()))
                    .Concat(emitters.Select(emitter => emitter.ToString()))
                    .Concat(decals.Select(decal => decal.ToString()))
                    .Concat(effects.Select(effect => effect.ToString()))));
        return new VisualSceneFeaturePlan(
            lights,
            volumes,
            emitters,
            decals,
            effects,
            diagnostics,
            status,
            fingerprint);
    }

    private static LightDefinition ToLight(LightValue value) =>
        new(
            value.LightKind == Novolis.Visual.Definitions.LightKind.Directional
                ? Novolis.Rendering.Scene.LightKind.Directional
                : Novolis.Rendering.Scene.LightKind.Point,
            new Vector3(
                (float)value.Transform.Translation.Components[0],
                (float)value.Transform.Translation.Components[1],
                (float)value.Transform.Translation.Components[2]),
            new Vector3(
                (float)value.Color.Red,
                (float)value.Color.Green,
                (float)value.Color.Blue),
            (float)value.Intensity);
}
