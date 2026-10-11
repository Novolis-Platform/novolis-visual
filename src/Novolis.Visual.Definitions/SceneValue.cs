using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// A renderer-neutral scene reference and its stable object membership.
/// </summary>
public sealed record SceneValue : VisualValue
{
    public SceneValue(string sceneId, IEnumerable<string>? objectIds = null)
    {
        SceneId = VisualValueValidation.Required(sceneId, nameof(sceneId));
        ObjectIds = (objectIds ?? [])
            .Select(value => VisualValueValidation.Required(value, nameof(objectIds)))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    public string SceneId { get; }

    public ImmutableArray<string> ObjectIds { get; }

    public override VisualValueKind Kind => VisualValueKind.Scene;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);
}
