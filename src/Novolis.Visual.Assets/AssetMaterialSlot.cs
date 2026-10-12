namespace Novolis.Visual.Assets;

/// <summary>
/// A named material assignment point exposed by an asset.
/// </summary>
public sealed record AssetMaterialSlot
{
    public AssetMaterialSlot(
        string id,
        string name,
        string materialId,
        bool required = true)
    {
        Id = RequireText(id, nameof(id));
        Name = RequireText(name, nameof(name));
        MaterialId = RequireText(materialId, nameof(materialId));
        Required = required;
    }

    public string Id { get; }

    public string Name { get; }

    public string MaterialId { get; }

    public bool Required { get; }

    private static string RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "A non-empty value is required.",
                name);
        }

        return value.Trim();
    }
}
