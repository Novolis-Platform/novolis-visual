using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Computes downstream invalidation without executing a graph.
/// </summary>
public static class VisualIncrementalAnalyzer
{
    public static VisualIncrementalAnalysis Analyze(
        VisualRecipe previous,
        VisualRecipe current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);

        var previousNodes = previous.Nodes.ToDictionary(node => node.Id);
        var currentNodes = current.Nodes.ToDictionary(node => node.Id);
        var changed = currentNodes.Keys
            .Where(id =>
                !previousNodes.TryGetValue(id, out var oldNode) ||
                !NodeEquivalent(oldNode!, currentNodes[id]))
            .ToHashSet();
        var removed = previousNodes.Keys
            .Where(id => !currentNodes.ContainsKey(id))
            .ToHashSet();
        var reasons = new HashSet<string>(StringComparer.Ordinal);

        if (changed.Count > 0 || removed.Count > 0)
        {
            reasons.Add("node");
        }

        var previousConnections = previous.Connections.ToDictionary(
            connection => connection.Id);
        var currentConnections = current.Connections.ToDictionary(
            connection => connection.Id);
        var changedConnectionIds = previousConnections.Keys
            .Concat(currentConnections.Keys)
            .Distinct()
            .Where(id =>
                !previousConnections.TryGetValue(id, out var oldConnection) ||
                !currentConnections.TryGetValue(id, out var newConnection) ||
                !ConnectionEquivalent(oldConnection!, newConnection))
            .ToArray();
        if (changedConnectionIds.Length > 0)
        {
            reasons.Add("connection");
            foreach (var connectionId in changedConnectionIds)
            {
                if (previousConnections.TryGetValue(connectionId, out var oldConnection))
                {
                    changed.Add(oldConnection.Source.NodeId);
                    changed.Add(oldConnection.Target.NodeId);
                }

                if (currentConnections.TryGetValue(connectionId, out var newConnection))
                {
                    changed.Add(newConnection.Source.NodeId);
                    changed.Add(newConnection.Target.NodeId);
                }
            }
        }

        if (!OutputsEquivalent(previous.Outputs, current.Outputs))
        {
            reasons.Add("output");
        }

        if (!SequenceEqual(previous.Parameters, current.Parameters))
        {
            reasons.Add("parameter");
        }

        if (!SequenceEqual(previous.Dependencies, current.Dependencies))
        {
            reasons.Add("dependency");
        }

        if (!SequenceEqual(previous.Capabilities, current.Capabilities))
        {
            reasons.Add("capability");
        }

        if (previous.SeedPolicy != current.SeedPolicy)
        {
            reasons.Add("seed");
        }

        if (previous.SchemaVersion != current.SchemaVersion)
        {
            reasons.Add("schema");
        }

        if (!string.Equals(previous.RecipeId, current.RecipeId, StringComparison.Ordinal))
        {
            reasons.Add("recipe");
        }

        var globalChange = reasons.Any(reason =>
            reason is "parameter" or "dependency" or "capability" or
            "seed" or "schema" or "recipe");
        var downstream = BuildDownstream(current.Connections);
        var affected = currentNodes.Keys
            .Where(changed.Contains)
            .ToHashSet();
        var pending = new Queue<NodeId>(
            affected.OrderBy(id => id));

        while (pending.Count > 0)
        {
            var nodeId = pending.Dequeue();
            if (!downstream.TryGetValue(nodeId, out var targets))
            {
                continue;
            }

            foreach (var target in targets.OrderBy(id => id))
            {
                if (affected.Add(target))
                {
                    pending.Enqueue(target);
                }
            }
        }

        var changedOutputIds = new HashSet<string>(StringComparer.Ordinal);
        var previousOutputs = previous.Outputs.ToDictionary(
            output => output.Id,
            StringComparer.Ordinal);
        foreach (var output in current.Outputs)
        {
            if (globalChange ||
                !previousOutputs.TryGetValue(output.Id, out var oldOutput) ||
                !Equals(oldOutput, output) ||
                affected.Contains(output.Source.NodeId))
            {
                changedOutputIds.Add(output.Id);
            }
        }

        return new VisualIncrementalAnalysis(
            changed.Where(currentNodes.ContainsKey),
            affected,
            removed,
            changedOutputIds,
            reasons);
    }

    private static Dictionary<NodeId, HashSet<NodeId>> BuildDownstream(
        IEnumerable<VisualConnection> connections)
    {
        var downstream = new Dictionary<NodeId, HashSet<NodeId>>();
        foreach (var connection in connections)
        {
            if (!downstream.TryGetValue(connection.Source.NodeId, out var targets))
            {
                targets = [];
                downstream.Add(connection.Source.NodeId, targets);
            }

            targets.Add(connection.Target.NodeId);
        }

        return downstream;
    }

    private static bool SequenceEqual<T>(
        IEnumerable<T> left,
        IEnumerable<T> right) =>
        left.SequenceEqual(right);

    private static bool ConnectionEquivalent(
        VisualConnection left,
        VisualConnection right) =>
        left.Id == right.Id &&
        left.Source == right.Source &&
        left.Target == right.Target &&
        ContractEquivalent(left.SourceContract, right.SourceContract) &&
        ContractEquivalent(left.TargetContract, right.TargetContract);

    private static bool NodeEquivalent(VisualNode left, VisualNode right)
    {
        if (left.Id != right.Id ||
            !string.Equals(left.TypeId, right.TypeId, StringComparison.Ordinal) ||
            left.Version != right.Version ||
            !string.Equals(
                left.UnknownPayloadJson,
                right.UnknownPayloadJson,
                StringComparison.Ordinal) ||
            left.Values.Count != right.Values.Count)
        {
            return false;
        }

        foreach (var value in left.Values)
        {
            if (!right.Values.TryGetValue(value.Key, out var rightValue) ||
                !ValueEquivalent(value.Value, rightValue))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ValueEquivalent(VisualValue left, VisualValue right) =>
        string.Equals(
            VisualRecipeSerializer.Serialize(
                new VisualRecipe("value", nodes:
                [
                    VisualNode.Create(
                        NodeId.Parse("ffffffffffffffffffffffffffffffff"),
                        "value",
                        1,
                        [new KeyValuePair<string, VisualValue>("value", left)]),
                ])),
            VisualRecipeSerializer.Serialize(
                new VisualRecipe("value", nodes:
                [
                    VisualNode.Create(
                        NodeId.Parse("ffffffffffffffffffffffffffffffff"),
                        "value",
                        1,
                        [new KeyValuePair<string, VisualValue>("value", right)]),
                ])),
            StringComparison.Ordinal);

    private static bool ContractEquivalent(
        VisualTypeContract left,
        VisualTypeContract right) =>
        left.ValueKind == right.ValueKind &&
        left.Unit == right.Unit &&
        left.CoordinateSpace == right.CoordinateSpace &&
        left.ColorSpace == right.ColorSpace &&
        left.Frequency == right.Frequency &&
        left.Purity == right.Purity &&
        left.Attributes == right.Attributes;

    private static bool OutputsEquivalent(
        IEnumerable<VisualOutput> left,
        IEnumerable<VisualOutput> right)
    {
        var leftValues = left.OrderBy(output => output.Id, StringComparer.Ordinal).ToArray();
        var rightValues = right.OrderBy(output => output.Id, StringComparer.Ordinal).ToArray();
        return leftValues.Length == rightValues.Length &&
               leftValues.Zip(rightValues).All(pair =>
                   string.Equals(pair.First.Id, pair.Second.Id, StringComparison.Ordinal) &&
                   string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal) &&
                   pair.First.Source == pair.Second.Source &&
                   Equals(pair.First.Contract, pair.Second.Contract) &&
                   pair.First.Required == pair.Second.Required);
    }
}
