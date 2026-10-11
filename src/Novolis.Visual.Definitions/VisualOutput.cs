namespace Novolis.Visual.Definitions;

/// <summary>
/// A named recipe output bound to a typed node port.
/// </summary>
public sealed record VisualOutput
{
    public VisualOutput(
        string id,
        string name,
        VisualPortReference source,
        VisualTypeContract contract,
        bool required = true)
    {
        var validatedId = VisualValueValidation.Required(id, nameof(id));
        var validatedName = VisualValueValidation.Required(name, nameof(name));
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(contract);

        Id = validatedId;
        Name = validatedName;
        Source = source;
        Contract = contract;
        Required = required;
    }

    public string Id { get; }

    public string Name { get; }

    public VisualPortReference Source { get; }

    public VisualTypeContract Contract { get; }

    public bool Required { get; }
}
