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
