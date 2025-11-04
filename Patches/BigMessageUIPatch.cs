using HarmonyLib;
using UnityEngine;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(BigMessageUI))]

public class BigMessageUIPatch
{
    [HarmonyPatch("BigMessage")]
    [HarmonyPostfix]
    public static void ChangeBigMessageTimer(BigMessageUI __instance, string message, string emoji, float size, Color colorMain, Color colorFlash)
    {
        // BigMessageUI.instance.bigMessageTimer = 15f;
    }
}