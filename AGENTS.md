Include ..\AGENTS.md

# Storage Tweaks — Mod-Specific Agent Instructions

## Identity
- **Assembly:** `storagetweaks`
- **Namespace:** `Calloatti.StorageTweaks`
- **ModId:** `calloatti.storagetweaks`
- **Framework:** Harmony, Bindito DI
- **Min Game Version:** 1.0.12.5 — uses `timberborn-decompiled-1.0.*`

## What This Mod Does
Tweaks storage capacity values and visualizations. Patches stockpile and warehouse capacity, adjusts good pile visualizers, and includes a dirt pile visualizer fix.

## Source Architecture (`Version-1.0/Source/`)

| File | Role |
|---|---|
| `ModStarter.cs` | Entry point — `IModStarter` |
| `StorageCapacityPatcher.cs` | Capacity override patches |
| `VolumeCalculator.cs` | Volume calculation utility |
| `OriginalCapacityFetcher.cs` | Original capacity fetch helper |
| `StockpileGoodPileVisualizerPatches.cs` | Good pile visual patches |
| `DirtPileVisualizerPatcher.cs` | Dirt pile visual fix |

## Build & Deploy

**Requirements:**
- .NET SDK (netstandard2.1)
- Unity Hub + Editor (for asset bundles) — path detected from `timberborn-modding-main/ProjectSettings/ProjectVersion.txt`
- Timberborn installed at `C:\Program Files (x86)\Steam\steamapps\common\timberborn_main` (set `TimberbornPath` in csproj if different)
- 0Harmony mod subscribed (Workshop ID `3284904751`)

**Commands (run from `Version-1.0/`):**
```powershell
dotnet build -c Release          # Builds + deploys to Documents\Timberborn\Mods\Storage Tweaks\Version-1.0\
.\unitybuild.ps1                  # Exports asset bundles via Unity (run before build if assets changed)
```

**Deploy target:** `%USERPROFILE%\Documents\Timberborn\Mods\Storage Tweaks\Version-1.0\`

**Pre/Post build scripts** (`prebuild.ps1`, `postbuild.ps1`) handle:
- Cleaning target dirs
- Backing up `workshop_data.json`
- Copying binaries + root assets (thumbnail, README, changelog, simpleconfig.txt)
- Stripping `.ps1`, `AGENTS.md`, `.zip` from deployed mod folder

## Configuration System (SimpleConfig)

- Schema defined in `Version-1.0/simpleconfig.txt`
- Runtime config saved to `%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\StorageTweaks.txt`
- Supports `bool`, `int`, `float`, `string` with validation, min/max, step, options, localization keys
- Auto-reloads on file change (500ms debounce)
- Access via `ModStarter.Config.GetInt(key)`, `GetBool(key)`, `GetFloat(key)`, `GetString(key)`

**Current settings:**
- `UseBagsForDirt` (bool, default false) — toggles dirt bag visualization

**Adding new config:**
1. Add entry to `simpleconfig.txt` (Key, Type, DefaultValue, Label, Tooltip, ControlType, MinValue, MaxValue, Step, RequiresReload/Restart)
2. Access via `ModStarter.Config.GetXxx(key)` in patchers

## Key Implementation Patterns

**Harmony patching:** `new Harmony("calloatti.storagetweaks").PatchAll()` in `ModStarter.StartMod`

**Publicized internals:** `CommonModSettings.props` lists 100+ `Timberborn.*` assemblies publicized via Krafs.Publicizer — allows direct field access (e.g., `specService._blueprintSourceService`)

**FieldRef for init-only props:** `AccessTools.FieldRefAccess<StockpileSpec, int>("<MaxCapacity>k__BackingField")` — required for record `{ get; init; }` properties

**Blueprint processing:** Iterate `specService._cachedBlueprintsBySpecs[typeof(StockpileSpec)]` — lazy-loaded blueprints with `Value` property

**Volume calculation:** `VolumeCalculator.Calculate(blueprint)` returns physical mesh bounds for visual limit

## File Structure Notes

```
Version-1.0/
├── Source/                    # Core patchers
├── SimpleConfig/              # Config system (shared across mods)
├── Resources/StockpileGoodModels/  # .timbermesh assets
├── GoodVisualization/         # Blueprint JSON for dirt pile fix
├── TemplateCollections/       # Blueprint overrides
├── Localizations/             # CSV translations
├── unitybuild.ps1             # Unity asset bundle export
├── prebuild.ps1 / postbuild.ps1  # Deploy automation
├── CommonModSettings.props    # Shared MSBuild config
└── Storage Tweaks.csproj      # Project file (set TimberbornPath here)
```

## TODO / Known Gaps
- Investigate `IBlueprintModifierProvider` to intercept blueprint loading (see example in original AGENTS.md)
- No automated tests — verify manually in-game
- Unity asset bundles require manual unitybuild.ps1 run when mesh/assets change

## Debugging
- Check `Player.log` for `[StorageTweaks]` prefix logs
- Config file at `%USERPROFILE%\AppData\LocalLow\Mechanistry\Timberborn\StorageTweaks.txt`
- Unity build log at `Version-1.0/unitybuild.log`