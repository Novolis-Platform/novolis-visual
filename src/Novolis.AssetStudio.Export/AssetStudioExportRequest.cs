using Novolis.AssetStudio.Core;

namespace Novolis.AssetStudio.Export;

/// <summary>
/// In-memory request for one deterministic export profile.
/// </summary>
public sealed record AssetStudioExportRequest
{
    /// <summary>
    /// Creates an export request.
    /// </summary>
    public AssetStudioExportRequest(
        AssetStudioDocument document,
        AssetStudioExportProfile profile,
        AssetStudioExportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        Profile = profile;
        Options = options ?? new AssetStudioExportOptions();
    }

    /// <summary>
    /// Immutable document snapshot to export.
    /// </summary>
    public AssetStudioDocument Document { get; }

    /// <summary>
    /// Selected export profile.
    /// </summary>
    public AssetStudioExportProfile Profile { get; }

    /// <summary>
    /// Export options and bounds.
    /// </summary>
    public AssetStudioExportOptions Options { get; }
}
