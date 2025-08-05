/*using HarmonyLib;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(HurtCollider))]

// Early attempt at modifying enemy damage based on name but seems inconsistent!

public class HurtColliderPatch {

    [HarmonyPatch("OnEnable")]
    [HarmonyPrefix]
    public static void ModifyDamage(HurtCollider __instance){

        if(__instance != null){
            if(__instance.enemyHost != null){
                SpawnConfig.Logger.LogInfo(__instance.enemyHost.EnemyParent.enemyName + " => " + __instance.playerDamage);
                __instance.playerDamage = 0;
                __instance.playerKill = false;
            }
        }

    }
}*/