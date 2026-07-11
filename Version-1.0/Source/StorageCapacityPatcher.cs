using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using Timberborn.Stockpiles;
using Timberborn.BlueprintSystem;

namespace Calloatti.StorageTweaks
{
  [HarmonyPatch(typeof(SpecService), "Load")]
  public static class StorageCapacityPatcher
  {
    // Store the true calculated visual limits based on building size
    public static readonly Dictionary<string, int> VisualLimits = new Dictionary<string, int>();

    // Replaced Reflection FieldInfo with Harmony's FieldRef for direct, zero-allocation memory access.
    // This is required because MaxCapacity is an { get; init; } property on a record.
    public static AccessTools.FieldRef<StockpileSpec, int> MaxCapacityRef = AccessTools.FieldRefAccess<StockpileSpec, int>("<MaxCapacity>k__BackingField");

    [HarmonyPostfix]
    public static void Postfix(SpecService __instance)
    {
      Debug.Log("[StorageTweaks] SpecService.Load Postfix started. Applying capacities...");
      ProcessConfig(__instance);
      Debug.Log("[StorageTweaks] Finished applying storage capacities.");
    }

    private static void ProcessConfig(SpecService specService)
    {
      try
      {
        VisualLimits.Clear();

        // Directly access publicized fields - no reflection needed
        var sourceService = specService._blueprintSourceService;
        var deserializer = specService._blueprintDeserializer;
        var specDict = specService._cachedBlueprintsBySpecs;

        if (specDict == null || deserializer == null || sourceService == null) return;

        // Native dictionary lookup instead of enumerating everything
        if (!specDict.TryGetValue(typeof(StockpileSpec), out var lazyList)) return;

        foreach (var lazyObj in lazyList)
        {
          var blueprint = lazyObj.Value;
          if (blueprint == null) continue;

          // --- CALCULATE TRUE VISUAL LIMIT ---
          // Use the VolumeCalculator to find the physical bounds of the mesh
          int visualLimit = Mathf.RoundToInt(VolumeCalculator.Calculate(blueprint));
          if (visualLimit > 0)
          {
            VisualLimits[blueprint.Name] = visualLimit;
          }

          // 1. Get Default Capacity
          string rawJson = OriginalCapacityFetcher.GetRawJson(sourceService, blueprint);
          int defaultCap = OriginalCapacityFetcher.GetOriginalCapacity(blueprint, rawJson);
          if (defaultCap <= 0) defaultCap = blueprint.GetSpec<StockpileSpec>().MaxCapacity;

          // 2. Modded Capacity using SimpleConfig
          int moddedCap = defaultCap;

          if (ModStarter.Config.HasKey(blueprint.Name))
          {
            moddedCap = ModStarter.Config.GetInt(blueprint.Name);
            if (moddedCap <= 0) moddedCap = defaultCap;
          }
          else
          {
            ModStarter.Config.Set(blueprint.Name, defaultCap);
          }

          // Always apply SetInlineComment to force legacy file comments into the modern layout structure
          ModStarter.Config.SetInlineComment(
            key: blueprint.Name,
            type: "int",
            defaultValue: defaultCap,
            label: blueprint.Name,
            tooltip: "Sets the maximum item capacity for this storage building.",
            controlType: "slider",
            minValue: 1,
            maxValue: 100000,
            step: 10,
            requiresReload: true
          );

          Debug.Log($"[StorageTweaks] {blueprint.Name} | Default: {defaultCap} | Modded: {moddedCap} | Limit: {visualLimit}");

          // 3. Apply modded capacity if it differs from the default
          if (moddedCap != defaultCap)
          {
            MaxCapacityRef(blueprint.GetSpec<StockpileSpec>()) = moddedCap;
          }
        }

        Debug.Log("[StorageTweaks] Saving dynamic keys via SimpleConfig...");
        ModStarter.Config.Save();
      }
      catch (Exception ex)
      {
        Debug.LogError($"[StorageTweaks] Error in ProcessConfig: {ex}");
      }
    }
  }
}