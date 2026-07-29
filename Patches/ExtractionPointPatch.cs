using HarmonyLib;
using Photon.Pun;
using UnityEngine;
/*
namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(ExtractionPoint))]

public class ExtractionPointPatch
{
    [HarmonyPatch("HaulGoalSet")]
    [HarmonyPrefix]
    public static bool ChangeHaulGoal(ExtractionPoint __instance, ref int value)
    {
        value = 100;
        if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				__instance.photonView.RPC("HaulGoalSetRPC", RpcTarget.All, value);
			}
		}
		else
		{
			__instance.HaulGoalSetRPC(value);
		}
        return false;
    }
}
*/