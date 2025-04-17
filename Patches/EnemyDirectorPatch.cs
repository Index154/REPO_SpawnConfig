using System.Collections.Generic;
using HarmonyLib;
using SpawnConfig.ExtendedClasses;
using static SpawnConfig.ListManager;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;
using SpawnConfig.Managers;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyDirector))]

public class EnemyDirectorPatch {

    public static bool setupDone = false;
    public static int currentDifficultyPick = 3;
    public static bool onlyOneSetup = false;

    public static string PickEnemySimulation(List<EnemySetup> _enemiesList){
		_enemiesList.Shuffle();
		EnemySetup? item = null;
		float num2 = -1f;
		foreach (EnemySetup _enemies in _enemiesList)
		{
			float num4 = 100f;
			if (_enemies != null && _enemies.rarityPreset != null)
			{
				num4 = _enemies.rarityPreset.chance;
			}
			float maxInclusive = Mathf.Max(0f, num4);
			float num5 = Random.Range(0f, maxInclusive);
			if (num5 > num2)
			{
				item = _enemies;
				num2 = num5;
			}
		}
        return item?.name ?? "Unknown";
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void SetupOnStart(EnemyDirector __instance){

        if (!setupDone) {
            SpawnHelper.RegisterEnemyDirector(__instance);
            
            List<EnemySetup>[] enemiesDifficulties = [__instance.enemiesDifficulty3, __instance.enemiesDifficulty2, __instance.enemiesDifficulty1];

            int x = 3;
            foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties){

                foreach (EnemySetup enemySetup in enemiesDifficulty){

                    foreach (GameObject spawnObject in enemySetup.spawnObjects){
                        spawnObject.name = spawnObject.name;
                        ExtendedSpawnObject extendedObj = new(spawnObject);
                        if (!spawnObjectsDict.ContainsKey(spawnObject.name)){
                            spawnObjectsDict.Add(spawnObject.name, spawnObject);
                        }
                    }
                    
                    ExtendedEnemySetup extendedSetup = new(enemySetup, x);
                    if(!extendedSetups.ContainsKey(enemySetup.name)){
                        extendedSetups.Add(extendedSetup.name, extendedSetup);
                    }
                }
                x--;
            }
            
            SpawnConfig.Logger.LogInfo("Found the following enemy spawnObjects:");
            foreach (KeyValuePair<string, GameObject> entry in spawnObjectsDict){
                SpawnConfig.Logger.LogInfo(entry.Key);
            }

            for(float y = 0.0f; y < 1.1f; y+=0.1f){
                difficulty3Counts.Add((int)__instance.amountCurve3.Evaluate(y));
                difficulty2Counts.Add((int)__instance.amountCurve2.Evaluate(y));
                difficulty1Counts.Add((int)__instance.amountCurve1.Evaluate(y));
            }
            for(int z = 0; z < difficulty1Counts.Count; z++){
                groupCountsList.Add(new ExtendedGroupCounts(z));
            }

            SpawnConfig.ReadAndUpdateJSON();

            List<string> invalidGroups = [];
            foreach (KeyValuePair<string, ExtendedEnemySetup> ext in extendedSetups) {
                bool invalid = false;
                int index = 0;
                List<int> objsToRemove = [];
                foreach(string sp in ext.Value.spawnObjects){
                    if(!spawnObjectsDict.ContainsKey(sp)) {
                        if(SpawnConfig.configManager.ignoreInvalidGroups.Value){
                            SpawnConfig.Logger.LogError("Unable to resolve enemy name \"" + sp + "\" in group \"" + ext.Value.name+ "\"! This group will be ignored");
                            invalid = true;
                        }else{
                            SpawnConfig.Logger.LogError("Unable to resolve enemy name \"" + sp + "\" in group \"" + ext.Value.name+ "\"! This enemy will be removed but the group can still spawn");
                            objsToRemove.Add(index);
                        }
                    }
                    index++;
                }
                for(int i = objsToRemove.Count - 1; i > -1; i--){
                    ext.Value.spawnObjects.RemoveAt(objsToRemove[i]);
                }
                if(ext.Value.spawnObjects.Count < 1 && !invalid){
                    invalid = true;
                    SpawnConfig.Logger.LogError("The group \"" + ext.Value.name+ "\" contains no valid enemies! This group will be ignored");
                }
                if(invalid) invalidGroups.Add(ext.Key);
            }
            foreach (string sp in invalidGroups) {
                extendedSetups.Remove(sp);
            }
            setupDone = true;
        }
    }

    [HarmonyPatch("AmountSetup")]
    [HarmonyPrefix]
    public static void AmountSetupOverride(EnemyDirector __instance){

        __instance.enemiesDifficulty1.Clear();
        __instance.enemiesDifficulty2.Clear();
        __instance.enemiesDifficulty3.Clear();
        onlyOneSetup = false;
        foreach (KeyValuePair<string, ExtendedEnemySetup> ext in extendedSetups) {
            if(ext.Value.difficulty1Weight > 0) __instance.enemiesDifficulty1.Add(ext.Value.GetEnemySetup());
            if(ext.Value.difficulty2Weight > 0) __instance.enemiesDifficulty2.Add(ext.Value.GetEnemySetup());
            if(ext.Value.difficulty3Weight > 0) __instance.enemiesDifficulty3.Add(ext.Value.GetEnemySetup());
        }

        EnemySetup emptySetup = ScriptableObject.CreateInstance<EnemySetup>();
        emptySetup.name = "Fallback";
        emptySetup.spawnObjects = [];
        if(__instance.enemiesDifficulty1.Count < 1) __instance.enemiesDifficulty1.Add(emptySetup);
        if(__instance.enemiesDifficulty2.Count < 1) __instance.enemiesDifficulty2.Add(emptySetup);
        if(__instance.enemiesDifficulty3.Count < 1) __instance.enemiesDifficulty3.Add(emptySetup);
    }


    [HarmonyPatch("PickEnemies")]
    [HarmonyPrefix]
    public static bool PickEnemiesOverride(List<EnemySetup> _enemiesList, EnemyDirector __instance){
        if(_enemiesList == __instance.enemiesDifficulty1) currentDifficultyPick = 1;
        if(_enemiesList == __instance.enemiesDifficulty2) currentDifficultyPick = 2;
        if(_enemiesList == __instance.enemiesDifficulty3) currentDifficultyPick = 3;
        SpawnConfig.Logger.LogInfo("Picking difficulty " + currentDifficultyPick + " setup...");
        SpawnConfig.Logger.LogInfo("Enemy group weights:");

        int num = DataDirector.instance.SettingValueFetch(DataDirector.Setting.RunsPlayed);
        List<EnemySetup> possibleEnemies = [];

        float weightSum = 0.0f;
        foreach(EnemySetup enemy in _enemiesList){
            
            if ((enemy.levelsCompletedCondition && (RunManager.instance.levelsCompleted < enemy.levelsCompletedMin || RunManager.instance.levelsCompleted > enemy.levelsCompletedMax)) || num < enemy.runsPlayed)
			{
				continue;
			}

            float weight = 1.0f;
            if (extendedSetups.ContainsKey(enemy.name)) weight = extendedSetups[enemy.name].GetWeight(currentDifficultyPick, __instance.enemyList);
            
            weight *= (float)SpawnConfig.configManager.globalSpawnMultiplier.Value;
            
            if (weight < 1) continue;
            weightSum += weight;

            possibleEnemies.Add(enemy);
            SpawnConfig.Logger.LogInfo(enemy.name + " = " + weight + " (after global multiplier: " + SpawnConfig.configManager.globalSpawnMultiplier.Value + ")");
        }

        if (weightSum <= 0f || possibleEnemies.Count == 0) {
            SpawnConfig.Logger.LogInfo("No enemies can spawn (total weight: " + weightSum + ")");
            return false;
        }

        EnemySetup? item = null;
        float randRoll = UnityEngine.Random.Range(1.0f, weightSum);
        SpawnConfig.Logger.LogInfo("Selecting a group based on random number " + randRoll + "...");
        foreach (EnemySetup enemy in possibleEnemies) {

            float weight = 1.0f;
            if (extendedSetups.ContainsKey(enemy.name)) weight = extendedSetups[enemy.name].GetWeight(currentDifficultyPick, __instance.enemyList);
            SpawnConfig.Logger.LogDebug("=> " + enemy.name + " = " + weight + " / " + randRoll);

            if (weight >= randRoll) {
                SpawnConfig.Logger.LogInfo("Selected: " + enemy.name);
                if(onlyOneSetup){
                    item = ScriptableObject.CreateInstance<EnemySetup>();
                    item.name = enemy.name;
                    item.spawnObjects = [];
                }else{
                    item = enemy;
                }
                break;
            }else{
                randRoll -= weight;
            }
        }
        
        if(item != null && extendedSetups.ContainsKey(item.name) && extendedSetups[item.name].thisGroupOnly && !onlyOneSetup){
            
            List<string> names = [];
            int count = __instance.enemyList.Count;
            foreach(EnemySetup enemy in __instance.enemyList){
                names.Add(enemy.name);
            }
            __instance.enemyList.Clear();
            __instance.enemyList.Add(item);
            onlyOneSetup = true;

            for(int i = 0; i < count; i++){
                EnemySetup item2 = ScriptableObject.CreateInstance<EnemySetup>();
                item2.name = names[i];
                item2.spawnObjects = [];
                __instance.enemyList.Add(item2);
            }

        }else if(item != null){
            __instance.enemyList.Add(item);
        }
        
        return false;
    }
}