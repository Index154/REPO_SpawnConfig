/*using HarmonyLib;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyHealth))]

// Early attempt at modifying enemy health based on name. Seems pretty consistent!
// But currently unimplemented and not very useful for the mod. Just thought I'd keep it in case I need it later

public class EnemyHealthPatch {

    [HarmonyPatch("OnSpawn")]
    [HarmonyPostfix]
    public static void ModifyHealth(EnemyHealth __instance){

        SpawnConfig.Logger.LogInfo(__instance.enemy.EnemyParent.enemyName + " => HP = " + __instance.healthCurrent);
        __instance.healthCurrent = 1;

    }

}*/