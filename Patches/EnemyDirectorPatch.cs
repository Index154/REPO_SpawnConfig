using System.Collections.Generic;
using HarmonyLib;
using REPOLib.Modules;
using SpawnConfig.ExtendedClasses;
using static SpawnConfig.ListManager;
using UnityEngine;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyDirector))]

public class EnemyDirectorPatch {

    public static bool setupDone = false;
    public static int currentDifficultyPick = 3;
    public static bool onlyOneSetup = false;

    public static string PickEnemySimulation(List<EnemySetup> _enemiesList){
		_enemiesList.Shuffle();
		EnemySetup item = null;
		float num2 = -1f;
		foreach (EnemySetup _enemies in _enemiesList)
		{
			float num5 = 100f;
			if ((bool)_enemies.rarityPreset)
			{
				num5 = _enemies.rarityPreset.chance;
			}
			float maxInclusive = Mathf.Max(0f, num5);
			float num6 = Random.Range(0f, maxInclusive);
			if (num6 > num2)
			{
				item = _enemies;
				num2 = num6;
			}
		}
        return item.name;
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void SetupAfterBundles(EnemyDirector __instance)
    {
        REPOLib.BundleLoader.OnAllBundlesLoaded += () =>
        {
            SetupOnStart(__instance);
        };
    }
    public static void SetupOnStart(EnemyDirector __instance){

        // Only do it once
        if (!setupDone)
        {
            List<EnemySetup>[] enemiesDifficulties = [__instance.enemiesDifficulty3, __instance.enemiesDifficulty2, __instance.enemiesDifficulty1];

            // Go through existing EnemySetups & the contained spawnObjects and construct extended objects with default values
            int x = 3;
            foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties){

                // Simulate a million vanilla spawns per tier to determine spawn rates
                /*
                SpawnConfig.Logger.LogInfo("Difficulty X spawn distribution:");
                Dictionary<string, int> enemyCounts = [];
                for(int y = 0; y < 1000000; y++){
                    string pickedEnemy = PickEnemySimulation(enemiesDifficulty);
                    if(!enemyCounts.ContainsKey(pickedEnemy)){ enemyCounts.Add(pickedEnemy, 1); }
                    else{ enemyCounts[pickedEnemy]++;}
                }
                foreach(KeyValuePair<string, int> kvp in enemyCounts){
                    SpawnConfig.Logger.LogInfo(kvp.Key + " = " + kvp.Value);
                }
                */

                foreach (EnemySetup enemySetup in enemiesDifficulty){

                    // Make list of functional enemy spawnObjects
                    foreach (GameObject spawnObject in enemySetup.spawnObjects)
                    {
                        spawnObject.name = spawnObject.name;
                        ExtendedSpawnObject extendedObj = new(spawnObject);
                        if (!spawnObjectsDict.ContainsKey(spawnObject.name))
                        {
                            spawnObjectsDict.Add(spawnObject.name, spawnObject);
                            //extendedSpawnObjects.Add(extendedObj.name, extendedObj);
                        }
                    }

                    // Make list of extended enemy setups
                    ExtendedEnemySetup extendedSetup = new(enemySetup, x);
                    if (!extendedSetups.ContainsKey(enemySetup.name)){
                        extendedSetups.Add(extendedSetup.name, extendedSetup);
                    }
                }
                x--;
            }

            // Log default spawnObjects
            SpawnConfig.Logger.LogInfo("Found the following enemy spawnObjects:");
            foreach (KeyValuePair<string, GameObject> entry in spawnObjectsDict){
                SpawnConfig.Logger.LogInfo(entry.Key);
            }

            // Get default enemy group counts per level
            /*
            for (float y = 0.0f; y < 1.1f; y += 0.1f){
                difficulty3Counts.Add((int)__instance.amountCurve3.Evaluate(y));
                difficulty2Counts.Add((int)__instance.amountCurve2.Evaluate(y));
                difficulty1Counts.Add((int)__instance.amountCurve1.Evaluate(y));
            }
            for (int z = 0; z < difficulty1Counts.Count; z++){
                groupCountsList.Add(new ExtendedGroupCounts(z));
            }
            */

            // Read / update JSON configs
            SpawnConfig.ReadAndUpdateJSON();

            // Deal with invalid enemy names
            List<string> invalidGroups = [];
            foreach (KeyValuePair<string, ExtendedEnemySetup> ext in extendedSetups)
            {
                bool invalid = false;
                int index = 0;
                List<int> objsToRemove = [];
                foreach (string sp in ext.Value.spawnObjects)
                {
                    if (!spawnObjectsDict.ContainsKey(sp))
                    {
                        if (SpawnConfig.configManager.ignoreInvalidGroups.Value)
                        {
                            SpawnConfig.Logger.LogError("Unable to resolve enemy name \"" + sp + "\" in group \"" + ext.Value.name + "\"! This group will be ignored");
                            invalid = true;
                        }
                        else
                        {
                            SpawnConfig.Logger.LogError("Unable to resolve enemy name \"" + sp + "\" in group \"" + ext.Value.name + "\"! This enemy will be removed but the group can still spawn");
                            objsToRemove.Add(index);
                        }
                    }
                    index++;
                }
                // Remove invalid objects from group (from highest to lowest index)
                for (int i = objsToRemove.Count - 1; i > -1; i--){
                    ext.Value.spawnObjects.RemoveAt(objsToRemove[i]);
                }
                // Group is invalid if no objects remain
                if (ext.Value.spawnObjects.Count < 1 && !invalid){
                    invalid = true;
                    SpawnConfig.Logger.LogError("The group \"" + ext.Value.name + "\" contains no valid enemies! This group will be ignored");
                }
                if (invalid) invalidGroups.Add(ext.Key);
            }
            // Remove invalid groups
            foreach (string sp in invalidGroups){
                extendedSetups.Remove(sp);
            }
            setupDone = true;
        }
    }

    [HarmonyPatch("AmountSetup")]
    [HarmonyPrefix]
    public static void AmountSetupOverride(EnemyDirector __instance){

        // WIP
        // Clear default animation curves
        // Modifying the curves in Start() does not work as they are reset afterwards at some unknown point in time
        /*
        __instance.amountCurve3 = new AnimationCurve();
        __instance.amountCurve2 = new AnimationCurve();
        __instance.amountCurve1 = new AnimationCurve();

        // Fill curves using custom config
        Dictionary<int, ExtendedGroupCounts> groupCountsDict = groupCountsList.ToDictionary(obj => obj.level);
        int highest = 11;
        int previousDiff3Value = 0;
        int previousDiff2Value = 0;
        int previousDiff1Value = 0;
        for(int i = 1; i < highest; i++){

            // Get values from config if they exist (default to previous level's values if nothing is found)
            // Saving a key for every increment of 0.1 because I don't know if the AnimationCurve does some sort of gradual change between key values instead of keeping the previous value constant until changed
            float index = (i - 1) / 10;
            int diff3Value = previousDiff3Value;
            int diff2Value = previousDiff2Value;
            int diff1Value = previousDiff1Value;
            if(groupCountsDict.ContainsKey(i)){
                // Pick random list from the object and use its values if it's large enough
                ExtendedGroupCounts egc = groupCountsDict[i];
                List<int> groupCounts = groupCountsDict[i].possibleGroupCounts[UnityEngine.Random.Range(0, egc.possibleGroupCounts.Count)];
                if(groupCounts.Count < 3){
                    // Skip the current list with error
                    SpawnConfig.Logger.LogError("Group counts array [" + string.Join(",", groupCounts) + "] must contain 3 elements! The custom config for level " + egc.level + " will be ignored! The previous level's group counts will be used instead");
                }else{
                    diff3Value = groupCounts[2];
                    diff2Value = groupCounts[1];
                    diff1Value = groupCounts[0];
                }
            }
            
            __instance.amountCurve3.AddKey(new Keyframe(index, diff3Value));
            __instance.amountCurve2.AddKey(new Keyframe(index, diff2Value));
            __instance.amountCurve1.AddKey(new Keyframe(index, diff1Value));
            previousDiff3Value = diff3Value;
            previousDiff2Value = diff2Value;
            previousDiff1Value = diff1Value;
        }

        // Log animation curves for debugging
        SpawnConfig.Logger.LogInfo("AnimationCurves:");
        for(float x = 0.0f; x < highest; x += 0.1f){
            SpawnConfig.Logger.LogInfo(x + " (diff 3) = " + __instance.amountCurve3.Evaluate(x));
            //SpawnConfig.Logger.LogInfo(x + " (diff 2) = " + __instance.amountCurve2.Evaluate(x));
            //SpawnConfig.Logger.LogInfo(x + " (diff 1) = " + __instance.amountCurve1.Evaluate(x));
        }
        */

        // Update enemiesDifficulty lists with customized setups
        // Gotta do it here because it seems that the enemiesDifficulty lists get reset to their default values between Awake() and AmountSetup() - And doing it here is required so we can replace the spawnObjects with empty lists for the duration of one level only
        __instance.enemiesDifficulty1.Clear();
        __instance.enemiesDifficulty2.Clear();
        __instance.enemiesDifficulty3.Clear();
        onlyOneSetup = false;
        foreach (KeyValuePair<string, ExtendedEnemySetup> ext in extendedSetups) {
            if(ext.Value.difficulty1Weight > 0) __instance.enemiesDifficulty1.Add(ext.Value.GetEnemySetup());
            if(ext.Value.difficulty2Weight > 0) __instance.enemiesDifficulty2.Add(ext.Value.GetEnemySetup());
            if(ext.Value.difficulty3Weight > 0) __instance.enemiesDifficulty3.Add(ext.Value.GetEnemySetup());
        }

        // Fill up with empty objects if required to prevent errors
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

        // Filter the list before doing the selection because we need to only use the weights of EnemySetups that can actually spawn
        float weightSum = 0.0f;
        foreach(EnemySetup enemy in _enemiesList){
            
            // Vanilla code
            if ((enemy.levelsCompletedCondition && (RunManager.instance.levelsCompleted < enemy.levelsCompletedMin || RunManager.instance.levelsCompleted > enemy.levelsCompletedMax)) || num < enemy.runsPlayed)
			{
				continue;
			}

            // Weight logic
            float weight = 1.0f;
            if (extendedSetups.ContainsKey(enemy.name)) weight = extendedSetups[enemy.name].GetWeight(currentDifficultyPick, __instance.enemyList);
            if (weight < 1) continue;
            weightSum += weight;

            possibleEnemies.Add(enemy);
            SpawnConfig.Logger.LogInfo(enemy.name + " = " + weight);
        }

        // Pick EnemySetup
        EnemySetup item = null;
        float randRoll = UnityEngine.Random.Range(1.0f, weightSum);
        SpawnConfig.Logger.LogInfo("Selecting a group based on random number " + randRoll + "...");
        foreach (EnemySetup enemy in possibleEnemies) {

            float weight = 1.0f;
            if (extendedSetups.ContainsKey(enemy.name)) weight = extendedSetups[enemy.name].GetWeight(currentDifficultyPick, __instance.enemyList);
            SpawnConfig.Logger.LogDebug("=> " + enemy.name + " = " + weight + " / " + randRoll);

            if (weight >= randRoll) {
                SpawnConfig.Logger.LogInfo("Selected: " + enemy.name);
                if(onlyOneSetup){
                    // Replace with empty dummy setup if a thisGroupOnly setup has been selected already
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

        // Replace all other EnemySetups with empty objects if only this one should spawn
        if (extendedSetups.ContainsKey(item.name) && extendedSetups[item.name].thisGroupOnly && !onlyOneSetup)
        {
            List<string> names = [];
            int count = __instance.enemyList.Count;
            foreach (EnemySetup enemy in __instance.enemyList)
            {
                names.Add(enemy.name);
            }
            __instance.enemyList.Clear();
            __instance.enemyList.Add(item);
            onlyOneSetup = true;

            for (int i = 0; i < count; i++)
            {
                EnemySetup item2 = ScriptableObject.CreateInstance<EnemySetup>();
                item2.name = names[i];
                item2.spawnObjects = [];
                __instance.enemyList.Add(item2);
            }
        }
        else{
            __instance.enemyList.Add(item);
            // enemyListCurrent does not seem to serve any purpose yet so far so I left it out
        }
        
        return false;
    }

}