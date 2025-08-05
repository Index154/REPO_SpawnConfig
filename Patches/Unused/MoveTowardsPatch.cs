/*
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Photon.Pun;

namespace SpawnConfig.Patches;

// Funky stuff for patching Vector3.MoveTowards calls in various different classes
// I spent way too many hours on this
// Not sure if it works fully...

[HarmonyPatch]

public class MoveTowardsPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        // Enemy
        yield return AccessTools.FirstMethod(typeof(Enemy), method => method.Name.Contains("Update"));
        // Bang
        yield return AccessTools.FirstMethod(typeof(EnemyBang), method => method.Name.Contains("StateMoveOver"));   // HAS TWO
        // Duck
        yield return AccessTools.FirstMethod(typeof(EnemyDuck), method => method.Name.Contains("StateGoToPlayerOver")); // HAS TWO
        yield return AccessTools.FirstMethod(typeof(EnemyDuck), method => method.Name.Contains("StateFlyBackToNavmesh"));
        yield return AccessTools.FirstMethod(typeof(EnemyDuck), method => method.Name.Contains("StateTransform"));
        yield return AccessTools.FirstMethod(typeof(EnemyDuck), method => method.Name.Contains("StateChaseTowards"));
        yield return AccessTools.FirstMethod(typeof(EnemyDuck), method => method.Name.Contains("StateChaseMoveBack"));
        // Gnome
        yield return AccessTools.FirstMethod(typeof(EnemyGnome), method => method.Name.Contains("StateMoveOver"));  // HAS TWO
        // Hidden
        yield return AccessTools.FirstMethod(typeof(EnemyHidden), method => method.Name.Contains("StatePlayerGoTo"));
        // SlowMouth
        yield return AccessTools.FirstMethod(typeof(EnemySlowMouth), method => method.Name.Contains("StateGoToPlayerOver"));
        // Tumbler
        yield return AccessTools.FirstMethod(typeof(EnemyTumbler), method => method.Name.Contains("HopLogic"));
        // Upscream
        yield return AccessTools.FirstMethod(typeof(EnemyUpscream), method => method.Name.Contains("StateGoToPlayer"));

    }

    public static float MoveTowardsPatch()
    {
        return EnemyNavMeshAgentPatch.speedMultiplier;
    }

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
    {
        var code = new List<CodeInstruction>(instructions);

        var instructionsToInsert = new List<CodeInstruction>();
        instructionsToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Vector3Patch), nameof(MoveTowardsPatch))));
        instructionsToInsert.Add(new CodeInstruction(OpCodes.Mul));

        List<int> insertionIndices = [];
        for (int i = 0; i < code.Count - 1; i++) // -1 since we check i + 1 as well (just kidding, but also the final instruction will never be a Call so no need to check it)
        {
            if (code[i].opcode == OpCodes.Call && code[i].operand.ToString().Contains("UnityEngine.Vector3 MoveTowards("))
            // && code[i + 1].opcode == OpCodes.Callvirt && code[i + 1].operand.ToString().Equals("Void set_position(UnityEngine.Vector3)") 
            {
                insertionIndices.Add(i + (instructionsToInsert.Count * insertionIndices.Count));
            }
        }

        if (insertionIndices.Count > 0)
        {
            SpawnConfig.Logger.LogInfo("--------------> Found [" + insertionIndices.Count + "] Transpiler instruction matches!");
        }
        foreach (int x in insertionIndices)
        {
            code.InsertRange(x, instructionsToInsert);
        }
        return code;
    }
}
*/