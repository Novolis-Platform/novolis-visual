using System.Collections.Immutable;
using Novolis.Rendering.FrameGraph;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// FrameGraph description and capability diagnostics for visual frame effects.
/// </summary>
public sealed class VisualFrameGraphResult
{
    internal VisualFrameGraphResult(
        FrameGraphDescription description,
        FrameGraphValidationResult validation,
        IEnumerable<FrameEffectValue> effects,
        IEnumerable<VisualRenderingDiagnostic> diagnostics,
        VisualCapabilityStatus status)
    {
        Description = description;
        Validation = validation;
        Effects = effects.ToImmutableArray();
        Diagnostics = RenderingAdapterDiagnostics.Order(diagnostics);
        Status = status;
        Fingerprint = RenderingAdapterFingerprint.CreateCacheKey(
            "visual-frame-graph",
            description.Name,
            description.OutputSize.ToString(),
            string.Join("|", Effects.Select(effect => effect.ToString())));
    }

    public FrameGraphDescription Description { get; }

    public FrameGraphValidationResult Validation { get; }

    public ImmutableArray<FrameEffectValue> Effects { get; }

    public ImmutableArray<VisualRenderingDiagnostic> Diagnostics { get; }

    public VisualCapabilityStatus Status { get; }

    public string Fingerprint { get; }

    public bool IsUsable =>
        Status != VisualCapabilityStatus.Unsupported &&
        Validation.IsValid;
}
