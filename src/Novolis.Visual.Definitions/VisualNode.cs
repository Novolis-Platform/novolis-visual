using System.Collections.Immutable;

namespace Novolis.Visual.Definitions;

/// <summary>
/// An immutable node instance. Unknown node payloads are retained verbatim in
/// canonical form so a newer schema can pass them through an older reader.
/// </summary>
public sealed record VisualNode
{
    private VisualNode(
        NodeId id,
        string typeId,
        int version,
        ImmutableSortedDictionary<string, VisualValue> values,
        string? unknownPayloadJson)
    {
        if (id.IsEmpty)
        {
            throw new ArgumentException("A node identifier is required.", nameof(id));
        }

        Id = id;
        TypeId = VisualValueValidation.Required(typeId, nameof(typeId));

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Version = version;
        Values = values;
        UnknownPayloadJson = unknownPayloadJson;
    }

    public NodeId Id { get; }

    public string TypeId { get; }

    public int Version { get; }

    public ImmutableSortedDictionary<string, VisualValue> Values { get; }

    public string? UnknownPayloadJson { get; }

    public bool IsUnknown => UnknownPayloadJson is not null;

    public static VisualNode Create(
        NodeId id,
        string typeId,
        int version,
        IEnumerable<KeyValuePair<string, VisualValue>>? values = null)
    {
        var builder = ImmutableSortedDictionary.CreateBuilder<string, VisualValue>(
            StringComparer.Ordinal);

        if (values is not null)
        {
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value.Key))
                {
                    throw new ArgumentException(
                        "Node value names are required.",
                        nameof(values));
                }

                ArgumentNullException.ThrowIfNull(value.Value);
                if (!builder.TryAdd(value.Key, value.Value))
                {
                    throw new ArgumentException(
                        $"Duplicate node value '{value.Key}'.",
                        nameof(values));
                }
            }
        }

        return new VisualNode(id, typeId, version, builder.ToImmutable(), null);
    }

    /// <summary>
    /// Creates a node whose complete JSON object is preserved for round-tripping.
    /// </summary>
    public static VisualNode Unknown(
        NodeId id,
        string typeId,
        int version,
        string unknownPayloadJson)
    {
        ArgumentNullException.ThrowIfNull(unknownPayloadJson);
        var canonicalPayload = CanonicalJson.Normalize(unknownPayloadJson);

        using var document = System.Text.Json.JsonDocument.Parse(canonicalPayload);
        if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            throw new ArgumentException(
                "An unknown node payload must be a JSON object.",
                nameof(unknownPayloadJson));
        }

        return new VisualNode(
            id,
            typeId,
            version,
            ImmutableSortedDictionary<string, VisualValue>.Empty,
            canonicalPayload);
    }

    internal static VisualNode FromUnknownPayload(
        NodeId id,
        string typeId,
        int version,
        string canonicalPayloadJson) =>
        new(
            id,
            typeId,
            version,
            ImmutableSortedDictionary<string, VisualValue>.Empty,
            canonicalPayloadJson);

    public VisualNode WithValue(string name, VisualValue value)
    {
        if (IsUnknown)
        {
            throw new InvalidOperationException(
                "Unknown node payloads must be replaced as a complete payload.");
        }

        var values = Values.SetItem(
            VisualValueValidation.Required(name, nameof(name)),
            value ?? throw new ArgumentNullException(nameof(value)));

        return new VisualNode(Id, TypeId, Version, values, null);
    }
}
