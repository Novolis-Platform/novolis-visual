using Novolis.Rendering.FrameGraph;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// Lowers frame-effect values into the renderer-owned standard FrameGraph.
/// Workflow graphs are never used to execute frame passes.
/// </summary>
public static class VisualFrameGraphAdapter
{
    public static VisualFrameGraphResult Build(
        IEnumerable<FrameEffectValue> effects,
        FrameGraphSize outputSize,
        FrameGraphQualityProfile? qualityProfile = null)
    {
        ArgumentNullException.ThrowIfNull(effects);
        var values = effects
            .OrderBy(effect => effect.EffectKind)
            .ThenBy(effect => effect.Amount)
            .ToArray();
        var quality = qualityProfile ?? FrameGraphQualityProfile.Default;
        var diagnostics = new List<VisualRenderingDiagnostic>();
        foreach (var effect in values)
        {
            var requiredFeature = effect.EffectKind switch
            {
                FrameEffectKind.Bloom => FrameGraphTargetFeature.Bloom,
                FrameEffectKind.ToneMapping => FrameGraphTargetFeature.ToneMapping,
                FrameEffectKind.ColorGrade => FrameGraphTargetFeature.ColorGrading,
                FrameEffectKind.Vignette => FrameGraphTargetFeature.Vignette,
                FrameEffectKind.Distortion => FrameGraphTargetFeature.Distortion,
                _ => FrameGraphTargetFeature.None,
            };
            if (requiredFeature != FrameGraphTargetFeature.None &&
                !quality.IsEnabled(requiredFeature))
            {
                diagnostics.Add(
                    new VisualRenderingDiagnostic(
                        VisualCompileDiagnosticSeverity.Warning,
                        "VRA510",
                        $"Frame effect '{effect.EffectKind}' is disabled by the selected quality profile.",
                        "frame-effects",
                        $"frame-effect.{effect.EffectKind}"));
            }
        }

        var description = FrameGraphPipelineBuilder.Build(
            outputSize,
            quality,
            "VisualAssetStudioFrame");
        var validation = description.Validate(qualityProfile: quality);
        diagnostics.AddRange(
            validation.Diagnostics
                .Where(diagnostic =>
                    diagnostic.Severity == FrameGraphDiagnosticSeverity.Warning)
                .Select(
                    diagnostic =>
                        new VisualRenderingDiagnostic(
                            VisualCompileDiagnosticSeverity.Warning,
                            diagnostic.Code,
                            diagnostic.Message,
                            "frame-graph",
                            "rendering.frame-graph")));
        var status = diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Error)
            ? VisualCapabilityStatus.Unsupported
            : diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Warning)
                ? VisualCapabilityStatus.Approximate
                : VisualCapabilityStatus.Exact;
        return new VisualFrameGraphResult(
            description,
            validation,
            values,
            diagnostics,
            status);
    }
}
