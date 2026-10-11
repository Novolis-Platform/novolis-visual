using Novolis.Visual.Compile;

namespace Novolis.AssetStudio.Core;

/// <summary>
/// Host-neutral request for one preview target evaluation.
/// </summary>
public sealed record AssetStudioPreviewRequest
{
    /// <summary>
    /// Creates a preview request.
    /// </summary>
    public AssetStudioPreviewRequest(
        AssetStudioDocument document,
        string targetId,
        VisualIR? compilation = null,
        int width = 256,
        int height = 256)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException(
                "A preview target identifier is required.",
                nameof(targetId));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Document = document;
        TargetId = targetId;
        Compilation = compilation;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Document snapshot to preview.
    /// </summary>
    public AssetStudioDocument Document { get; }

    /// <summary>
    /// Target identifier owned by the host or adapter.
    /// </summary>
    public string TargetId { get; }

    /// <summary>
    /// Optional compilation matching the document semantic revision.
    /// </summary>
    public VisualIR? Compilation { get; }

    /// <summary>
    /// Requested width.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Requested height.
    /// </summary>
    public int Height { get; }
}
