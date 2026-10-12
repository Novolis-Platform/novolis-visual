using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// A friendly, serializable control exposed by a reusable material.
/// </summary>
public sealed record MaterialParameterDefinition
{
    public MaterialParameterDefinition(
        string id,
        string name,
        VisualTypeContract contract,
        VisualValue defaultValue,
        VisualParameterEditorMetadata? editor = null,
        bool isAnimatable = false,
        bool isInstanceEditable = true,
        MaterialParameterTarget? target = null)
    {
        Id = Required(id, nameof(id));
        Name = Required(name, nameof(name));
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(defaultValue);
        if (!contract.IsValueCompatibleWith(defaultValue.Contract))
        {
            throw new ArgumentException(
                "The default value does not match the parameter contract.",
                nameof(defaultValue));
        }

        Contract = contract;
        DefaultValue = defaultValue;
        Editor = editor ?? new VisualParameterEditorMetadata(
            displayName: name);
        IsAnimatable = isAnimatable;
        IsInstanceEditable = isInstanceEditable;
        Target = target;
    }

    public string Id { get; }

    public string Name { get; }

    public VisualTypeContract Contract { get; }

    public VisualValue DefaultValue { get; }

    public VisualParameterEditorMetadata Editor { get; }

    public bool IsAnimatable { get; }

    public bool IsInstanceEditable { get; }

    public MaterialParameterTarget? Target { get; }

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
