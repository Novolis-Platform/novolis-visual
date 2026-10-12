using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// Shared node catalog used by authoring hosts and semantic consumers.
/// </summary>
public sealed class VisualNodeCatalog
{
    private readonly ImmutableArray<VisualNodeCatalogEntry> entries;

    public VisualNodeCatalog(
        VisualNodeRegistry registry,
        IEnumerable<VisualNodeCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(entries);
        Registry = registry;
        this.entries = entries
            .OrderBy(entry => entry.Category)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.TypeId, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public VisualNodeRegistry Registry { get; }

    public ImmutableArray<VisualNodeCatalogEntry> Entries => entries;

    public IEnumerable<VisualNodeCatalogEntry> Search(
        string? query = null,
        VisualNodeCategory? category = null)
    {
        var normalized = query?.Trim();
        return entries
            .Where(entry => category is null || entry.Category == category)
            .Where(entry => string.IsNullOrWhiteSpace(normalized) ||
                            Matches(entry, normalized!));
    }

    public static VisualNodeCatalog CreateDefault()
    {
        return FromRegistry(VisualNodeRegistry.CreateDefault());
    }

    public static VisualNodeCatalog FromRegistry(
        VisualNodeRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        var entries = registry.Descriptors.Select(
            descriptor =>
                new VisualNodeCatalogEntry(
                    descriptor,
                    DefaultMetadata(descriptor)));
        return new VisualNodeCatalog(registry, entries);
    }

    private static bool Matches(
        VisualNodeCatalogEntry entry,
        string query)
    {
        var editor = entry.Editor;
        return entry.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               entry.TypeId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               (editor.Description?.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase) ?? false) ||
               editor.Keywords.Any(keyword =>
                   keyword.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private static VisualNodeEditorMetadata DefaultMetadata(
        VisualNodeDescriptor descriptor)
    {
        var type = descriptor.TypeId;
        var category = type switch
        {
            var value when value.Contains("color", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Color,
            var value when value.Contains("material", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Surface,
            var value when value.Contains("emission", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Emission,
            var value when value.Contains("frame", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Effects,
            var value when value.Contains("light", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Assembly,
            var value when value.Contains("volume", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Glass,
            var value when value.Contains("output", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Output,
            var value when value.Contains("noise", StringComparison.OrdinalIgnoreCase) =>
                VisualNodeCategory.Pattern,
            _ => VisualNodeCategory.Inputs,
        };

        var preview = descriptor.Ports.Any(
            port => port.Direction == PortDirection.Output &&
                    port.Contract.ValueKind == VisualValueKind.Material)
            ? VisualNodePreviewPolicy.Material
            : descriptor.Ports.Any(
                port => port.Direction == PortDirection.Output &&
                        port.Contract.ValueKind == VisualValueKind.Color)
                ? VisualNodePreviewPolicy.Color
                : VisualNodePreviewPolicy.None;
        return new VisualNodeEditorMetadata(
            category,
            [descriptor.DisplayName, type],
            preview,
            descriptor.DisplayName);
    }
}
