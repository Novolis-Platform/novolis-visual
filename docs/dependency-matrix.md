# Visual dependency matrix

This matrix is the review checklist for package references and architectural
boundary verification.

## Allowed dependency direction

| Project family | May reference | Must not reference |
| --- | --- | --- |
| `Novolis.Visual.Definitions` | WorkflowEngine Graph/Flow/FlowGraph, Math | Avalonia, Silk.NET, Rendering hosts, Roslyn, Asset Studio UI |
| `Novolis.Visual.Compile` | Visual Definitions, WorkflowEngine Flow/FlowGraph, Math | Avalonia, Silk.NET, runtime graph execution |
| `Novolis.Visual.Evaluation` | Visual Definitions/Compile, Math | Avalonia, Silk.NET, GPU APIs |
| `Novolis.Visual.Baking` | Visual Evaluation, Math, approved image/mesh contracts | Avalonia, Silk.NET, arbitrary process execution |
| `Novolis.Visual.CodeGen` | Visual Compile, CodeGen Pipeline/Roslyn tooling | editor UI, runtime Roslyn, Silk.NET |
| `Novolis.Visual.Adapters.Rendering` | Visual packages, Modeling.Scene, Rendering packages | Avalonia, Silk.NET, WorkflowEngine execution |
| `Novolis.Rendering.*` | Math and lower rendering packages | Visual authoring, WorkflowEngine execution, Avalonia, Silk.NET |
| `Novolis.Silk.*` | Math/BCL and Silk.NET | Rendering scene/material packages, Simulation |

## Existing API inventory

- Modeling: `SceneDocument`, `SceneNode`, `MeshNode`, `GeneratorNode`,
  `ModifierNode`, `SceneEvaluator`, `MeshStackEvaluator`, `EvaluatedMesh`,
  and `EditableMesh`.
- Rendering authoring: `Scene`, `MeshInstance`, `LightDefinition`, `IMaterial`,
  concrete materials, and `AppearanceStack`.
- Rendering compile/runtime: `MaterialCompiler`, `SceneCompiler`,
  `CompiledScene`, `GpuMaterial`, `IRayTracingBackend`, `IRenderOutput`, and
  `IFramePresenter`.
- Rendering appearance: `AppearanceEvaluator`, `AppearanceBaker`,
  `AppearanceRecipes`, `AppearancePost`, and the surface/volume/light sample
  records.
- Workflow: immutable topology and typed connection APIs in the four new
  WorkflowEngine packages.
- Build: `IPipelineStep`, input/output fingerprints, expected-output checks,
  and pipeline artifact records in `Novolis.CodeGen.Pipeline`.
- Editor foundations: `Novolis.Avalonia.Studio`,
  `Novolis.Avalonia.Layout`, `Novolis.Avalonia.GraphicalProfile`, and
  `Novolis.Avalonia.Rendering`.

## Required verification

Every package addition requires:

1. `dotnet run --project d:\novolis\novolis-tools\src\Novolis.Solution.Tool\Novolis.Solution.Tool.csproj --no-launch-profile -- generate --root d:\novolis`
2. `dotnet run --file d:\novolis\novolis-governance\scripts\verify-nuget-only.cs`
3. `pwsh -File d:\novolis\novolis-governance\scripts\verify-project-ref-mode.ps1 -SkipBuild`
4. `dotnet run --file d:\novolis\novolis-governance\scripts\verify-layer-boundaries.cs`
5. Relevant repository unit tests and a consumer build in ProjectReference mode.

No local package feed is valid for consumer validation. A missing published
package version is resolved by publishing through GitHub Packages, not by
adding a folder source or sibling checkout.
