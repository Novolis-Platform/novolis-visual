using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Search and preview metadata for one node descriptor.
/// </summary>
public sealed record VisualNodeEditorMetadata
{
    public VisualNodeEditorMetadata(
        VisualNodeCategory category,
        IEnumerable<string>? keywords = null,
        VisualNodePreviewPolicy previewPolicy = VisualNodePreviewPolicy.None,
        string? description = null)
    {
        Category = category;
        Keywords = (keywords ?? [])
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Select(keyword => keyword.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(keyword => keyword, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        PreviewPolicy = previewPolicy;
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }

    public VisualNodeCategory Category { get; }

    public ImmutableArray<string> Keywords { get; }

    public VisualNodePreviewPolicy PreviewPolicy { get; }

    public string? Description { get; }
}
