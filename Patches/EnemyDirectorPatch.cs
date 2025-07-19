using System.Collections.Generic;
using HarmonyLib;
using REPOLib.Modules;
using SpawnConfig.ExtendedClasses;
using static SpawnConfig.ListManager;
using UnityEngine;
using System.Linq;

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
                if(!entry.Key.Contains("Director")) SpawnConfig.Logger.LogInfo(entry.Key);
            }

            // Get default enemy group counts per level
            int previousIndex = -1;
            for (int y = 0; y < 21; y += 1)
            {
                float multi1 = Mathf.Clamp01((float)y / 9f);
                float multi2 = Mathf.Clamp01((float)(y - 9) / 10f);

                int diff3Count;
                int diff2Count;
                int diff1Count;
                if (multi2 > 0f)
                {
                    diff3Count = (int)__instance.amountCurve3_2.Evaluate(multi2);
                    diff2Count = (int)__instance.amountCurve2_2.Evaluate(multi2);
                    diff1Count = (int)__instance.amountCurve1_2.Evaluate(multi2);
                }
                else
                {
                    diff3Count = (int)__instance.amountCurve3_1.Evaluate(multi1);
                    diff2Count = (int)__instance.amountCurve2_1.Evaluate(multi1);
                    diff1Count = (int)__instance.amountCurve1_1.Evaluate(multi1);
                }
                // Only add values if they have changed compared to the previous
                if (y == 0 || diff3Count != difficulty3Counts[previousIndex] || diff2Count != difficulty2Counts[previousIndex] || diff1Count != difficulty1Counts[previousIndex])
                {
                    levelNumbers.Add(y + 1);
                    difficulty3Counts.Add(diff3Count);
                    difficulty2Counts.Add(diff2Count);
                    difficulty1Counts.Add(diff1Count);
                    previousIndex++;
                }
            }
            for (int z = 0; z < difficulty1Counts.Count; z++){
                ExtendedGroupCounts extendedGroupCount = new ExtendedGroupCounts(z);
                extendedGroupCounts.Add(extendedGroupCount.level, extendedGroupCount);
            }

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
    public static bool AmountSetupOverride(EnemyDirector __instance){

        __instance.enemyListCurrent.Clear();
        __instance.enemyList.Clear();

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

        // Prepare stuff
        int groupCount3 = 0;
        int groupCount2 = 0;
        int groupCount1 = 0;
        int currentLevel = RunManager.instance.levelsCompleted + 1;

        // Find the closest level config entry to use (current level or any previous)
        int configKey = 0;
        int x = currentLevel;
        while(configKey == 0){
            if(extendedGroupCounts.ContainsKey(x)){
                configKey = x;
                SpawnConfig.Logger.LogInfo("Using group count configs from [Level " + x + "]!");
            }else{
                x--;
            }
            if(x < 1){
                SpawnConfig.Logger.LogError("No config entry for level 1 group counts found! Enemy spawning will break");
                return false;
            }
        }

        // Pick a random entry from the list of possibleGroupCounts
        int weightSum = 0;
        foreach(GroupCountEntry groupCountEntry in extendedGroupCounts[configKey].possibleGroupCounts){
            weightSum += groupCountEntry.weight;
        }
        int randRoll = UnityEngine.Random.Range(1, weightSum + 1);
        SpawnConfig.Logger.LogInfo("Selecting group counts based on random number " + randRoll + "...");
        foreach (GroupCountEntry groupCountEntry in extendedGroupCounts[configKey].possibleGroupCounts) {

            int weight = groupCountEntry.weight;
            if (groupCountEntry.counts.Count < 3) {
                SpawnConfig.Logger.LogError("Found possibleGroupCounts entry with less than 3 numbers in the \"counts\" value! Missing group counts will lead to 0 groups spawning for the corresponding difficulty tier!");
                while (groupCountEntry.counts.Count < 3) { groupCountEntry.counts.Add(0); }
            }
            SpawnConfig.Logger.LogDebug("=> [" + groupCountEntry.counts[0] + "," + groupCountEntry.counts[1] + "," + groupCountEntry.counts[2] + "] = " + weight + " / " + randRoll);

            if (weight >= randRoll)
            {
                groupCount3 = groupCountEntry.counts[2];
                groupCount2 = groupCountEntry.counts[1];
                groupCount1 = groupCountEntry.counts[0];
                if (SpawnConfig.configManager.groupCountMultiplier.Value != 1){
                    SpawnConfig.Logger.LogInfo("Applying global group count multiplier: " + SpawnConfig.configManager.groupCountMultiplier.Value);
                    groupCount3 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                    groupCount2 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                    groupCount1 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                }
                SpawnConfig.Logger.LogInfo("Selected group counts: [" + groupCount1 + "," + groupCount2 + "," + groupCount3 + "]");
                break;
            }
            else{
                randRoll -= weight;
            }
        }
        
        // Pick spawns
        for (int i = 0; i < groupCount3; i++){
            __instance.PickEnemies(__instance.enemiesDifficulty3);
        }
        for(int i = 0; i < groupCount2; i++){
            __instance.PickEnemies(__instance.enemiesDifficulty2);
        }
        for(int i = 0; i < groupCount1; i++){
            __instance.PickEnemies(__instance.enemiesDifficulty1);
        }

        // Vanilla AmountSetup code
        if (SemiFunc.RunGetDifficultyMultiplier3() > 0f)
        {
            __instance.despawnedTimeMultiplier = __instance.despawnTimeCurve_2.Evaluate(SemiFunc.RunGetDifficultyMultiplier3());
        }
        else if (SemiFunc.RunGetDifficultyMultiplier2() > 0f)
        {
            __instance.despawnedTimeMultiplier = __instance.despawnTimeCurve_1.Evaluate(SemiFunc.RunGetDifficultyMultiplier2());
        }
        else
        {
            __instance.despawnedTimeMultiplier = 1f;
        }
        __instance.totalAmount = groupCount1 + groupCount2 + groupCount3;

        return false;
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