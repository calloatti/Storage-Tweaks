using System;
using UnityEngine;
using Timberborn.BlueprintSystem;
using Timberborn.BlockSystem; // Needed for BlockObjectSpec

namespace Calloatti.StorageTweaks
{
  public static class VolumeCalculator
  {
    // Vanilla game fixed value: 20 items fit in one block volume
    private const float CapacityPerBlock = 20f;

    public static float Calculate(Blueprint blueprint)
    {
      try
      {
        // 1. Get the Spec from the loaded Blueprint object
        var blockSpec = blueprint.GetSpec<BlockObjectSpec>();
        if (blockSpec == null) return 0f;

        var Volume = blockSpec.Size.x * blockSpec.Size.y * blockSpec.Size.z * CapacityPerBlock;
        return Volume;
      }
      catch (Exception ex)
      {
        Debug.LogError($"[StorageTweaks] Error reading Blueprint object for {blueprint.Name}: {ex.Message}");
      }

      return 0f; // Fallback
    }
  }
}