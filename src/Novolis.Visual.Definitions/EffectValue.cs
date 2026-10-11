using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// A named effect with immutable, typed effect parameters.
/// </summary>
public sealed record EffectValue : VisualValue
{
    public EffectValue(
        string effectId,
        IEnumerable<KeyValuePair<string, VisualValue>>? parameters = null)
    {
        EffectId = VisualValueValidation.Required(effectId, nameof(effectId));
        Parameters = CreateParameters(parameters);
    }

    public string EffectId { get; }

    public ImmutableSortedDictionary<string, VisualValue> Parameters { get; }

    public override VisualValueKind Kind => VisualValueKind.Effect;

    public override VisualTypeContract Contract => new(
        Kind,
        VisualUnit.Unitless,
        CoordinateSpace.None,
        ColorSpace.None);

    private static ImmutableSortedDictionary<string, VisualValue> CreateParameters(
        IEnumerable<KeyValuePair<string, VisualValue>>? parameters)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, VisualValue>(
            StringComparer.Ordinal);

        if (parameters is null)
        {
            return builder.ToImmutable();
        }

        foreach (var parameter in parameters)
        {
            if (string.IsNullOrWhiteSpace(parameter.Key))
            {
                throw new ArgumentException(
                    "Effect parameter names are required.",
                    nameof(parameters));
            }

            ArgumentNullException.ThrowIfNull(parameter.Value);
            if (!builder.TryAdd(parameter.Key, parameter.Value))
            {
                throw new ArgumentException(
                    $"Duplicate effect parameter '{parameter.Key}'.",
                    nameof(parameters));
            }
        }

        return builder.ToImmutable();
    }
}
