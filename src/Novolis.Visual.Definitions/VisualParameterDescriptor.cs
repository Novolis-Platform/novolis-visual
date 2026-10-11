namespace Novolis.Visual.Definitions;

/// <summary>
/// Declares a typed parameter that a node can expose to recipe authors.
/// </summary>
public sealed record VisualParameterDescriptor
{
    public VisualParameterDescriptor(
        string name,
        VisualTypeContract contract,
        VisualValue? defaultValue = null,
        bool isExposed = false,
        string? description = null)
    {
        Name = VisualValueValidation.Required(name, nameof(name));
        ArgumentNullException.ThrowIfNull(contract);

        if (defaultValue is not null &&
            !contract.IsValueCompatibleWith(defaultValue.Contract))
        {
            throw new ArgumentException(
                "The parameter default does not match its type contract.",
                nameof(defaultValue));
        }

        Contract = contract;
        DefaultValue = defaultValue;
        IsExposed = isExposed;
        Description = description;
    }

    public string Name { get; }

    public VisualTypeContract Contract { get; }

    public VisualValue? DefaultValue { get; }

    public bool IsExposed { get; }

    public string? Description { get; }
}
