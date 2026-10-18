using System.Collections.Immutable;
using System.Numerics;
using Novolis.Visual.Compile;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>Result of generic asset geometry lowering.</summary>
public sealed record VisualAssetScenePreview(
    RenderingSceneConversion Rendering,
    Vector3 BoundsMinimum,
    Vector3 BoundsMaximum,
    ImmutableArray<VisualRenderingDiagnostic> Diagnostics,
    VisualCapabilityStatus Status);
