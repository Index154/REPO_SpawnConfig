using HarmonyLib;
using UnityEngine;
/*
namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(MoonUI))]

public class MoonUIPatch
{
    public static bool insertAfterMoonUI = false;
    public static bool enableAfterMoonUI = false;

    public static void ShowMessages(){
        SemiFunc.UIBigMessage("This IS A TEST HOW LONG can this be also WHAT where does this even appear", "{!}", 25f, Color.white, Color.white);
        SemiFunc.UIFocusText("Eat my nuts and merry christmas ho ho ho happy halloween mothereffers I wonder how long this text can be", Color.white, AssetManager.instance.colorYellow, 10f);
        enableAfterMoonUI = false;
    }

    [HarmonyPatch("Check")]
    [HarmonyPrefix]
    public static void BeforeMoonUI(MoonUI __instance)
    {
        if (SemiFunc.RunIsLevel() && !RunManager.instance.moonLevelChanged) {
            ShowMessages();
            insertAfterMoonUI = false;
        } else {
            insertAfterMoonUI = true;
        }
    }

    [HarmonyPatch("StateNone")]
    [HarmonyPostfix]
    public static void AfterMoonUI(MoonUI __instance)
    {
        if(insertAfterMoonUI && enableAfterMoonUI) ShowMessages();
    }

    [HarmonyPatch("StateStart")]
    [HarmonyPostfix]
    public static void EnableAfterMoonUI(MoonUI __instance)
    {
        enableAfterMoonUI = true;
    }
}
*/