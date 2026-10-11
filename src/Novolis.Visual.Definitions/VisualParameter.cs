namespace Novolis.Visual.Definitions;

/// <summary>
/// A recipe-level parameter exposed to an author or consuming tool.
/// </summary>
public sealed record VisualParameter
{
    public VisualParameter(
        string id,
        string name,
        VisualTypeContract contract,
        VisualValue value)
    {
        Id = VisualValueValidation.Required(id, nameof(id));
        Name = VisualValueValidation.Required(name, nameof(name));
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(value);

        if (!contract.IsValueCompatibleWith(value.Contract))
        {
            throw new ArgumentException(
                "The parameter value does not match its type contract.",
                nameof(value));
        }

        Contract = contract;
        Value = value;
    }

    public string Id { get; }

    public string Name { get; }

    public VisualTypeContract Contract { get; }

    public VisualValue Value { get; }
}
