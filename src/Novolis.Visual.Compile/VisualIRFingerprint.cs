using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Novolis.Visual.Definitions;

namespace Novolis.Visual.Compile;

internal static class VisualIRFingerprint
{
    public static string Compute(VisualIR ir)
    {
        var builder = new StringBuilder();
        Append(builder, "recipe", ir.RecipeId);
        Append(builder, "schema", ir.SchemaVersion);
        Append(builder, "seed-mode", ir.SeedPolicy.Mode);
        Append(builder, "seed", ir.SeedPolicy.Seed);
        Append(builder, "seed-scope", ir.SeedPolicy.Scope);

        foreach (var node in ir.Nodes)
        {
            Append(builder, "node", node.Id);
            Append(builder, "type", node.TypeId);
            Append(builder, "operation", node.Operation);
            Append(builder, "contract", node.Contract);
            foreach (var input in node.Inputs)
            {
                Append(builder, "input-port", input.TargetPort.Value);
                Append(builder, "input-source", input.SourceNodeId);
                Append(builder, "input-source-port", input.SourcePort.Value);
                Append(builder, "input-contract", input.Contract);
            }

            Append(builder, "constant", node.ConstantValue);
        }

        foreach (var output in ir.Outputs)
        {
            Append(builder, "output", output.Id);
            Append(builder, "output-name", output.Name);
            Append(builder, "output-source", output.SourceNodeId);
            Append(builder, "output-port", output.SourcePort.Value);
            Append(builder, "output-contract", output.Contract);
            Append(builder, "output-required", output.Required);
        }

        foreach (var capability in ir.Capabilities.Results)
        {
            Append(builder, "capability", capability.Id);
            Append(builder, "capability-status", capability.Status);
            Append(builder, "capability-required", capability.Required);
            Append(builder, "capability-version", capability.MinimumVersion);
        }

        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())))
            .ToLowerInvariant();
    }

    private static void Append(StringBuilder builder, string name, object? value)
    {
        builder.Append(name)
            .Append('=')
            .Append(value switch
            {
                null => "<null>",
                NodeId nodeId => nodeId.ToString(),
                bool boolean => boolean ? "true" : "false",
                int integer => integer.ToString(CultureInfo.InvariantCulture),
                ulong unsigned => unsigned.ToString(CultureInfo.InvariantCulture),
                VisualTypeContract contract => Contract(contract),
                VisualValue visualValue => Value(visualValue),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            })
            .Append('\n');
    }

    private static string Contract(VisualTypeContract contract) =>
        string.Join(
            "|",
            (int)contract.ValueKind,
            (int)contract.Unit,
            (int)contract.CoordinateSpace,
            (int)contract.ColorSpace,
            (int)contract.Frequency,
            (int)contract.Purity,
            (int)contract.Attributes);

    private static string Value(VisualValue value) =>
        value switch
        {
            ScalarValue scalar => string.Join(
                "|",
                "scalar",
                scalar.Value.ToString("R", CultureInfo.InvariantCulture),
                (int)scalar.Unit),
            IntegerValue integer => string.Join(
                "|",
                "integer",
                integer.Value.ToString(CultureInfo.InvariantCulture),
                (int)integer.Unit),
            BooleanValue boolean => $"boolean|{(boolean.Value ? "true" : "false")}",
            ColorValue color => string.Join(
                "|",
                "color",
                color.Red.ToString("R", CultureInfo.InvariantCulture),
                color.Green.ToString("R", CultureInfo.InvariantCulture),
                color.Blue.ToString("R", CultureInfo.InvariantCulture),
                color.Alpha.ToString("R", CultureInfo.InvariantCulture),
                (int)color.ColorSpace),
            VectorValue vector => string.Join(
                "|",
                ["vector", .. vector.Components.Select(component =>
                    component.ToString("R", CultureInfo.InvariantCulture))]),
            TransformValue transform => string.Join(
                "|",
                "transform",
                Value(transform.Translation),
                Value(transform.Rotation),
                Value(transform.Scale),
                (int)transform.CoordinateSpace),
            ImageValue image => string.Join(
                "|",
                "image",
                image.AssetId,
                image.Width,
                image.Height,
                (int)image.Format,
                (int)image.ColorSpace,
                (int)image.AlphaMode),
            GeometryValue geometry => string.Join(
                "|",
                "geometry",
                geometry.AssetId,
                (int)geometry.GeometryKind,
                (int)geometry.CoordinateSpace,
                (int)geometry.Unit),
            FrameValue frame => string.Join(
                "|",
                "frame",
                frame.FrameIndex,
                frame.TimeSeconds.ToString("R", CultureInfo.InvariantCulture),
                frame.DeltaSeconds.ToString("R", CultureInfo.InvariantCulture),
                frame.FramesPerSecond.ToString("R", CultureInfo.InvariantCulture)),
            _ => string.Join("|", value.Kind, value.Contract),
        };
}
