using System;
using UnityEngine;
using Timberborn.BlueprintSystem;

namespace Calloatti.StorageTweaks
{
  public static class OriginalCapacityFetcher
  {
    // --- 1. SAFE JSON RETRIEVAL ---
    public static string GetRawJson(BlueprintSourceService sourceService, Blueprint blueprint)
    {
      try
      {
        var bundle = sourceService.Get(blueprint);
        if (bundle == null) return null;

        // Native access to publicized array, no reflection needed
        if (bundle.Jsons.Length > 0)
        {
          return bundle.Jsons[0];
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[StorageTweaks] Failed to extract JSON for {blueprint.Name}: {ex.Message}");
      }

      return null;
    }

    // --- 2. ORIGINAL CAPACITY RETRIEVAL ---
    public static int GetOriginalCapacity(Blueprint blueprint, string originalJson)
    {
      if (string.IsNullOrEmpty(originalJson)) return -1;

      try
      {
        // We extract the capacity directly from the JSON string.
        // This eliminates the need to trick the game into re-deserializing the bundle.
        string searchKey = "\"MaxCapacity\":";
        int index = originalJson.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);

        if (index != -1)
        {
          int startIndex = index + searchKey.Length;
          int endIndex = originalJson.IndexOfAny(new char[] { ',', '}' }, startIndex);

          if (endIndex != -1)
          {
            string valueStr = originalJson.Substring(startIndex, endIndex - startIndex).Trim();
            if (int.TryParse(valueStr, out int capacity))
            {
              return capacity;
            }
          }
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[StorageTweaks] Failed to parse JSON capacity for {blueprint.Name}: {ex.Message}");
      }

      return -1;
    }
  }
}