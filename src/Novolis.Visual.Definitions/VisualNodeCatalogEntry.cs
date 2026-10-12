namespace Novolis.Visual.Definitions;

/// <summary>
/// A searchable, user-facing node catalog item.
/// </summary>
public sealed record VisualNodeCatalogEntry
{
    public VisualNodeCatalogEntry(
        VisualNodeDescriptor descriptor,
        VisualNodeEditorMetadata editor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(editor);
        Descriptor = descriptor;
        Editor = editor;
    }

    public VisualNodeDescriptor Descriptor { get; }

    public VisualNodeEditorMetadata Editor { get; }

    public string TypeId => Descriptor.TypeId;

    public string DisplayName => Descriptor.DisplayName;

    public VisualNodeCategory Category => Editor.Category;
}
