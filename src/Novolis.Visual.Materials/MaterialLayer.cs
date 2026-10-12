namespace Novolis.Visual.Materials;

/// <summary>
/// Authoring projection metadata for an ordered material layer.
/// </summary>
public sealed record MaterialLayer
{
    public MaterialLayer(
        string id,
        string name,
        int order,
        double weight = 1,
        bool muted = false,
        bool solo = false)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order));
        }

        if (!double.IsFinite(weight) || weight is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(weight));
        }

        Order = order;
        Weight = weight;
        Muted = muted;
        Solo = solo;
    }

    public string Id { get; }

    public string Name { get; }

    public int Order { get; }

    public double Weight { get; }

    public bool Muted { get; }

    public bool Solo { get; }

    private static string Required(string value, string name)
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
