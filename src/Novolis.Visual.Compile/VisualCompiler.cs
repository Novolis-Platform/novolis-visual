using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic binding, validation, normalization, and optimization for
/// visual recipes.
/// </summary>
public static class VisualCompiler
{
    public static VisualNodeRegistry CreateDefaultRegistry()
    {
        var scalar = new VisualTypeContract(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
        var color = new VisualTypeContract(
            VisualValueKind.Color,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.LinearSrgb);
        var descriptors = VisualNodeRegistry.CreateDefault().Descriptors
            .Concat(
            [
                BinaryDescriptor("multiply", "Multiply scalars", scalar),
                BinaryDescriptor("mul", "Multiply scalars", scalar),
                NoiseDescriptor("noise", "Deterministic scalar noise", scalar),
            ]);

        return new VisualNodeRegistry(descriptors);
    }

    public static VisualValidationResult Validate(
        VisualRecipe recipe,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var effectiveOptions = options ?? VisualCompileOptions.Default;
        return new VisualValidationResult(
            Bind(recipe, effectiveOptions).Diagnostics);
    }

    /// <summary>
    /// Binds recipe nodes and connections into an unoptimized IR.
    /// </summary>
    public static VisualIR Bind(
        VisualRecipe recipe,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        return Build(recipe, options ?? VisualCompileOptions.Default, optimize: false);
    }

    /// <summary>
    /// Normalizes a bound IR. Compilation is already normalized, so this
    /// overload is useful for callers that explicitly stage their pipeline.
    /// </summary>
    public static VisualIR Normalize(
        VisualIR bound,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(bound);
        var effectiveOptions = options ?? VisualCompileOptions.Default;

        if (!effectiveOptions.Optimize)
        {
            return bound;
        }

        var diagnostics = bound.Diagnostics.ToList();
        var nodes = bound.Nodes.ToList();
        var byId = nodes.ToDictionary(node => node.Id);

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];
            if (!TryFold(node, byId, out var folded))
            {
                continue;
            }

            nodes[index] = folded;
            byId[node.Id] = folded;
        }

        return new VisualIR(
            bound.RecipeId,
            bound.SchemaVersion,
            bound.SeedPolicy,
            nodes,
            bound.Outputs,
            diagnostics,
            bound.SourceMap,
            bound.Capabilities);
    }

    public static VisualIR Normalize(
        VisualRecipe recipe,
        VisualCompileOptions? options = null) =>
        Compile(recipe, options);

    /// <summary>
    /// Binds, validates, normalizes, optimizes, and fingerprints one recipe.
    /// </summary>
    public static VisualIR Compile(
        VisualRecipe recipe,
        VisualCompileOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var effectiveOptions = options ?? VisualCompileOptions.Default;
        var bound = Build(recipe, effectiveOptions, optimize: false);
        return effectiveOptions.Optimize
            ? Normalize(bound, effectiveOptions)
            : bound;
    }

    public static VisualIncrementalAnalysis AnalyzeIncremental(
        VisualRecipe previous,
        VisualRecipe current)
    {
        return VisualIncrementalAnalyzer.Analyze(previous, current);
    }

    private static VisualIR Build(
        VisualRecipe recipe,
        VisualCompileOptions options,
        bool optimize)
    {
        var diagnostics = options.Registry.Validate(recipe).Diagnostics.ToList();
        var capabilities = NegotiateCapabilities(recipe, options, diagnostics);
        var nodesById = recipe.Nodes.ToDictionary(node => node.Id);
        var descriptorById = recipe.Nodes.ToDictionary(
            node => node.Id,
            node => options.Registry.TryGetDescriptor(node.TypeId, out var descriptor)
                ? descriptor
                : null);
        var operations = new Dictionary<NodeId, string>();
        var irNodes = new Dictionary<NodeId, VisualIRNode>();

        foreach (var node in recipe.Nodes)
        {
            var descriptor = descriptorById[node.Id];
            var operation = GetOperation(node.TypeId);
            operations[node.Id] = operation;
            var contract = FindOutputContract(node, descriptor, recipe.Connections);
            var constant = ResolveConstant(node, descriptor, operation);

            try
            {
                irNodes[node.Id] = new VisualIRNode(
                    node.Id,
                    node.TypeId,
                    operation,
                    contract,
                    constantValue: constant);
            }
            catch (ArgumentException)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS110",
                    $"Constant value for node '{node.Id}' does not satisfy its output contract.",
                    node.Id));
                irNodes[node.Id] = new VisualIRNode(
                    node.Id,
                    node.TypeId,
                    operation,
                    contract);
            }
        }

        var boundInputPorts = new HashSet<(NodeId NodeId, string PortId)>();
        foreach (var connection in recipe.Connections)
        {
            if (!irNodes.TryGetValue(connection.Target.NodeId, out var target) ||
                !irNodes.ContainsKey(connection.Source.NodeId))
            {
                continue;
            }

            if (!boundInputPorts.Add(
                    (connection.Target.NodeId, connection.Target.PortId.Value)))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS105",
                    $"Input port '{connection.Target.PortId}' has more than one connection.",
                    connection.Target.NodeId,
                    connection.Id));
            }

            var inputs = target.Inputs
                .Where(input => input.TargetPort != connection.Target.PortId)
                .Append(
                    new VisualIRInput(
                        connection.Target.PortId,
                        connection.Source.NodeId,
                        connection.Source.PortId,
                        connection.TargetContract));
            irNodes[connection.Target.NodeId] = new VisualIRNode(
                target.Id,
                target.TypeId,
                target.Operation,
                target.Contract,
                inputs,
                target.ConstantValue);
        }

        foreach (var node in recipe.Nodes)
        {
            if (!descriptorById.TryGetValue(node.Id, out var descriptor) ||
                descriptor is null ||
                !irNodes.TryGetValue(node.Id, out var boundNode))
            {
                continue;
            }

            foreach (var port in descriptor.Ports.Where(port =>
                         port.Direction == PortDirection.Input &&
                         port.Required &&
                         !boundNode.Inputs.Any(input =>
                             input.TargetPort == port.Id)))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS106",
                    $"Required input port '{port.Id}' is not connected.",
                    node.Id));
            }
        }

        DetectCycles(recipe, nodesById.Keys, diagnostics);

        var selectedOutputs = recipe.Outputs
            .Where(output =>
                options.SelectedOutputIds.Count == 0 ||
                options.SelectedOutputIds.Contains(output.Id))
            .ToArray();

        foreach (var selectedOutputId in options.SelectedOutputIds)
        {
            if (recipe.Outputs.All(output => output.Id != selectedOutputId))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS040",
                    $"Selected output '{selectedOutputId}' does not exist."));
            }
        }

        var live = DetermineLiveNodes(
            recipe,
            selectedOutputs,
            options.EliminateDeadNodes);
        var orderedIds = TopologicalOrder(
            live,
            recipe.Connections,
            irNodes.Keys);
        var orderedNodes = orderedIds
            .Where(irNodes.ContainsKey)
            .Select(id => irNodes[id])
            .ToList();

        var outputs = selectedOutputs
            .Select(output =>
                new VisualIROutput(
                    output.Id,
                    output.Name,
                    output.Source.NodeId,
                    output.Source.PortId,
                    output.Contract,
                    output.Required))
            .ToArray();
        var sourceMap = new VisualSourceMap(
            orderedNodes
                .Select(node =>
                    new VisualSourceMapEntry(
                        $"nodes/{node.Id}",
                        node.Id,
                        typeId: node.TypeId))
                .Concat(
                    outputs.Select(output =>
                        new VisualSourceMapEntry(
                            $"outputs/{output.Id}",
                            outputId: output.Id,
                            portId: output.SourcePort.Value))));

        var result = new VisualIR(
            recipe.RecipeId,
            recipe.SchemaVersion,
            recipe.SeedPolicy,
            orderedNodes,
            outputs,
            diagnostics,
            sourceMap,
            capabilities);

        return optimize ? Normalize(result, options) : result;
    }

    private static VisualCapabilityReport NegotiateCapabilities(
        VisualRecipe recipe,
        VisualCompileOptions options,
        ICollection<VisualDiagnostic> diagnostics)
    {
        var results = new List<VisualCapabilityResult>();
        foreach (var requirement in recipe.Capabilities)
        {
            var support = options.TargetCapabilities.GetSupport(
                requirement.Id,
                requirement.MinimumVersion);
            var result = new VisualCapabilityResult(
                requirement.Id,
                support.Status,
                requirement.Required,
                requirement.MinimumVersion,
                support.Message);
            results.Add(result);

            if (support.Status == VisualCapabilityStatus.Unsupported &&
                requirement.Required)
            {
                diagnostics.Add(new(
                    options.Strict
                        ? VisualDiagnosticSeverity.Error
                        : VisualDiagnosticSeverity.Warning,
                    "VIS300",
                    $"Required capability '{requirement.Id}' is unsupported."));
            }
            else if (support.Status is VisualCapabilityStatus.Approximate
                     or VisualCapabilityStatus.BakedFallback)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Warning,
                    "VIS301",
                    $"Capability '{requirement.Id}' uses {support.Status}."));
            }
        }

        return new VisualCapabilityReport(results);
    }

    private static VisualTypeContract FindOutputContract(
        VisualNode node,
        VisualNodeDescriptor? descriptor,
        IEnumerable<VisualConnection> connections)
    {
        var descriptorContract = descriptor?.Ports
            .Where(port => port.Direction == PortDirection.Output)
            .Select(port => port.Contract)
            .FirstOrDefault();
        if (descriptorContract is not null)
        {
            return descriptorContract;
        }

        var connectionContract = connections
            .Where(connection => connection.Source.NodeId == node.Id)
            .Select(connection => connection.SourceContract)
            .FirstOrDefault();
        if (connectionContract is not null)
        {
            return connectionContract;
        }

        var valueContract = node.Values.Values
            .Select(value => value.Contract)
            .FirstOrDefault();
        return valueContract ?? ScalarContract();
    }

    private static VisualValue? ResolveConstant(
        VisualNode node,
        VisualNodeDescriptor? descriptor,
        string operation)
    {
        if (!operation.StartsWith("constant.", StringComparison.Ordinal))
        {
            return null;
        }

        if (node.Values.TryGetValue("value", out var value))
        {
            return value;
        }

        return descriptor?.Parameters
            .FirstOrDefault(parameter => parameter.Name == "value")
            ?.DefaultValue;
    }

    private static string GetOperation(string typeId)
    {
        var normalized = typeId.Trim().ToLowerInvariant();
        if (normalized == VisualBuiltInNodeTypes.ConstantScalar)
        {
            return "constant.scalar";
        }

        if (normalized == VisualBuiltInNodeTypes.ConstantColor)
        {
            return "constant.color";
        }

        if (normalized == VisualBuiltInNodeTypes.MaterialOutput)
        {
            return "material.output";
        }

        const string constantPrefix = "novolis.visual.constant.";
        if (normalized.StartsWith(
                constantPrefix,
                StringComparison.Ordinal))
        {
            return "constant." + normalized[constantPrefix.Length..];
        }

        if (normalized is VisualCompileBuiltInNodeTypes.ConstantVector or
            "constant.vector" or
            "vector.constant")
        {
            return "constant.vector";
        }

        if (normalized == VisualBuiltInNodeTypes.AddScalar ||
            normalized.EndsWith(".add", StringComparison.Ordinal) ||
            normalized == "add")
        {
            return "add.scalar";
        }

        if (normalized.Contains("multiply", StringComparison.Ordinal) ||
            normalized.EndsWith(".mul", StringComparison.Ordinal) ||
            normalized is "multiply" or "mul")
        {
            return "multiply.scalar";
        }

        if (normalized.Contains("noise", StringComparison.Ordinal) ||
            normalized.Contains("hash", StringComparison.Ordinal))
        {
            return "noise.scalar";
        }

        return normalized;
    }

    private static bool TryFold(
        VisualIRNode node,
        IReadOnlyDictionary<NodeId, VisualIRNode> byId,
        out VisualIRNode folded)
    {
        folded = node;
        if (node.Operation is not ("add.scalar" or "multiply.scalar") ||
            node.Inputs.Length < 2)
        {
            return false;
        }

        var values = node.Inputs
            .Select(input =>
                byId.TryGetValue(input.SourceNodeId, out var source)
                    ? source.ConstantValue
                    : null)
            .ToArray();
        if (values.Any(value => value is not ScalarValue))
        {
            return false;
        }

        var left = (ScalarValue)values[0]!;
        var right = (ScalarValue)values[1]!;
        if (left.Unit != right.Unit)
        {
            return false;
        }

        var result = node.Operation == "add.scalar"
            ? left.Value + right.Value
            : left.Value * right.Value;
        folded = new VisualIRNode(
            node.Id,
            node.TypeId,
            "constant.scalar",
            node.Contract,
            constantValue: new ScalarValue(result, left.Unit));
        return true;
    }

    private static HashSet<NodeId> DetermineLiveNodes(
        VisualRecipe recipe,
        IReadOnlyList<VisualOutput> selectedOutputs,
        bool eliminateDeadNodes)
    {
        if (!eliminateDeadNodes || recipe.Outputs.Length == 0)
        {
            return recipe.Nodes.Select(node => node.Id).ToHashSet();
        }

        var incoming = recipe.Connections
            .GroupBy(connection => connection.Target.NodeId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(connection => connection.Source.NodeId).ToArray());
        var live = new HashSet<NodeId>();
        var pending = new Stack<NodeId>(
            selectedOutputs
                .Select(output => output.Source.NodeId)
                .OrderBy(id => id));

        while (pending.Count > 0)
        {
            var nodeId = pending.Pop();
            if (!live.Add(nodeId) ||
                !incoming.TryGetValue(nodeId, out var sources))
            {
                continue;
            }

            foreach (var source in sources.OrderBy(id => id))
            {
                pending.Push(source);
            }
        }

        return live;
    }

    private static IReadOnlyList<NodeId> TopologicalOrder(
        IReadOnlySet<NodeId> included,
        IEnumerable<VisualConnection> connections,
        IEnumerable<NodeId> allIds)
    {
        var indegree = included.ToDictionary(id => id, _ => 0);
        var outgoing = included.ToDictionary(id => id, _ => new List<NodeId>());

        foreach (var connection in connections)
        {
            if (!included.Contains(connection.Source.NodeId) ||
                !included.Contains(connection.Target.NodeId))
            {
                continue;
            }

            outgoing[connection.Source.NodeId].Add(connection.Target.NodeId);
            indegree[connection.Target.NodeId]++;
        }

        var remaining = new SortedSet<NodeId>(
            indegree
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));
        var result = new List<NodeId>(included.Count);
        while (remaining.Count > 0)
        {
            var current = remaining.Min;
            remaining.Remove(current);
            result.Add(current);

            foreach (var target in outgoing[current].OrderBy(id => id))
            {
                indegree[target]--;
                if (indegree[target] == 0)
                {
                    remaining.Add(target);
                }
            }
        }

        result.AddRange(
            allIds
                .Where(included.Contains)
                .Except(result)
                .OrderBy(id => id));
        return result;
    }

    private static void DetectCycles(
        VisualRecipe recipe,
        IEnumerable<NodeId> nodeIds,
        ICollection<VisualDiagnostic> diagnostics)
    {
        var ids = nodeIds.ToHashSet();
        var indegree = ids.ToDictionary(id => id, _ => 0);
        var outgoing = ids.ToDictionary(id => id, _ => new List<NodeId>());
        foreach (var connection in recipe.Connections)
        {
            if (!ids.Contains(connection.Source.NodeId) ||
                !ids.Contains(connection.Target.NodeId))
            {
                continue;
            }

            outgoing[connection.Source.NodeId].Add(connection.Target.NodeId);
            indegree[connection.Target.NodeId]++;
        }

        var queue = new SortedSet<NodeId>(
            indegree
                .Where(pair => pair.Value == 0)
                .Select(pair => pair.Key));
        var visited = new HashSet<NodeId>();
        while (queue.Count > 0)
        {
            var id = queue.Min;
            queue.Remove(id);
            visited.Add(id);
            foreach (var target in outgoing[id].OrderBy(target => target))
            {
                indegree[target]--;
                if (indegree[target] == 0)
                {
                    queue.Add(target);
                }
            }
        }

        foreach (var cycleNode in ids.Except(visited).OrderBy(id => id))
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS120",
                "The visual graph contains a cycle.",
                cycleNode));
        }
    }

    private static VisualNodeDescriptor ConstantDescriptor(
        string typeId,
        string displayName,
        VisualTypeContract contract,
        VisualValue defaultValue) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("value"),
                    PortDirection.Output,
                    contract),
            ],
            parameters:
            [
                new VisualParameterDescriptor(
                    "value",
                    contract,
                    defaultValue,
                    isExposed: true),
            ]);

    private static VisualNodeDescriptor BinaryDescriptor(
        string typeId,
        string displayName,
        VisualTypeContract contract) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("left"),
                    PortDirection.Input,
                    contract),
                new VisualPortDescriptor(
                    new PortId("right"),
                    PortDirection.Input,
                    contract),
                new VisualPortDescriptor(
                    new PortId("result"),
                    PortDirection.Output,
                    contract),
            ]);

    private static VisualNodeDescriptor NoiseDescriptor(
        string typeId,
        string displayName,
        VisualTypeContract contract) =>
        new(
            typeId,
            1,
            displayName,
            ports:
            [
                new VisualPortDescriptor(
                    new PortId("result"),
                    PortDirection.Output,
                    contract),
            ]);

    private static VisualTypeContract ScalarContract() =>
        new(
            VisualValueKind.Scalar,
            VisualUnit.Unitless,
            CoordinateSpace.None,
            ColorSpace.None);
}
#if false
using System.Collections.Immutable;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

/// <summary>
/// Deterministic recipe compiler that lowers semantic definitions into VisualIR.
/// </summary>
public sealed class VisualCompiler
{
    /// <summary>
    /// Compiles one recipe for a selected target.
    /// </summary>
    public VisualCompilationResult Compile(
        VisualRecipe recipe,
        VisualCompilationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        options ??= new VisualCompilationOptions();
        var registry = options.Registry ?? VisualNodeRegistry.CreateDefault();
        var diagnostics = new List<VisualCompileDiagnostic>();
        var validation = recipe.Validate(registry);
        diagnostics.AddRange(validation.Diagnostics.Select(ToDiagnostic));

        var capabilities = Negotiate(recipe, options, diagnostics);
        if (options.Strict &&
            diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Error))
        {
            return CreateFailure(recipe, diagnostics, capabilities);
        }

        var nodesById = recipe.Nodes.ToDictionary(node => node.Id);
        var outgoing = BuildAdjacency(recipe, nodesById);
        var incoming = BuildIncoming(recipe, nodesById);
        var selected = SelectLiveNodes(recipe, options, incoming);
        var order = TopologicalOrder(
            selected,
            outgoing,
            incoming,
            diagnostics);
        if (diagnostics.Any(diagnostic =>
                diagnostic.Severity == VisualCompileDiagnosticSeverity.Error))
        {
            return CreateFailure(recipe, diagnostics, capabilities);
        }

        var irNodes = new List<VisualIrNode>();
        var foldedScalars = new Dictionary<NodeId, double>();
        var sourceMap = ImmutableSortedDictionary.CreateBuilder<
            string,
            VisualSourceMapEntry>(StringComparer.Ordinal);

        foreach (var id in order)
        {
            var node = nodesById[id];
            var descriptor = registry.TryGetDescriptor(node.TypeId, out var found)
                ? found
                : null;
            var contract = FindOutputContract(descriptor) ??
                new VisualTypeContract(
                    VisualValueKind.Scalar,
                    VisualUnit.Unitless,
                    CoordinateSpace.None,
                    ColorSpace.None);
            var inputs = incoming[id]
                .Where(selected.Contains)
                .OrderBy(connection => connection.Id)
                .Select(connection => connection.Source.NodeId)
                .Distinct()
                .ToImmutableArray();
            var operation = node.TypeId;
            var values = node.Values;
            var folded = false;

            if (options.Optimize &&
                string.Equals(node.TypeId, VisualBuiltInNodeTypes.AddScalar, StringComparison.Ordinal) &&
                inputs.Length == 2 &&
                foldedScalars.TryGetValue(inputs[0], out var left) &&
                foldedScalars.TryGetValue(inputs[1], out var right))
            {
                var result = new ScalarValue(left + right);
                foldedScalars[id] = result.Value;
                operation = VisualBuiltInNodeTypes.ConstantScalar;
                values = values.SetItem("value", result);
                inputs = [];
                folded = true;
            }
            else if (TryGetScalarConstant(node, out var constant))
            {
                foldedScalars[id] = constant;
            }

            irNodes.Add(new VisualIrNode(
                id,
                operation,
                contract,
                inputs,
                values,
                folded));
            sourceMap[id.ToString()] = new VisualSourceMapEntry(
                id,
                node.TypeId,
                $"nodes[{id}]");
        }

        var outputs = recipe.Outputs
            .Where(output => selected.Contains(output.Source.NodeId))
            .OrderBy(output => output.Id, StringComparer.Ordinal)
            .Select(output => new VisualIrOutput(
                output.Id,
                output.Name,
                output.Source.NodeId,
                output.Source.PortId,
                output.Contract))
            .ToImmutableArray();
        var ir = new VisualIrRecipe
        {
            RecipeId = recipe.RecipeId,
            Nodes = irNodes.ToImmutableArray(),
            Outputs = outputs,
            SourceMap = sourceMap.ToImmutable(),
            Fingerprint = recipe.SemanticFingerprint(),
        };

        return new VisualCompilationResult
        {
            Ir = ir,
            Diagnostics = SortDiagnostics(diagnostics),
            Capabilities = capabilities,
        };
    }

    private static VisualCompilationResult CreateFailure(
        VisualRecipe recipe,
        IEnumerable<VisualCompileDiagnostic> diagnostics,
        VisualCapabilityReport capabilities) =>
        new()
        {
            Ir = null,
            Diagnostics = SortDiagnostics(diagnostics),
            Capabilities = capabilities,
        };

    private static VisualCapabilityReport Negotiate(
        VisualRecipe recipe,
        VisualCompilationOptions options,
        ICollection<VisualCompileDiagnostic> diagnostics)
    {
        var decisions = ImmutableSortedDictionary.CreateBuilder<
            string,
            VisualCapabilityStatus>(StringComparer.Ordinal);
        foreach (var requirement in recipe.Capabilities.OrderBy(
                     capability => capability.Id,
                     StringComparer.Ordinal))
        {
            var status = options.Capabilities.Exact.Contains(requirement.Id)
                ? VisualCapabilityStatus.Exact
                : options.Capabilities.Approximate.Contains(requirement.Id)
                    ? VisualCapabilityStatus.Approximate
                    : options.Capabilities.BakedFallback.Contains(requirement.Id)
                        ? VisualCapabilityStatus.BakedFallback
                        : VisualCapabilityStatus.Unsupported;
            decisions[requirement.Id] = status;
            if (status == VisualCapabilityStatus.Unsupported && requirement.Required)
            {
                diagnostics.Add(new(
                    VisualCompileDiagnosticSeverity.Error,
                    "VIS-CAP",
                    $"Target '{options.Target}' does not support capability '{requirement.Id}'."));
            }
            else if (status != VisualCapabilityStatus.Exact)
            {
                diagnostics.Add(new(
                    VisualCompileDiagnosticSeverity.Warning,
                    "VIS-CAP-APPROX",
                    $"Capability '{requirement.Id}' lowers as {status}."));
            }
        }

        return new VisualCapabilityReport
        {
            Decisions = decisions.ToImmutable(),
        };
    }

    private static HashSet<NodeId> SelectLiveNodes(
        VisualRecipe recipe,
        VisualCompilationOptions options,
        IReadOnlyDictionary<NodeId, List<VisualConnection>> incoming)
    {
        var roots = options.SelectedOutputs is { Count: > 0 }
            ? options.SelectedOutputs
            : recipe.Outputs.Select(output => output.Source.NodeId).ToHashSet();
        if (roots.Count == 0)
        {
            return recipe.Nodes.Select(node => node.Id).ToHashSet();
        }

        var live = new HashSet<NodeId>();
        var pending = new Stack<NodeId>(roots.OrderByDescending(id => id));
        while (pending.TryPop(out var current))
        {
            if (!live.Add(current) || !incoming.TryGetValue(current, out var inputs))
            {
                continue;
            }

            foreach (var input in inputs
                         .OrderByDescending(connection => connection.Source.NodeId))
            {
                pending.Push(input.Source.NodeId);
            }
        }

        return live;
    }

    private static IReadOnlyList<NodeId> TopologicalOrder(
        IReadOnlySet<NodeId> selected,
        IReadOnlyDictionary<NodeId, HashSet<NodeId>> outgoing,
        IReadOnlyDictionary<NodeId, List<VisualConnection>> incoming,
        ICollection<VisualCompileDiagnostic> diagnostics)
    {
        var indegree = selected.ToDictionary(
            id => id,
            id => incoming[id].Count(connection =>
                selected.Contains(connection.Source.NodeId)));
        var pending = new PriorityQueue<NodeId, NodeId>();
        foreach (var id in selected.Where(id => indegree[id] == 0).OrderBy(id => id))
        {
            pending.Enqueue(id, id);
        }

        var order = new List<NodeId>(selected.Count);
        while (pending.TryDequeue(out var current, out _))
        {
            order.Add(current);
            foreach (var next in outgoing[current]
                         .Where(selected.Contains)
                         .OrderBy(id => id))
            {
                indegree[next]--;
                if (indegree[next] == 0)
                {
                    pending.Enqueue(next, next);
                }
            }
        }

        foreach (var cycleNode in selected.Except(order).OrderBy(id => id))
        {
            diagnostics.Add(new(
                VisualCompileDiagnosticSeverity.Error,
                "VIS-CYCLE",
                "The selected visual graph contains a cycle.",
                cycleNode));
        }

        return order;
    }

    private static Dictionary<NodeId, HashSet<NodeId>> BuildAdjacency(
        VisualRecipe recipe,
        IReadOnlyDictionary<NodeId, VisualNode> nodes)
    {
        var outgoing = nodes.Keys.ToDictionary(
            id => id,
            _ => new HashSet<NodeId>());
        foreach (var connection in recipe.Connections)
        {
            if (outgoing.TryGetValue(connection.Source.NodeId, out var targets) &&
                nodes.ContainsKey(connection.Target.NodeId))
            {
                targets.Add(connection.Target.NodeId);
            }
        }

        return outgoing;
    }

    private static Dictionary<NodeId, List<VisualConnection>> BuildIncoming(
        VisualRecipe recipe,
        IReadOnlyDictionary<NodeId, VisualNode> nodes)
    {
        var incoming = nodes.Keys.ToDictionary(
            id => id,
            _ => new List<VisualConnection>());
        foreach (var connection in recipe.Connections)
        {
            if (incoming.TryGetValue(connection.Target.NodeId, out var inputs) &&
                nodes.ContainsKey(connection.Source.NodeId))
            {
                inputs.Add(connection);
            }
        }

        return incoming;
    }

    private static VisualTypeContract? FindOutputContract(
        VisualNodeDescriptor? descriptor) =>
        descriptor?.Ports
            .Where(port => port.Direction == PortDirection.Output)
            .OrderBy(port => port.Id.Value, StringComparer.Ordinal)
            .Select(port => port.Contract)
            .FirstOrDefault();

    private static bool TryGetScalarConstant(
        VisualNode node,
        out double value)
    {
        value = 0d;
        if (!string.Equals(
                node.TypeId,
                VisualBuiltInNodeTypes.ConstantScalar,
                StringComparison.Ordinal))
        {
            return false;
        }

        var scalar = node.Values.Values.OfType<ScalarValue>().FirstOrDefault();
        if (scalar is null)
        {
            return false;
        }

        value = scalar.Value;
        return true;
    }

    private static VisualCompileDiagnostic ToDiagnostic(
        VisualDiagnostic diagnostic) =>
        new(
            diagnostic.Severity == VisualDiagnosticSeverity.Error
                ? VisualCompileDiagnosticSeverity.Error
                : VisualCompileDiagnosticSeverity.Warning,
            diagnostic.Code,
            diagnostic.Message,
            diagnostic.NodeId,
            diagnostic.ConnectionId);

    private static ImmutableArray<VisualCompileDiagnostic> SortDiagnostics(
        IEnumerable<VisualCompileDiagnostic> diagnostics) =>
        diagnostics
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.NodeId)
            .ThenBy(diagnostic => diagnostic.ConnectionId)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ToImmutableArray();
}
#endif
