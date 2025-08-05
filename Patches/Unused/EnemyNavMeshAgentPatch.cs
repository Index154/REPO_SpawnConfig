/*
using HarmonyLib;
using UnityEngine;

namespace SpawnConfig.Patches;

// Attempt at modifying enemy speed. Kind of works but not 100% always
*/
/*
Notes on speed:
- Speed is overwritten by certain functions. This may be related to the specific enemy's class or the shared enemy state classes. Probably both though
- Velocity is automatically calculated according to the speed
- After an agent override ends, its speed is reverted to its defaultspeed
- Acceleration shouldn't need to be modified
- The defaultspeed of an agent is set to its speed on awake (does it vary by enemy type?)
- Gnomes are assigned a random defaultspeed from a certain range on start. There don't seem to be any other outliers in regards to this value
- Agent override and update calls are often used to set the enemy's speed to its defaultspeed, but sometimes they also assign hard-coded speed values (see duck)

Notes on scale:
- The class for the specific enemy has to be transformed? At least in UnityExplorer resizing the EnemyRunner Clone object (gameObject?) made him smaller. It also changed his hitbox (maybe not perfectly?)
- Resizing a Runner put him slightly in the ground, maybe the feet position has to be shifted up somehow?
- Resizing a Duck causes issues with its attack mode because it seems to go too far off the ground and then fails to path to the player
- Still no clue what to actually transform in the code to change the size
=> Conclusion: Scale adjustments come with many caviats and are too annoying to implement
*/
/*
[HarmonyPatch(typeof(EnemyNavMeshAgent))]

public class EnemyNavMeshAgentPatch
{
    public static float speedMultiplier = 100f;

    [HarmonyPatch("Awake")]
    [HarmonyPostfix]
    public static void Awake(EnemyNavMeshAgent __instance)
    {
        __instance.Agent.speed *= speedMultiplier;
        __instance.DefaultSpeed *= speedMultiplier;
        
        //Vector3 newScale = new Vector3(2f, 2f, 2f);
        //EnemyDuck enemyDuck = __instance.GetComponentInParent<EnemyDuck>();
        //enemyDuck.gameObject.transform.localScale = newScale;
        //EnemyParent enemyParent = enemy.GetComponentInParent<EnemyParent>();
        //enemyParent.transform.localScale = newScale;
        //enemy.transform.localScale = newScale;
        //enemy.gameObject.transform.localScale = newScale;
        //enemy.Rigidbody.transform.localScale = newScale;
    }

    [HarmonyPatch("OverrideAgent")]
    [HarmonyPrefix]
    public static bool OverrideAgent(float speed, float acceleration, float time, EnemyNavMeshAgent __instance)
    {
        __instance.Agent.speed = speed * speedMultiplier;
        __instance.Agent.acceleration = acceleration;
        __instance.OverrideTimer = time;
        return false;
    }

    [HarmonyPatch("UpdateAgent")]
    [HarmonyPrefix]
    public static bool UpdateAgent(float speed, float acceleration, EnemyNavMeshAgent __instance)
    {
        __instance.Agent.speed = speed * speedMultiplier;
        __instance.Agent.acceleration = acceleration;
        return false;
    }
}
*/