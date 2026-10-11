# Visual schema baseline

`VisualRecipe` is the semantic representation. It contains:

- stable `NodeId` values and immutable node instances;
- typed input/output ports and connections;
- exposed parameters and named outputs;
- seed policy, dependency fingerprints, and target capabilities;
- explicit units, coordinate spaces, color spaces, field frequency, purity,
  and required attributes.

`VisualValue` covers scalar, integer, boolean, color, vector, transform, image,
geometry, surface, light, volume, effect, scene, and frame values. Values and
port contracts carry the metadata needed to reject an unsafe connection before
compilation.

## Persistence

`VisualRecipeSerializer.Serialize` emits canonical compact JSON. Keys and
semantic collections are sorted with ordinal comparison. The SHA-256 result of
that JSON is the semantic fingerprint, so editor layout cannot change it.

`VisualEditorViewState` is intentionally separate from `VisualRecipe`; it can
hold node positions, selection, and zoom without becoming semantic input.

Unknown node objects are stored as canonical JSON payloads. A reader can
validate the known portion of a recipe while retaining future node data for a
later writer. `VisualNodeRegistry` is an explicit allowlist and never creates
types by reflection or by a type name from JSON.

## Versioning

The current schema version is `1`. `VisualSchemaMigrationRegistry` applies
registered migrations in order. The built-in migration upgrades version `0`
documents by mapping legacy `links` to `connections`, adding missing semantic
collections, and assigning node versions.
