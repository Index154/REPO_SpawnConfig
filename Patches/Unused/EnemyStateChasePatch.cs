/*using HarmonyLib;
using UnityEngine;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyStateChase))]

// Pointless code

public class EnemyStateChasePatch
{
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static void Update(EnemyStateChase __instance)
    {
        SpawnConfig.Logger.LogInfo("Chase Speed = " + __instance.Speed);
        
        //__instance.Speed *= EnemyNavMeshAgentPatch.speedMultiplier;
    }
}
*/