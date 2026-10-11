# Novolis.Visual.Compile

`Novolis.Visual.Compile` binds an immutable `VisualRecipe` to an allowlisted
node registry and produces deterministic `VisualIR`.

The compiler validates typed ports, normalizes graph order, records source
maps, folds scalar constants, removes nodes that cannot reach a selected
output, detects cycles, negotiates target capabilities, and emits a semantic
fingerprint. It has no host, rendering, GPU, or executable-node dependency.
# Novolis.Visual.Compile

Compiles an allowlisted `VisualRecipe` into deterministic `VisualIR`. The
compiler validates typed connections, removes unreachable branches, folds
constant scalar operations, records source maps, fingerprints semantics, and
reports target capability decisions.
