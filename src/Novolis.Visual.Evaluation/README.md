# Novolis.Visual.Evaluation

`Novolis.Visual.Evaluation` is the deterministic CPU reference for normalized
visual IR. It evaluates scalar, color, and vector constants, scalar arithmetic,
and deterministic noise-like operations. Each sample can include an ordered
source trace for diagnostics, previews, and focused tests.

The evaluator uses an allowlisted operation set and does not deserialize or
invoke arbitrary delegates.
