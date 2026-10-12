using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// A reusable child material that overrides only exposed parent controls.
/// </summary>
public sealed record MaterialInstance
{
    public MaterialInstance(
        string instanceId,
        MaterialGraph parent,
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
                    $"Material parameter '{pair.Key}' is not exposed.",
                    nameof(overrides));
            if (!parameter.IsInstanceEditable)
            {
                throw new ArgumentException(
                    $"Material parameter '{pair.Key}' is not instance-editable.",
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
                    $"Duplicate material override '{pair.Key}'.",
                    nameof(overrides));
            }
        }

        Overrides = builder.ToImmutable();
    }

    public string InstanceId { get; }

    public MaterialGraph Parent { get; }

    public ImmutableSortedDictionary<string, VisualValue> Overrides { get; }

    public VisualValue Resolve(string parameterId)
    {
        var parameter = Parent.FindParameter(parameterId)
            ?? throw new KeyNotFoundException(
                $"Material parameter '{parameterId}' does not exist.");
        return Overrides.TryGetValue(parameterId, out var value)
            ? value
            : parameter.DefaultValue;
    }

    public MaterialInstance WithOverride(
        string parameterId,
        VisualValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var values = Overrides.SetItem(parameterId, value);
        return new MaterialInstance(InstanceId, Parent, values);
    }

    public MaterialInstance ResetOverride(string parameterId) =>
        new(
            InstanceId,
            Parent,
            Overrides.Remove(parameterId));

    public MaterialInstance ResetAll() =>
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
