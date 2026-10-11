namespace Novolis.Visual.Baking;

/// <summary>
/// One deterministic RGBA pixel.
/// </summary>
public readonly record struct VisualRgba32(byte R, byte G, byte B, byte A = 255);
