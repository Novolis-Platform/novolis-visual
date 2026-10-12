using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// A friendly control exposed by a reusable assembled asset.
/// </summary>
public sealed record AssetParameterDefinition
{
    public AssetParameterDefinition(
        string id,
        string name,
        VisualTypeContract contract,
        VisualValue defaultValue,
        VisualParameterEditorMetadata? editor = null,
        bool isInstanceEditable = true,
        AssetParameterTarget? target = null)
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
        IsInstanceEditable = isInstanceEditable;
        Target = target;
    }

    public string Id { get; }

    public string Name { get; }

    public VisualTypeContract Contract { get; }

    public VisualValue DefaultValue { get; }

    public VisualParameterEditorMetadata Editor { get; }

    public bool IsInstanceEditable { get; }

    public AssetParameterTarget? Target { get; }

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
