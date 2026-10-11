namespace Novolis.Visual.Definitions;

[Flags]
public enum VisualAttributeRequirement
{
    None = 0,
    Position = 1 << 0,
    Normal = 1 << 1,
    Tangent = 1 << 2,
    Uv0 = 1 << 3,
    Uv1 = 1 << 4,
    Color = 1 << 5,
    MaterialIndex = 1 << 6,
    Time = 1 << 7,
    Seed = 1 << 8,
}
