# Novolis.Visual.Definitions

`Novolis.Visual.Definitions` is the semantic core of the visual schema. It is
immutable and host-neutral, with no Avalonia, Silk.NET, GPU, or editor-runtime
dependency.

```csharp
using Novolis.Visual.Definitions;

var node = VisualNode.Create(
    NodeId.Parse("00000000000000000000000000000001"),
    VisualBuiltInNodeTypes.ConstantScalar,
    version: 1,
    [new KeyValuePair<string, VisualValue>("value", new ScalarValue(0.5))]);

var recipe = new VisualRecipe("sample", [node]);
var json = VisualRecipeSerializer.Serialize(recipe);
var fingerprint = recipe.SemanticFingerprint();
```

Canonical JSON sorts object keys and semantic collections. Unknown node objects
are retained for round-tripping, while `VisualNodeRegistry` remains an explicit
allowlist for interpretation and validation. Schema migration is performed by
`VisualSchemaMigrationRegistry`.
