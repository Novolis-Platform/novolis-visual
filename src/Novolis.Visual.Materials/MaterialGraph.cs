using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Materials;

/// <summary>
/// A reusable material graph and its friendly authoring surface.
/// </summary>
public sealed record MaterialGraph
{
    public MaterialGraph(
        string materialId,
        string displayName,
        VisualRecipe recipe,
        IEnumerable<MaterialChannelBinding>? channels = null,
        IEnumerable<MaterialParameterDefinition>? parameters = null,
        IEnumerable<MaterialLayer>? layers = null,
        IEnumerable<string>? tags = null)
    {
        MaterialId = Required(materialId, nameof(materialId));
        DisplayName = Required(displayName, nameof(displayName));
        ArgumentNullException.ThrowIfNull(recipe);

        Recipe = recipe;
        Channels = (channels ?? [])
            .OrderBy(binding => binding.Channel)
            .ToImmutableArray();
        Parameters = (parameters ?? [])
            .OrderBy(parameter => parameter.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        Layers = (layers ?? [])
            .OrderBy(layer => layer.Order)
            .ThenBy(layer => layer.Id, StringComparer.Ordinal)
            .ToImmutableArray();
        Tags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        EnsureUnique(
            Channels.Select(binding => binding.Channel),
            "material channel");
        EnsureUnique(
            Parameters.Select(parameter => parameter.Id),
            "material parameter");
        EnsureUnique(
            Layers.Select(layer => layer.Id),
            "material layer");
    }

    public string MaterialId { get; }

    public string DisplayName { get; }

    public VisualRecipe Recipe { get; }

    public ImmutableArray<MaterialChannelBinding> Channels { get; }

    public ImmutableArray<MaterialParameterDefinition> Parameters { get; }

    public ImmutableArray<MaterialLayer> Layers { get; }

    public ImmutableArray<string> Tags { get; }

    public MaterialParameterDefinition? FindParameter(string id) =>
        Parameters.FirstOrDefault(
            parameter => string.Equals(
                parameter.Id,
                id,
                StringComparison.Ordinal));

    public bool HasChannel(MaterialChannel channel) =>
        Channels.Any(binding => binding.Channel == channel);

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

    private static void EnsureUnique<T>(
        IEnumerable<T> values,
        string description)
    {
        var materialValues = values.ToArray();
        if (materialValues.Distinct().Count() != materialValues.Length)
        {
            throw new ArgumentException(
                $"Duplicate {description} values are not allowed.");
        }
    }
}
