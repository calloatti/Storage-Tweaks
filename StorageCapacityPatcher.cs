using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Timberborn.Stockpiles;
using Timberborn.BlueprintSystem;

namespace Calloatti.StorageTweaks
{
  [HarmonyPatch("Timberborn.BlueprintSystem.SpecService", "Load")]
  public static class StorageCapacityPatcher
  {
    private static bool DebugMode = false;

    // Store the true calculated visual limits based on building size
    public static readonly Dictionary<string, int> VisualLimits = new Dictionary<string, int>();

    [HarmonyPostfix]
    public static void Postfix(object __instance)
    {
      Debug.Log("[StorageTweaks] SpecService.Load Postfix started. Applying capacities...");
      ProcessConfig(__instance);
      Debug.Log("[StorageTweaks] Finished applying storage capacities.");
    }

    private static void ProcessConfig(object specService)
    {
      try
      {
        bool fileModified = false;
        VisualLimits.Clear();

        var type = specService.GetType();
        var sourceService = type.GetField("_blueprintSourceService", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(specService) as BlueprintSourceService;
        var deserializer = type.GetField("_blueprintDeserializer", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(specService) as BlueprintDeserializer;
        var specDict = type.GetField("_cachedBlueprintsBySpecs", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(specService) as IDictionary;

        if (specDict == null || deserializer == null || sourceService == null) return;

        foreach (DictionaryEntry entry in specDict)
        {
          if ((Type)entry.Key != typeof(StockpileSpec)) continue;

          var lazyList = entry.Value as IList;
          if (lazyList == null) continue;

          foreach (object lazyObj in lazyList)
          {
            var blueprint = lazyObj.GetType().GetProperty("Value")?.GetValue(lazyObj) as Blueprint;
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
            int defaultCap = OriginalCapacityFetcher.GetOriginalCapacity(deserializer, blueprint, rawJson);
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

            if (DebugMode) Debug.Log($"[StorageTweaks] Processed {blueprint.Name} | Default: {defaultCap} | Modded: {moddedCap} | Limit: {visualLimit}");

            // 3. Apply modded capacity if it differs from the default
            if (moddedCap != defaultCap)
            {
              var field = typeof(StockpileSpec).GetField("<MaxCapacity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
              field?.SetValue(blueprint.GetSpec<StockpileSpec>(), moddedCap);
            }
          }
        }

        if (fileModified)
        {
          if (DebugMode) Debug.Log("[StorageTweaks] Saving dynamic keys via SimpleConfig...");
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