using Novolis.Modeling;
using Novolis.Rendering.Materials;

namespace Novolis.Visual.Adapters.Rendering;

/// <summary>
/// One evaluated modeling mesh and its already-lowered rendering material.
/// </summary>
public sealed record RenderingMeshInput
{
    public RenderingMeshInput(
        EvaluatedMesh mesh,
        IMaterial material,
        string? sourcePath = null)
    {
        Mesh = mesh ?? throw new ArgumentNullException(nameof(mesh));
        Material = material ?? throw new ArgumentNullException(nameof(material));
        SourcePath = sourcePath;
    }

    public EvaluatedMesh Mesh { get; }

    public IMaterial Material { get; }

    public string? SourcePath { get; }
}
