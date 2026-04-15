using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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

    // Cache the backing field once so we aren't calling GetField in loops.
    // This is required because MaxCapacity is an { get; init; } property on a record.
    public static readonly FieldInfo MaxCapacityField = typeof(StockpileSpec).GetField("<MaxCapacity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

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
        bool fileModified = false;
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
            ModStarter.Config.SetComment(blueprint.Name, $"Default value: {defaultCap}");
            fileModified = true;
          }

          Debug.Log($"[StorageTweaks] {blueprint.Name} | Default: {defaultCap} | Modded: {moddedCap} | Limit: {visualLimit}");

          // 3. Apply modded capacity if it differs from the default
          if (moddedCap != defaultCap)
          {
            MaxCapacityField?.SetValue(blueprint.GetSpec<StockpileSpec>(), moddedCap);
          }
        }

        if (fileModified)
        {
          Debug.Log("[StorageTweaks] Saving dynamic keys via SimpleConfig...");
          ModStarter.Config.Save();
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[StorageTweaks] Error in ProcessConfig: {ex}");
      }
    }
  }
}