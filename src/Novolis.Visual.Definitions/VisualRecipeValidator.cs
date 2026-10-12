namespace Novolis.Visual.Definitions;

internal static class VisualRecipeValidator
{
    public static VisualValidationResult Validate(
        VisualRecipe recipe,
        VisualNodeRegistry registry)
    {
        var diagnostics = new List<VisualDiagnostic>();
        var nodes = recipe.Nodes.ToDictionary(node => node.Id);

        foreach (var node in recipe.Nodes)
        {
            if (node.IsUnknown)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS001",
                    $"Node type '{node.TypeId}' is not known to the registry.",
                    node.Id));
                continue;
            }

            if (!registry.TryGetDescriptor(node.TypeId, out var descriptor) ||
                descriptor is null)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS001",
                    $"Node type '{node.TypeId}' is not allowlisted.",
                    node.Id));
                continue;
            }

            if (node.Version > descriptor.Version)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS002",
                    $"Node version {node.Version} is newer than descriptor version {descriptor.Version}.",
                    node.Id));
            }

            foreach (var value in node.Values)
            {
                var parameter = descriptor.Parameters.FirstOrDefault(
                    candidate => candidate.Name == value.Key);

                if (parameter is null)
                {
                    diagnostics.Add(new(
                        VisualDiagnosticSeverity.Warning,
                        "VIS003",
                        $"Node value '{value.Key}' is not declared by the descriptor.",
                        node.Id));
                }
                else if (!parameter.Contract.IsValueCompatibleWith(value.Value.Contract))
                {
                    diagnostics.Add(new(
                        VisualDiagnosticSeverity.Error,
                        "VIS004",
                        $"Node value '{value.Key}' does not satisfy its descriptor contract.",
                        node.Id));
                }
            }
        }

        foreach (var connection in recipe.Connections)
        {
            ValidateConnection(connection, nodes, registry, diagnostics);
        }

        foreach (var output in recipe.Outputs)
        {
            ValidateOutput(output, nodes, registry, diagnostics);
        }

        foreach (var parameter in recipe.Parameters)
        {
            if (!parameter.Contract.IsValueCompatibleWith(parameter.Value.Contract))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS010",
                    $"Recipe parameter '{parameter.Id}' does not satisfy its contract."));
            }
        }

        return new VisualValidationResult(diagnostics);
    }

    private static void ValidateConnection(
        VisualConnection connection,
        IReadOnlyDictionary<NodeId, VisualNode> nodes,
        VisualNodeRegistry registry,
        ICollection<VisualDiagnostic> diagnostics)
    {
        if (!nodes.TryGetValue(connection.Source.NodeId, out var sourceNode))
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS020",
                "Connection source node does not exist.",
                ConnectionId: connection.Id));
        }

        if (!nodes.TryGetValue(connection.Target.NodeId, out var targetNode))
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS021",
                "Connection target node does not exist.",
                ConnectionId: connection.Id));
        }

        if (sourceNode is null ||
            targetNode is null ||
            sourceNode.IsUnknown ||
            targetNode.IsUnknown)
        {
            return;
        }

        if (!registry.TryGetDescriptor(sourceNode.TypeId, out var sourceDescriptor) ||
            sourceDescriptor is null ||
            !registry.TryGetDescriptor(targetNode.TypeId, out var targetDescriptor) ||
            targetDescriptor is null)
        {
            return;
        }

        var sourcePort = sourceDescriptor.FindPort(connection.Source.PortId);
        var targetPort = targetDescriptor.FindPort(connection.Target.PortId);

        if (sourcePort is null)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS022",
                "Connection source port is not declared.",
                sourceNode.Id,
                connection.Id));
        }
        else
        {
            if (sourcePort.Direction != PortDirection.Output)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS023",
                    "Connection source port must be an output.",
                    sourceNode.Id,
                    connection.Id));
            }

            if (!sourcePort.Contract.IsCompatibleWith(connection.SourceContract) &&
                !sourcePort.Contract.IsValueCompatibleWith(
                    connection.SourceContract) &&
                !IsLegacySourceContractCompatible(
                    sourcePort.Contract,
                    connection.SourceContract))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS024",
                    "Stored source contract differs from the descriptor.",
                    sourceNode.Id,
                    connection.Id));
            }
        }

        if (targetPort is null)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS025",
                "Connection target port is not declared.",
                targetNode.Id,
                connection.Id));
        }
        else
        {
            if (targetPort.Direction != PortDirection.Input)
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS026",
                    "Connection target port must be an input.",
                    targetNode.Id,
                    connection.Id));
            }

            if (!targetPort.Contract.IsCompatibleWith(connection.TargetContract))
            {
                diagnostics.Add(new(
                    VisualDiagnosticSeverity.Error,
                    "VIS027",
                    "Stored target contract differs from the descriptor.",
                    targetNode.Id,
                    connection.Id));
            }
        }

        if (!connection.IsTypeCompatible)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS028",
                "Connection source and target contracts are incompatible.",
                ConnectionId: connection.Id));
        }
    }

    private static bool IsLegacySourceContractCompatible(
        VisualTypeContract descriptor,
        VisualTypeContract stored) =>
        descriptor.ValueKind == stored.ValueKind &&
        descriptor.Unit == stored.Unit &&
        descriptor.ColorSpace == stored.ColorSpace &&
        stored.CoordinateSpace == CoordinateSpace.None &&
        stored.Frequency == FieldFrequency.Constant &&
        stored.Purity == FieldPurity.Pure &&
        stored.Attributes == VisualAttributeRequirement.None;

    private static void ValidateOutput(
        VisualOutput output,
        IReadOnlyDictionary<NodeId, VisualNode> nodes,
        VisualNodeRegistry registry,
        ICollection<VisualDiagnostic> diagnostics)
    {
        if (!nodes.TryGetValue(output.Source.NodeId, out var node))
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS030",
                "Output source node does not exist."));
            return;
        }

        if (node.IsUnknown ||
            !registry.TryGetDescriptor(node.TypeId, out var descriptor) ||
            descriptor is null)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS031",
                "Output source node is not allowlisted.",
                node.Id));
            return;
        }

        var port = descriptor.FindPort(output.Source.PortId);
        if (port is null)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS032",
                "Output source port is not declared.",
                node.Id));
            return;
        }

        if (port.Direction != PortDirection.Output)
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS033",
                "Recipe outputs must bind to output ports.",
                node.Id));
        }

        if (!port.Contract.IsCompatibleWith(output.Contract))
        {
            diagnostics.Add(new(
                VisualDiagnosticSeverity.Error,
                "VIS034",
                "Output contract differs from the descriptor.",
                node.Id));
        }
    }
}
