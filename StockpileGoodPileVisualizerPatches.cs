using HarmonyLib;
using UnityEngine;
using Timberborn.Stockpiles;
using Timberborn.TemplateSystem;
using Timberborn.StockpileVisualization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Calloatti.StorageTweaks
{
  public static class VisualizerCache
  {
    public class RatioWrapper
    {
      public float Ratio;
    }

    public static ConditionalWeakTable<StockpileGoodPileVisualizer, RatioWrapper> Ratios = new ConditionalWeakTable<StockpileGoodPileVisualizer, RatioWrapper>();
  }

  [HarmonyPatch]
  public static class StockpileGoodPileVisualizerPatches
  {
    public static bool EnableVisualScaling = true;

    private static int _tempCapacity = -1;

    // --- 1. MEMORY OPTIMIZATION PATCH ---
    [HarmonyPatch(typeof(GoodPileVariantsService), "LoadVisualizerVariants")]
    [HarmonyPrefix]
    public static void Prefix(StockpileGoodPileVisualizerSpec visualizer)
    {
      var templateSpec = visualizer.GetSpec<TemplateSpec>();

      if (templateSpec != null && StorageCapacityPatcher.VisualLimits.TryGetValue(templateSpec.TemplateName, out int visualLimit))
      {
        var stockpileSpec = visualizer.GetSpec<StockpileSpec>();
        // Only throttle the mesh generation if the capacity exceeds the physical visual bounds
        if (stockpileSpec != null && stockpileSpec.MaxCapacity > visualLimit)
        {
          _tempCapacity = stockpileSpec.MaxCapacity;
          var field = typeof(StockpileSpec).GetField("<MaxCapacity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
          field?.SetValue(stockpileSpec, visualLimit);
        }
      }
    }

    [HarmonyPatch(typeof(GoodPileVariantsService), "LoadVisualizerVariants")]
    [HarmonyPostfix]
    public static void Postfix(StockpileGoodPileVisualizerSpec visualizer)
    {
      if (_tempCapacity != -1)
      {
        var stockpileSpec = visualizer.GetSpec<StockpileSpec>();
        if (stockpileSpec != null)
        {
          var field = typeof(StockpileSpec).GetField("<MaxCapacity>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
          field?.SetValue(stockpileSpec, _tempCapacity);
        }
        _tempCapacity = -1;
      }
    }

    // --- 2. RELIABLE SCALING INITIALIZATION ---
    [HarmonyPatch(typeof(StockpileGoodPileVisualizer), "Initialize")]
    [HarmonyPostfix]
    public static void InitializePostfix(StockpileGoodPileVisualizer __instance, int capacity)
    {
      var template = __instance.GetComponent<TemplateSpec>();
      if (template != null && StorageCapacityPatcher.VisualLimits.TryGetValue(template.TemplateName, out int visualLimit))
      {
        if (capacity > 0)
        {
          var wrapper = VisualizerCache.Ratios.GetOrCreateValue(__instance);
          // Scale down if capacity > visual limit. Otherwise, use a 1:1 ratio.
          wrapper.Ratio = capacity > visualLimit ? ((float)visualLimit / capacity) : 1f;
        }
      }
    }

    // --- 3. APPLY VISUAL SCALING ---
    [HarmonyPatch(typeof(StockpileGoodPileVisualizer), "UpdateAmount")]
    [HarmonyPrefix]
    public static bool UpdateAmountPrefix(StockpileGoodPileVisualizer __instance, ref int amountInStock)
    {
      if (!EnableVisualScaling || __instance == null) return true;

      if (VisualizerCache.Ratios.TryGetValue(__instance, out VisualizerCache.RatioWrapper wrapper))
      {
        amountInStock = Mathf.RoundToInt(amountInStock * wrapper.Ratio);
      }

      return true;
    }

    [HarmonyPatch(typeof(StockpileGoodPileVisualizer), "Clear")]
    [HarmonyPostfix]
    public static void ClearPostfix(StockpileGoodPileVisualizer __instance)
    {
      if (__instance != null)
      {
        VisualizerCache.Ratios.Remove(__instance);
      }
    }
  }
}