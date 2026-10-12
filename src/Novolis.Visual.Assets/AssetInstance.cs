using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Assets;

/// <summary>
/// A child asset that inherits its parent's assembly and overrides controls.
/// </summary>
public sealed record AssetInstance
{
    public AssetInstance(
        string instanceId,
        AssetRecipe parent,
        IEnumerable<KeyValuePair<string, VisualValue>>? overrides = null)
    {
        InstanceId = Required(instanceId, nameof(instanceId));
        ArgumentNullException.ThrowIfNull(parent);
        Parent = parent;

        var builder = ImmutableSortedDictionary.CreateBuilder<
            string,
            VisualValue>(StringComparer.Ordinal);
        foreach (var pair in overrides ?? [])
        {
            var parameter = parent.FindParameter(pair.Key)
                ?? throw new ArgumentException(
                    $"Asset parameter '{pair.Key}' is not exposed.",
                    nameof(overrides));
            if (!parameter.IsInstanceEditable)
            {
                throw new ArgumentException(
                    $"Asset parameter '{pair.Key}' is not instance-editable.",
                    nameof(overrides));
            }

            ArgumentNullException.ThrowIfNull(pair.Value);
            if (!parameter.Contract.IsValueCompatibleWith(pair.Value.Contract))
            {
                throw new ArgumentException(
                    $"Override '{pair.Key}' does not match its contract.",
                    nameof(overrides));
            }

            if (!builder.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException(
                    $"Duplicate asset override '{pair.Key}'.",
                    nameof(overrides));
            }
        }

        Overrides = builder.ToImmutable();
    }

    public string InstanceId { get; }

    public AssetRecipe Parent { get; }

    public ImmutableSortedDictionary<string, VisualValue> Overrides { get; }

    public VisualValue Resolve(string parameterId)
    {
        var parameter = Parent.FindParameter(parameterId)
            ?? throw new KeyNotFoundException(
                $"Asset parameter '{parameterId}' does not exist.");
        return Overrides.TryGetValue(parameterId, out var value)
            ? value
            : parameter.DefaultValue;
    }

    public AssetInstance WithOverride(
        string parameterId,
        VisualValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new AssetInstance(
            InstanceId,
            Parent,
            Overrides.SetItem(parameterId, value));
    }

    public AssetInstance ResetOverride(string parameterId) =>
        new(
            InstanceId,
            Parent,
            Overrides.Remove(parameterId));

    public AssetInstance ResetAll() =>
        new(InstanceId, Parent);

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
