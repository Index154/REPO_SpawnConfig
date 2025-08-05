/*
using HarmonyLib;
using UnityEngine;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyDuck))]

// Pointless code for duck debugging
// Duck has some special logic going on that seems to override its agent speed while it's in the chasing state

public class EnemyDuckPatch
{
    [HarmonyPatch("StateChaseTowards")]
    [HarmonyPrefix]
    public static bool StateChaseTowards(EnemyDuck __instance)
    {
        SpawnConfig.Logger.LogInfo("Duck is chasing towards!");
        if (__instance.stateImpulse)
        {
            __instance.stateImpulse = false;
        }
        if (__instance.VisionBlocked())
        {
            __instance.UpdateState(EnemyDuck.State.ChaseMoveBack);
            return false;
        }
        __instance.enemy.NavMeshAgent.Disable(0.1f);
        __instance.transform.position = Vector3.MoveTowards(__instance.transform.position, __instance.playerTarget.localCameraPosition + Vector3.down * 0.31f, EnemyNavMeshAgentPatch.speedMultiplier * 5f * Time.deltaTime);
        __instance.ChaseStop();
        return false;
    }
}
*/