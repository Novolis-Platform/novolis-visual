using Novolis.Visual.Definitions;

namespace Novolis.AssetStudio.Core;

/// <summary>
/// Immutable Asset Studio document containing revisioned semantic and view
/// snapshots.
/// </summary>
public sealed record AssetStudioDocument
{
    /// <summary>
    /// Creates a document from a semantic recipe.
    /// </summary>
    public AssetStudioDocument(
        VisualRecipe recipe,
        Guid? documentId = null,
        AssetStudioViewState? view = null,
        long revision = 0)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        DocumentId = documentId.GetValueOrDefault(Guid.NewGuid());
        if (DocumentId == Guid.Empty)
        {
            throw new ArgumentException(
                "A document identifier is required.",
                nameof(documentId));
        }

        Semantic = new AssetStudioSemanticState(recipe, revision);
        View = view ?? new AssetStudioViewState();
        Revision = revision;
    }

    private AssetStudioDocument(
        Guid documentId,
        AssetStudioSemanticState semantic,
        AssetStudioViewState view,
        long revision)
    {
        DocumentId = documentId;
        Semantic = semantic;
        View = view;
        Revision = revision;
    }

    /// <summary>
    /// Creates a new document with a generated identifier.
    /// </summary>
    public static AssetStudioDocument Create(
        VisualRecipe recipe,
        Guid? documentId = null) =>
        new(recipe, documentId);

    /// <summary>
    /// Stable identity for this document independent of semantic revisions.
    /// </summary>
    public Guid DocumentId { get; }

    /// <summary>
    /// The immutable semantic snapshot.
    /// </summary>
    public AssetStudioSemanticState Semantic { get; }

    /// <summary>
    /// The immutable editor view snapshot.
    /// </summary>
    public AssetStudioViewState View { get; }

    /// <summary>
    /// Total document revision. Semantic and view revisions are also available
    /// independently through <see cref="Semantic"/> and <see cref="View"/>.
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// Convenience access to the canonical recipe.
    /// </summary>
    public VisualRecipe Recipe => Semantic.Recipe;

    /// <summary>
    /// Convenience access to the semantic revision.
    /// </summary>
    public long SemanticRevision => Semantic.Revision;

    /// <summary>
    /// Convenience access to the view revision.
    /// </summary>
    public long ViewRevision => View.Revision;

    /// <summary>
    /// Returns a document with a new semantic recipe and a new semantic
    /// revision.
    /// </summary>
    public AssetStudioDocument WithRecipe(VisualRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        if (ReferenceEquals(recipe, Recipe) ||
            recipe.SemanticFingerprint() == Semantic.Fingerprint)
        {
            return this;
        }

        return new AssetStudioDocument(
            DocumentId,
            new AssetStudioSemanticState(recipe, Semantic.Revision + 1),
            View,
            Revision + 1);
    }

    /// <summary>
    /// Returns a document with new view state and a new view revision.
    /// </summary>
    public AssetStudioDocument WithView(AssetStudioViewState view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (view == View)
        {
            return this;
        }

        return new AssetStudioDocument(
            DocumentId,
            Semantic,
            new AssetStudioViewState(
                view.Editor,
                view.ActiveOutputId,
                view.PinnedNodes,
                View.Revision + 1),
            Revision + 1);
    }

    /// <summary>
    /// Restores semantic and view content from an earlier snapshot while
    /// keeping this document's revision counters monotonic.
    /// </summary>
    public AssetStudioDocument RestoreContent(AssetStudioDocument snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.DocumentId != DocumentId)
        {
            throw new ArgumentException(
                "The snapshot belongs to another document.",
                nameof(snapshot));
        }

        var semanticChanged = Semantic.Fingerprint != snapshot.Semantic.Fingerprint;
        var viewChanged = View != snapshot.View;
        if (!semanticChanged && !viewChanged)
        {
            return this;
        }

        return new AssetStudioDocument(
            DocumentId,
            semanticChanged
                ? new AssetStudioSemanticState(
                    snapshot.Recipe,
                    Semantic.Revision + 1)
                : Semantic,
            viewChanged
                ? new AssetStudioViewState(
                    snapshot.View.Editor,
                    snapshot.View.ActiveOutputId,
                    snapshot.View.PinnedNodes,
                    View.Revision + 1)
                : View,
            Revision + 1);
    }
}
