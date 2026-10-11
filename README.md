# novolis-visual

Host-neutral visual authoring contracts for the Asset Studio family.

The first package, `Novolis.Visual.Definitions`, defines immutable versioned
recipes, stable node identities, typed ports and connections, exposed
parameters, deterministic seed and dependency metadata, capability requirements,
typed values, allowlisted descriptors, schema migrations, and canonical JSON
fingerprints.

Semantic recipes deliberately contain no editor layout, Avalonia, Silk.NET, GPU
handle, or runtime-host dependency. `VisualEditorViewState` is a separate
record for positions, selection, and zoom.

## Build and test

```powershell
dotnet restore .\Novolis.Visual.slnx --configfile .\nuget.config
dotnet test .\tests\Novolis.Visual.Definitions.Unit\Novolis.Visual.Definitions.Unit.csproj
```

The committed package source policy is nuget.org plus Novolis GitHub Packages.
No folder feed is required.
