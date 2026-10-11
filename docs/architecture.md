# Novolis Visual and Asset Studio architecture

Status: accepted implementation baseline

This repository owns host-neutral visual authoring, compilation, baking, code
generation, and Asset Studio headless services. It does not own Avalonia,
Silk.NET, a window, a GPU context, or a second scene hierarchy.

## Existing authorities

- `Novolis.WorkflowEngine.Graph`, `.Flow`, `.Pipeline`, and `.FlowGraph` own
  topology, typed ports, linear composition, immutable graph snapshots, and
  structural diagnostics.
- `Novolis.Modeling.Scene` owns renderer-neutral scene documents, generators,
  modifiers, staged evaluation, and evaluated meshes.
- `Novolis.Rendering.Scene`, `.Materials`, `.Compile`, `.Runtime`, and backend
  packages own render authoring, material lowering, compiled scenes, runtime
  buffers, acceleration structures, and path-tracing execution.
- `Novolis.Rendering.Appearance` owns the current serializable appearance
  stack, CPU surface/volume/light/effect evaluation, recipes, and CPU baking.
- `Novolis.CodeGen.Pipeline` owns offline step orchestration, fingerprints,
  output checks, and cache records.
- `Novolis.Avalonia.*` owns reusable Avalonia controls and chrome.
- `Novolis.Silk.*` is the only package family allowed to reference Silk.NET.

Visual packages adapt to these authorities. None of those lower-level
authorities reference Visual or Asset Studio.

## Package ownership

The first stable projects are:

- `Novolis.Visual.Definitions`
- `Novolis.Visual.Compile`
- `Novolis.Visual.Evaluation`
- `Novolis.Visual.Baking`
- `Novolis.Visual.CodeGen`
- `Novolis.Visual.Adapters.Rendering`
- `Novolis.AssetStudio.Core`
- `Novolis.AssetStudio.Import`
- `Novolis.AssetStudio.Export`

The Avalonia editor is owned by `Novolis.Avalonia.AssetStudio` or the product
host after a second consumer proves that extraction is worthwhile. The
PackAsTool command is owned by `novolis-tools`; the product host is owned by
`novolis-apps`.

## Dependency rules

```text
WorkflowEngine.Graph/Flow/FlowGraph
                ↓
       Visual.Definitions
                ↓
       Visual.Compile/Evaluation/Baking/CodeGen
                ↓
   Visual rendering and modeling adapters
                ↓
     Rendering and Modeling authorities
```

The visual compiler may consume `FlowGraph`; it never executes a graph per
frame. `Novolis.Rendering.FrameGraph`, when added, owns graphics resource
lifetimes and pass scheduling and does not depend on `FlowGraph`.

Cross-repository references use `LibraryReference` or package references.
ProjectReference mode is only an evaluation mode supplied by governance.
Committed projects do not contain sibling-checkout references or local feeds.

## Current lab graduation

`novolis-lab/labs/visual/AssetStudioLab` is retained as a vertical-slice
fixture. Its tree AST, DSL, CPU evaluator, bake path, C# emitter, timeline
integration, and tests become migration fixtures for the stable packages.
The lab's `RenderingVisualProgram` and `SilkVisualProgram` remain neutral
handshake records until actual adapters are proven.

## Architecture decisions

### ADR-001: Visual ownership

Visual definitions and compilation live in `novolis-visual`, not
`novolis-rendering`. Rendering is a lower-level runtime authority and must not
depend on authoring schema, workflow execution, Roslyn, or editor services.

### ADR-002: One semantic representation

`VisualRecipe` is canonical. Stack, graph, DSL, inspector, and generated C#
are projections or targets. Arbitrary C# is not promised to round-trip into a
recipe.

### ADR-003: Geometry and mesh authority

Modeling owns geometry and scene evaluation. Rendering owns its runtime mesh
and material consumption. Visual adapters bridge the two and never introduce
a duplicate `SceneDocument` or universal render object.

### ADR-004: Backend capability truth

Targets report exact, approximate, baked-fallback, or unsupported status.
Unsupported procedural fields are baked or rejected; they are not silently
replaced with a visually unrelated preview.

### ADR-005: Frame graph separation

`Novolis.Rendering.FrameGraph` is renderer-specific and per-frame. It is not a
generic WorkflowEngine executor and is not the storage model for visual
recipes.

### ADR-006: Code-only output

Code-only means no required external runtime files, editor assemblies, recipe
JSON, graph interpreter, Roslyn runtime, or source paths. Embedded bytes,
generated pixels, compressed mesh data, and shader strings are allowed when
the export report records size and performance tradeoffs.

### ADR-007: Host isolation

Visual core has no Avalonia or Silk.NET references. Avalonia controls live in
the `Novolis.Avalonia.*` family or the product host. Silk.NET remains inside
`novolis-silk`; visual targets exchange Math/BCL handshake payloads.
