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


TODO: Investigate IBlueprintModifierProvider to intercept blueprint loading

example code:

using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BlueprintSystem;
using Timberborn.Modding;

namespace MyCustomMod
{
    // 1. Create the Modifier Provider
    public class ConditionalBuildingModifier : IBlueprintModifierProvider
    {
        private readonly ModRepository _modRepository;
        
        public string ModifierName => "ConditionalBuildingModifier";

        // Inject the ModRepository to check for active mods
        public ConditionalBuildingModifier(ModRepository modRepository)
        {
            _modRepository = modRepository;
        }

        public IEnumerable<string> GetModifiers(string blueprintPath)
        {
            // Check if the game is loading the specific blueprint you want to be conditional
            if (blueprintPath.Contains("YourOptionalBuildingBlueprintName"))
            {
                // Check if the dependency mod is missing using its manifest ID
                if (_modRepository.ModIsNotEnabled("other.mod.manifest.id"))
                {
                    // Yield a JSON payload that modifies the blueprint at runtime.
                    // For example, this forces the building to be a DevModeTool, hiding it from normal players.
                    yield return "{\"PlaceableBlockObjectSpec\": {\"DevModeTool\": true}}";
                }
            }
        }
    }

    // 2. Bind the Provider in your Configurator
    [Context("MainMenu")]
    [Context("Game")]
    [Context("MapEditor")]
    internal class ConditionalBuildingConfigurator : Configurator
    {
        protected override void Configure()
        {
            // Bind the modifier provider so the SpecService picks it up during blueprint loading
            MultiBind<IBlueprintModifierProvider>().To<ConditionalBuildingModifier>().AsSingleton();
        }
    }
}