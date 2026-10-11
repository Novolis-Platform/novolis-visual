# Novolis.Visual.Adapters.Rendering

This package is the host-neutral boundary between normalized VisualIR,
evaluated `Novolis.Modeling` meshes, and the existing
`Novolis.Rendering.Scene` and material contracts.

The adapter:

- measures geometry and appearance readiness from actual `EvaluatedMesh`
  attributes;
- copies positions, triangle indices, and the evaluated world transform into
  `MeshInstance`;
- lowers constant linear-sRGB PBR surfaces to `StandardMaterial`;
- lowers only constant `AppearanceStack` surface operations to a fixed
  material; and
- reports `Exact`, `Approximate`, `BakedFallback`, or `Unsupported` decisions
  with deterministic fingerprints, cache keys, source paths, and capability
  diagnostics.

The current `MeshInstance` contract carries positions, indices, a material,
and a transform. Optional modeling attributes are therefore measured and
reported separately rather than silently claimed as renderer inputs.

This adapter does not evaluate geometry generators, modifiers, booleans, LOD
policies, or mesh stacks. Those operations remain owned by
`Novolis.Modeling.Scene`; callers must provide an `EvaluatedMesh`.
Procedural appearance operations that cannot be represented by the fixed
material contract report `Unsupported` or an explicit bake requirement. The
adapter does not create textures or invoke a host.
