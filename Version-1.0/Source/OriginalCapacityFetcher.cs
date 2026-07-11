using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Timberborn.BlueprintSystem;

namespace Calloatti.StorageTweaks
{
  public static class OriginalCapacityFetcher
  {
    // Regex to safely extract MaxCapacity despite JSON spacing variations
    private static readonly Regex CapacityRegex = new Regex(@"\""MaxCapacity\""\s*:\s*(\d+)", RegexOptions.Compiled);

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
        var match = CapacityRegex.Match(originalJson);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int capacity))
        {
          return capacity;
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