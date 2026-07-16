using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using UnityEngine;
using Timberborn.BlueprintSystem;
using Timberborn.Stockpiles;
using Timberborn.StockpileVisualization;

namespace Calloatti.StorageTweaks
{
  [HarmonyPatch(typeof(SpecService), "Load")]
  public static class BlueprintPatcher
  {
    private static readonly AccessTools.FieldRef<StockpileGoodPileVisualizerSpec, ImmutableArray<string>> GoodPileVisualizationsRef =
        AccessTools.FieldRefAccess<StockpileGoodPileVisualizerSpec, ImmutableArray<string>>("<GoodPileVisualizations>k__BackingField");

    private static readonly AccessTools.FieldRef<StockpilePlaneVisualizerSpec, ImmutableArray<StockpilePlaneVisualization>> StockpilePlaneVisualizationsRef =
        AccessTools.FieldRefAccess<StockpilePlaneVisualizerSpec, ImmutableArray<StockpilePlaneVisualization>>("<StockpilePlaneVisualizations>k__BackingField");

    [HarmonyPostfix]
    public static void Postfix(SpecService __instance)
    {
      try
      {
        bool useBagsForDirt = false;
        if (ModStarter.Config.HasKey("UseBagsForDirt"))
        {
          useBagsForDirt = ModStarter.Config.GetBool("UseBagsForDirt");
        }

        if (!useBagsForDirt) return;

        var specDict = __instance._cachedBlueprintsBySpecs;
        if (specDict == null || !specDict.TryGetValue(typeof(StockpileSpec), out var lazyList)) return;

        foreach (var lazyObj in lazyList)
        {
          if (lazyObj.Value != null) ConvertDirtPlaneToPileVisualizer(lazyObj.Value);
        }
      }
      catch (Exception ex)
      {
        Debug.LogError($"[StorageTweaks] Error redirecting dirt blueprints: {ex}");
      }
    }

    private static void ConvertDirtPlaneToPileVisualizer(Blueprint blueprint)
    {
      var planeSpec = blueprint.GetSpec<StockpilePlaneVisualizerSpec>();
      if (planeSpec == null) return;

      var planeVisualizations = planeSpec.StockpilePlaneVisualizations;
      bool hasDirt = false;
      var remainingPlaneVisualizations = new List<StockpilePlaneVisualization>();

      foreach (var v in planeVisualizations)
      {
        if (v.GoodVisualizationId == "Dirt") hasDirt = true;
        else remainingPlaneVisualizations.Add(v);
      }

      if (!hasDirt) return;

      // Strip dirt out of the plane visualizer
      StockpilePlaneVisualizationsRef(planeSpec) = remainingPlaneVisualizations.ToImmutableArray();

      // Find or create the pile visualizer
      var pileSpec = blueprint.GetSpec<StockpileGoodPileVisualizerSpec>();
      if (pileSpec == null)
      {
        pileSpec = new StockpileGoodPileVisualizerSpec
        {
          CenterOffset = new Vector3(0.5f, 0.5f, 0.09f),
          GoodPileVisualizations = ImmutableArray.Create<string>()
        };
        AddSpecToBlueprint(blueprint, pileSpec);
      }

      // Inject Dirt into the pile visualizer's whitelist
      var currentGoods = pileSpec.GoodPileVisualizations;
      if (!currentGoods.Contains("Dirt"))
      {
        GoodPileVisualizationsRef(pileSpec) = currentGoods.Add("Dirt");
      }
    }

    private static void AddSpecToBlueprint<TSpec>(Blueprint blueprint, TSpec spec) where TSpec : ComponentSpec
    {
      var specsField = AccessTools.Field(typeof(Blueprint), "_specs")
                       ?? AccessTools.Field(typeof(Blueprint), "specs")
                       ?? AccessTools.Field(typeof(Blueprint), "_componentSpecs")
                       ?? AccessTools.Field(typeof(Blueprint), "componentSpecs");

      if (specsField != null)
      {
        var dict = specsField.GetValue(blueprint);
        if (dict != null)
        {
          var method = dict.GetType().GetMethod("Add") ?? dict.GetType().GetMethod("set_Item");
          if (method != null)
          {
            method.Invoke(dict, new object[] { typeof(TSpec), spec });
          }
        }
      }
    }
  }
}