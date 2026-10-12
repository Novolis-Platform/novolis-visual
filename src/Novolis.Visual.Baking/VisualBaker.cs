using System.Collections.Immutable;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Novolis.Visual.Compile;
using Novolis.Visual.Definitions;
using Novolis.Visual.Evaluation;

namespace Novolis.Visual.Baking;

/// <summary>
/// Produces deterministic CPU reference pixels from a compiled visual recipe.
/// </summary>
public sealed class VisualBaker
{
    public VisualBakeResult Bake(
        VisualIR compilation,
        VisualBakeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        options ??= new VisualBakeOptions();
        options.Validate();
        if (!compilation.Success)
        {
            throw new InvalidOperationException(
                "A valid compilation is required before baking.");
        }

        var seed = Encoding.UTF8.GetBytes(
            $"{compilation.SemanticFingerprint}|{options.Target}|{options.Seed}");
        var digest = SHA256.HashData(seed);
        var pixels = ImmutableArray.CreateBuilder<VisualRgba32>(
            options.Width * options.Height);
        for (var y = 0; y < options.Height; y++)
        {
            for (var x = 0; x < options.Width; x++)
            {
                pixels.Add(EvaluatePixel(
                    compilation,
                    options.Target,
                    x,
                    y,
                    options.Width,
                    options.Height,
                    digest));
            }
        }

        var image = new VisualBakeImage
        {
            Width = options.Width,
            Height = options.Height,
            Pixels = pixels.ToImmutable(),
        };
        var png = image.ToPng();
        var contentHash = Convert.ToHexString(SHA256.HashData(png));
        var manifest = new VisualBakeManifest
        {
            RecipeFingerprint = compilation.SemanticFingerprint,
            Target = options.Target,
            Width = options.Width,
            Height = options.Height,
            ContentSha256 = contentHash,
            Metadata = ImmutableSortedDictionary<string, string>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("compilerTarget", compilation.Capabilities.Results.Length == 0
                    ? "cpu"
                    : "negotiated")
                .Add("pixelFormat", "rgba8"),
        };
        return new VisualBakeResult
        {
            Image = image,
            Manifest = manifest,
            PngBytes = png,
        };
    }

    private static VisualRgba32 EvaluatePixel(
        VisualIR compilation,
        VisualBakeTarget target,
        int x,
        int y,
        int width,
        int height,
        IReadOnlyList<byte> digest)
    {
        var u = width == 1 ? 0f : x / (float)(width - 1);
        var v = height == 1 ? 0f : y / (float)(height - 1);
        var context = new VisualEvaluationContext
        {
            Position = new Vector3(u, 0f, v),
            Uv = new Vector2(u, v),
            Normal = Vector3.UnitY,
            Time = 0d,
            Seed = digest[0],
        };
        var output = compilation.Outputs.FirstOrDefault();
        if (output is not null)
        {
            var evaluation = VisualEvaluator.Evaluate(
                compilation,
                context,
                new VisualEvaluationOptions());
            if (evaluation.TryGetOutput(output.Id, out var value) &&
                value is not null)
            {
                var evaluated = FromValue(value, target);
                if (evaluated is not null)
                {
                    return evaluated.Value;
                }
            }
        }

        return target switch
        {
            VisualBakeTarget.NormalMap => new VisualRgba32(
                ToByte(0.5f + (u - 0.5f) * 0.1f),
                ToByte(0.5f + (v - 0.5f) * 0.1f),
                255,
                255),
            VisualBakeTarget.RoughnessMap => new VisualRgba32(
                ToByte(u),
                ToByte(u),
                ToByte(u),
                255),
            VisualBakeTarget.EmissionMap => new VisualRgba32(
                digest[0],
                digest[1],
                digest[2],
                digest[3]),
            _ => new VisualRgba32(
                (byte)(digest[0] ^ (byte)(u * 31f)),
                (byte)(digest[1] ^ (byte)(v * 31f)),
                (byte)(digest[2] ^ (byte)((u + v) * 15f)),
                255),
        };
    }

    private static VisualRgba32? FromValue(
        VisualValue value,
        VisualBakeTarget target)
    {
        if (value is ColorValue color)
        {
            return new VisualRgba32(
                ToByte((float)color.Red),
                ToByte((float)color.Green),
                ToByte((float)color.Blue),
                ToByte((float)color.Alpha));
        }

        if (value is MaterialValue material)
        {
            var materialColor = target switch
            {
                VisualBakeTarget.EmissionMap => new ColorValue(
                    material.EmissionColor.Red * material.EmissionStrength,
                    material.EmissionColor.Green * material.EmissionStrength,
                    material.EmissionColor.Blue * material.EmissionStrength,
                    1),
                VisualBakeTarget.RoughnessMap => new ColorValue(
                    material.Roughness,
                    material.Roughness,
                    material.Roughness,
                    1),
                _ => material.BaseColor,
            };
            return new VisualRgba32(
                ToByte((float)materialColor.Red),
                ToByte((float)materialColor.Green),
                ToByte((float)materialColor.Blue),
                ToByte((float)materialColor.Alpha));
        }

        if (value is VectorValue vector && vector.Components.Length >= 3)
        {
            var red = vector.Components[0];
            var green = vector.Components[1];
            var blue = vector.Components[2];
            if (target == VisualBakeTarget.NormalMap)
            {
                red = red * 0.5d + 0.5d;
                green = green * 0.5d + 0.5d;
                blue = blue * 0.5d + 0.5d;
            }

            return new VisualRgba32(
                ToByte((float)red),
                ToByte((float)green),
                ToByte((float)blue),
                vector.Components.Length > 3
                    ? ToByte((float)vector.Components[3])
                    : (byte)255);
        }

        if (value is ScalarValue scalar)
        {
            var component = ToByte((float)scalar.Value);
            return new VisualRgba32(component, component, component, 255);
        }

        return null;
    }

    private static byte ToByte(float value) =>
        (byte)Math.Clamp((int)MathF.Round(value * 255f), 0, 255);
}
