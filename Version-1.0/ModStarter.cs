using Calloatti.Config;
using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace Calloatti.StorageTweaks
{
  public class ModStarter : IModStarter
  {

    // 1. Declare the globally accessible static instance
    public static SimpleConfig Config { get; private set; }
    public void StartMod(IModEnvironment modEnvironment)
    {

      // 2. Instantiate the config. This instantly runs the TXT synchronization.
      Config = new SimpleConfig(modEnvironment.ModPath);

      // This line finds all [HarmonyPatch] attributes in your project and runs them
      new Harmony("calloatti.storagetweaks").PatchAll();

      // This confirms the mod actually loaded in the Player.log
      Debug.Log("[StorageTweaks] Harmony Patches Applied.");
    }
  }
}