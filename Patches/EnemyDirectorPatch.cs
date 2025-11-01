using System.Collections.Generic;
using HarmonyLib;
using SpawnConfig.ExtendedClasses;
using static SpawnConfig.ListManager;
using UnityEngine;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(EnemyDirector))]

public class EnemyDirectorPatch
{

    public static bool setupDone = false;
    public static int currentDifficultyPick = 3;
    public static bool onlyOneSetup = false;

    public static List<string> enemyList = [];
    public static List<string> enemiesSpawned = [];
    public static List<string> enemiesSpawnedToDelete = [];
    public static Dictionary<string, int> enemyCounts = [];
    public static int enemyListIndex = 0;
    public static int enemySpawnCount = 0;

    public static void EnemySpawnSimulation(List<EnemySetup>[] enemiesDifficulties)
    {
        // Simulate vanilla spawns to determine spawn rates
        // The vanilla code for enemy spawning is deranged enough to warrant this
        SpawnConfig.Logger.LogInfo("Consolidated spawn distribution (100 runs, 15 levels each, 3 enemies per tier per level):");
        Dictionary<string, int> enemyCounts = [];
        Dictionary<string, float> enemyRarities = [];
        for (int z = 0; z < 100; z++)
        {
            enemyList.Clear();
            enemiesSpawned.Clear();
            enemiesSpawnedToDelete.Clear();
            enemyListIndex = 0;

            for (int b = 0; b < 15; b++)
            {
                enemiesSpawnedToDelete.Clear();
                foreach (string item in enemiesSpawned)
                {
                    bool flag = false;
                    foreach (string item2 in enemiesSpawnedToDelete)
                    {
                        if (item == item2)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        enemiesSpawnedToDelete.Add(item);
                    }
                }
                foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        string pickedEnemy = PickEnemySimulation(enemiesDifficulty);
                        if (!enemyCounts.ContainsKey(pickedEnemy)) { enemyCounts.Add(pickedEnemy, 1); }
                        else { enemyCounts[pickedEnemy]++; }
                    }
                }
                for (int y = 0; y < 9; y++)
                {
                    string enemySetup = enemyList[enemyListIndex];
                    enemyListIndex++;
                    int num = 0;
                    foreach (string item in enemiesSpawned)
                    {
                        if (item == enemySetup)
                        {
                            num++;
                        }
                    }
                    int num2 = 2;
                    while (num < 4 && num2 > 0)
                    {
                        enemiesSpawned.Add(enemySetup);
                        num++;
                        num2--;
                    }
                }
                foreach (string item in enemiesSpawnedToDelete)
                {
                    enemiesSpawned.Remove(item);
                }
            }
        }
        foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties)
        {
            foreach (EnemySetup _enemies in enemiesDifficulty)
            {
                if (enemyCounts.ContainsKey(_enemies.name))
                {
                    if ((bool)_enemies.rarityPreset) { enemyRarities.Add(_enemies.name, _enemies.rarityPreset.chance); }
                    else { enemyRarities.Add(_enemies.name, 100f); }
                }
            }
        }
        foreach (KeyValuePair<string, int> kvp in enemyCounts)
        {
            SpawnConfig.Logger.LogInfo(kvp.Key + " = " + kvp.Value + " (" + enemyRarities[kvp.Key] + ")");
        }
        foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties)
        {
            foreach (EnemySetup _enemies in enemiesDifficulty)
            {
                if (!enemyCounts.ContainsKey(_enemies.name)) SpawnConfig.Logger.LogInfo(_enemies.name + " = 0 (" + _enemies.rarityPreset.chance + ")");
            }
        }
    }

    public static string PickEnemySimulation(List<EnemySetup> _enemiesList)
    {
        _enemiesList.Shuffle();
        EnemySetup item = null;
        float num2 = -1f;
        foreach (EnemySetup _enemies in _enemiesList)
        {
            int num3 = 0;
            foreach (string item2 in enemiesSpawned)
            {
                if (item2 == _enemies.name)
                {
                    num3++;
                }
            }
            int num4 = 0;
            foreach (string enemy in enemyList)
            {
                if (enemy == _enemies.name)
                {
                    num4++;
                }
            }
            float num5 = 100f;
            if ((bool)_enemies.rarityPreset)
            {
                num5 = _enemies.rarityPreset.chance;
            }
            float maxInclusive = Mathf.Max(1f, num5 - 30f * (float)num3 - 10f * (float)num4);
            float num6 = Random.Range(0f, maxInclusive);
            if (num6 > num2)
            {
                item = _enemies;
                num2 = num6;
            }
        }
        enemyList.Add(item.name);
        return item.name;
    }

    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void SetupAfterBundles(EnemyDirector __instance)
    {
        //REPOLib.BundleLoader.OnAllBundlesLoaded += () =>
        //{
        //    SpawnConfig.Logger.LogInfo("All bundles have been loaded! Running setup...");
        //    SetupOnStart(__instance);
        //};
        SetupOnStart(__instance);
    }

    public static void SetupOnStart(EnemyDirector __instance)
    {
        // Only do it once
        if (!setupDone)
        {
            List<EnemySetup>[] enemiesDifficulties = [__instance.enemiesDifficulty3, __instance.enemiesDifficulty2, __instance.enemiesDifficulty1];

            EnemySpawnSimulation(enemiesDifficulties);

            // Go through existing EnemySetups & the contained spawnObjects and construct extended objects with default values
            int x = 3;
            foreach (List<EnemySetup> enemiesDifficulty in enemiesDifficulties)
            {

                foreach (EnemySetup enemySetup in enemiesDifficulty)
                {
                    // Make list of functional enemy spawnObjects
                    foreach (PrefabRef spawnObject in enemySetup.spawnObjects)
                    {
                        ExtendedSpawnObject extendedObj = new(spawnObject);
                        if (!spawnObjectsDict.ContainsKey(spawnObject.PrefabName))
                        {
                            spawnObjectsDict.Add(spawnObject.PrefabName, spawnObject);
                            //extendedSpawnObjects.Add(extendedObj.name, extendedObj);
                        }
                    }

                    // Make list of extended enemy setups
                    ExtendedEnemySetup extendedSetup = new(enemySetup, x);
                    if (!extendedSetups.ContainsKey(enemySetup.name))
                    {
                        extendedSetups.Add(extendedSetup.name, extendedSetup);
                    }
                }
                x--;
            }

            // Log default spawnObjects
            SpawnConfig.Logger.LogInfo("Found the following enemy spawnObjects:");
            foreach (KeyValuePair<string, PrefabRef> entry in spawnObjectsDict) {
                if (!entry.Key.Contains("Director")) SpawnConfig.Logger.LogInfo(entry.Key);
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
                if (multi2 > 0f) {
                    diff3Count = (int)__instance.amountCurve3_2.Evaluate(multi2);
                    diff2Count = (int)__instance.amountCurve2_2.Evaluate(multi2);
                    diff1Count = (int)__instance.amountCurve1_2.Evaluate(multi2);
                }
                else {
                    diff3Count = (int)__instance.amountCurve3_1.Evaluate(multi1);
                    diff2Count = (int)__instance.amountCurve2_1.Evaluate(multi1);
                    diff1Count = (int)__instance.amountCurve1_1.Evaluate(multi1);
                }
                // Only add values if they have changed compared to the previous
                if (y == 0 || diff3Count != difficulty3Counts[previousIndex] || diff2Count != difficulty2Counts[previousIndex] || diff1Count != difficulty1Counts[previousIndex]) {
                    levelNumbers.Add(y + 1);
                    difficulty3Counts.Add(diff3Count);
                    difficulty2Counts.Add(diff2Count);
                    difficulty1Counts.Add(diff1Count);
                    previousIndex++;
                }
            }
            for (int z = 0; z < difficulty1Counts.Count; z++) {
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
                for (int i = objsToRemove.Count - 1; i > -1; i--)
                {
                    ext.Value.spawnObjects.RemoveAt(objsToRemove[i]);
                }
                // Group is invalid if no objects remain
                if (ext.Value.spawnObjects.Count < 1 && !invalid)
                {
                    invalid = true;
                    SpawnConfig.Logger.LogError("The group \"" + ext.Value.name + "\" contains no valid enemies! This group will be ignored");
                }
                if (invalid) invalidGroups.Add(ext.Key);
            }
            // Remove invalid groups
            foreach (string sp in invalidGroups)
            {
                extendedSetups.Remove(sp);
            }
            setupDone = true;
        }
    }

    [HarmonyPatch("AmountSetup")]
    [HarmonyPrefix]
    public static bool AmountSetupOverride(EnemyDirector __instance)
    {

        if (!SemiFunc.IsMasterClientOrSingleplayer()) return false;

        // Try to run the setup again if it didn't for some unknown reason...
        if (!setupDone)
        {
            SpawnConfig.Logger.LogWarning("Setup function hasn't run yet! Trying again now...");
            SetupOnStart(__instance);
        }

        __instance.enemyListCurrent.Clear();
        __instance.enemyList.Clear();
        enemySpawnCount = 0;
        onlyOneSetup = false;

        // Update enemiesDifficulty lists with customized setups
        // Gotta do it here because it seems that the enemiesDifficulty lists get reset to their default values between Awake() and AmountSetup() - And doing it here is required so we can replace the spawnObjects with empty lists for the duration of one level only
        __instance.enemiesDifficulty1.Clear();
        __instance.enemiesDifficulty2.Clear();
        __instance.enemiesDifficulty3.Clear();
        foreach (KeyValuePair<string, ExtendedEnemySetup> ext in extendedSetups)
        {
            if (ext.Value.difficulty1Weight > 0) __instance.enemiesDifficulty1.Add(ext.Value.GetEnemySetup());
            if (ext.Value.difficulty2Weight > 0) __instance.enemiesDifficulty2.Add(ext.Value.GetEnemySetup());
            if (ext.Value.difficulty3Weight > 0) __instance.enemiesDifficulty3.Add(ext.Value.GetEnemySetup());
        }

        // Fill up with empty objects if required to prevent errors
        EnemySetup emptySetup = ScriptableObject.CreateInstance<EnemySetup>();
        emptySetup.name = "Fallback";
        emptySetup.spawnObjects = [];
        if (__instance.enemiesDifficulty1.Count < 1) __instance.enemiesDifficulty1.Add(emptySetup);
        if (__instance.enemiesDifficulty2.Count < 1) __instance.enemiesDifficulty2.Add(emptySetup);
        if (__instance.enemiesDifficulty3.Count < 1) __instance.enemiesDifficulty3.Add(emptySetup);

        // Prepare stuff
        int groupCount3 = 0;
        int groupCount2 = 0;
        int groupCount1 = 0;
        int currentLevel = RunManager.instance.levelsCompleted + 1;

        // Find the closest level config entry to use (current level or any previous)
        int configKey = 0;
        int x = currentLevel;
        while (configKey == 0)
        {
            if (extendedGroupCounts.ContainsKey(x))
            {
                configKey = x;
                SpawnConfig.Logger.LogInfo("Using group count configs from [Level " + x + "]!");
            }
            else
            {
                SpawnConfig.Logger.LogDebug("Found no config for [Level " + x + "]...");
                x--;
            }
            if (x < 1)
            {
                SpawnConfig.Logger.LogError("No config entry for level 1 found in GroupsPerLevel.json! Enemy spawning will break!");
                return false;
            }
        }

        // Pick a random entry from the list of possibleGroupCounts
        int weightSum = 0;
        foreach (GroupCountEntry groupCountEntry in extendedGroupCounts[configKey].possibleGroupCounts)
        {
            weightSum += groupCountEntry.weight;
        }
        int randRoll = UnityEngine.Random.Range(1, weightSum + 1);
        SpawnConfig.Logger.LogInfo("Selecting group counts based on random number " + randRoll + "...");
        foreach (GroupCountEntry groupCountEntry in extendedGroupCounts[configKey].possibleGroupCounts)
        {

            int weight = groupCountEntry.weight;
            if (groupCountEntry.counts.Count < 3)
            {
                SpawnConfig.Logger.LogError("Found possibleGroupCounts entry with less than 3 numbers in the \"counts\" value! Missing group counts will lead to 0 groups spawning for the corresponding difficulty tier!");
                while (groupCountEntry.counts.Count < 3) { groupCountEntry.counts.Add(0); }
            }
            SpawnConfig.Logger.LogDebug("=> [" + groupCountEntry.counts[0] + "," + groupCountEntry.counts[1] + "," + groupCountEntry.counts[2] + "] = " + weight + " / " + randRoll);

            if (weight >= randRoll)
            {
                groupCount3 = groupCountEntry.counts[2];
                groupCount2 = groupCountEntry.counts[1];
                groupCount1 = groupCountEntry.counts[0];
                if (SpawnConfig.configManager.groupCountMultiplier.Value != 1)
                {
                    SpawnConfig.Logger.LogInfo("Applying global group count multiplier: " + SpawnConfig.configManager.groupCountMultiplier.Value);
                    groupCount3 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                    groupCount2 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                    groupCount1 *= SpawnConfig.configManager.groupCountMultiplier.Value;
                }
                SpawnConfig.Logger.LogInfo("Selected group counts: [" + groupCount1 + "," + groupCount2 + "," + groupCount3 + "]");
                break;
            }
            else
            {
                randRoll -= weight;
            }
        }

        // Pick spawns
        __instance.totalAmount = groupCount1 + groupCount2 + groupCount3;
        for (int i = 0; i < groupCount3; i++)
        {
            PickEnemiesCustom(__instance.enemiesDifficulty3, __instance);
        }
        for (int i = 0; i < groupCount2; i++)
        {
            PickEnemiesCustom(__instance.enemiesDifficulty2, __instance);
        }
        for (int i = 0; i < groupCount1; i++)
        {
            PickEnemiesCustom(__instance.enemiesDifficulty1, __instance);
        }

        SpawnConfig.Logger.LogInfo("Spawned a total of [" + __instance.totalAmount + "] enemy groups");
        return false;
    }

    public static void PickEnemiesCustom(List<EnemySetup> _enemiesList, EnemyDirector __instance)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        if (onlyOneSetup) return;

        if (_enemiesList == __instance.enemiesDifficulty1) currentDifficultyPick = 1;
        if (_enemiesList == __instance.enemiesDifficulty2) currentDifficultyPick = 2;
        if (_enemiesList == __instance.enemiesDifficulty3) currentDifficultyPick = 3;
        SpawnConfig.Logger.LogInfo("Picking difficulty " + currentDifficultyPick + " setup...");
        SpawnConfig.Logger.LogInfo("Enemy group weights:");

        int num = DataDirector.instance.SettingValueFetch(DataDirector.Setting.RunsPlayed);
        List<EnemySetup> possibleEnemies = [];

        // Filter the list before doing the selection because we need to only use the weights of EnemySetups that can actually spawn
        float weightSum = 0.0f;
        foreach (EnemySetup enemy in _enemiesList)
        {
            // Vanilla code
            if ((enemy.levelsCompletedCondition && (RunManager.instance.levelsCompleted < enemy.levelsCompletedMin || RunManager.instance.levelsCompleted > enemy.levelsCompletedMax)) || num < enemy.runsPlayed) {
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

        // Prevent error when there's no selectable setups
        if(possibleEnemies.Count < 1){
            SpawnConfig.Logger.LogError("No selectable enemy groups found! This level will have one less enemy than intended");
            __instance.totalAmount--;
            return;
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
                item = enemy;
                break;
            }
            else {
                randRoll -= weight;
            }
        }

        // Remove all other EnemySetups if only this one should spawn
        if (extendedSetups.ContainsKey(item.name) && extendedSetups[item.name].thisGroupOnly && !onlyOneSetup) {
            List<string> names = [];
            foreach (EnemySetup enemy in __instance.enemyList) {
                names.Add(enemy.name);
            }
            __instance.enemyList.Clear();
            __instance.enemyList.Add(item);
            onlyOneSetup = true;
            __instance.totalAmount = 1;
            SpawnConfig.Logger.LogInfo("This is a solo group! Removing all other spawns...");
        }
        else {
            __instance.enemyList.Add(item);
            // enemyListCurrent does not seem to serve any purpose yet so far so I left it out
        }
    }

    [HarmonyPatch("DebugResult")]
    [HarmonyPrefix]
    public static void Debug()
    {
        SpawnConfig.Logger.LogInfo("Spawned a total of [" + enemySpawnCount + "] enemy objects");
    }
    
}