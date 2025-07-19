using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using static SpawnConfig.ListManager;

namespace SpawnConfig.Patches;

[HarmonyPatch(typeof(LevelGenerator))]

public class LevelGeneratorPatch {

    public static int PickNonDirector(EnemySetup enemySetup){
        int pickedIndex = -1;
        int min = 0;
        int max = enemySetup.spawnObjects.Count;    // Exlusive!
        while(pickedIndex == -1){
            int index = UnityEngine.Random.Range(min, max);
            if(!enemySetup.spawnObjects[index].name.Contains("Director")) {
                pickedIndex = index;
            }else{
                if(min == max) pickedIndex = min;
                else if(index == min) min++;
                else if(index == max) max--;
            }
        }
        return pickedIndex;
    }

    [HarmonyPatch("EnemySpawn")]
    [HarmonyPrefix]
    public static void LogAndModifySpawns(EnemySetup enemySetup, Vector3 position, LevelGenerator __instance){

        // Re-add missing Director enemies
        bool gnomeCheck = false;
        bool bangCheck = false;
        int directorCount = 0;
        foreach (GameObject spawnObject in enemySetup.spawnObjects){
            if(spawnObject.name.Contains("Gnome") && !gnomeCheck){
                gnomeCheck = true;
                directorCount++;
            }else if(spawnObject.name.Contains("Bang") && !bangCheck){
                bangCheck = true;
                directorCount++;
            }
        }
        if(gnomeCheck) enemySetup.spawnObjects.Insert(0, spawnObjectsDict["Enemy - Gnome Director"]);
        if(bangCheck) enemySetup.spawnObjects.Insert(0, spawnObjectsDict["Enemy - Bang Director"]);

        // Modify the amount of enemies randomly
        if(extendedSetups.ContainsKey(enemySetup.name)){
            int max = (int)Math.Round(1 / extendedSetups[enemySetup.name].alterAmountChance);
            if(max < 1) max = 1;
            int randRoll = UnityEngine.Random.Range(1, max + 1);
            if(randRoll <= 1 && enemySetup.spawnObjects.Count > 0){
                
                // Randomly pick spawnObjects to add or remove
                int amountChange = UnityEngine.Random.Range(extendedSetups[enemySetup.name].alterAmountMin, extendedSetups[enemySetup.name].alterAmountMax + 1);
                if(amountChange > 0){
                    while(amountChange > 0){
                        enemySetup.spawnObjects.Add(enemySetup.spawnObjects[PickNonDirector(enemySetup)]);
                        amountChange--;
                    }
                }else if(amountChange < 0){
                    while(amountChange < 0 && enemySetup.spawnObjects.Count > 0){
                        enemySetup.spawnObjects.RemoveAt(PickNonDirector(enemySetup));
                        amountChange++;
                    }
                }
            }
        }

        // Logging
        Dictionary<string, int> spawnObjects = new();
        foreach (GameObject spawnObject in enemySetup.spawnObjects){
            if(spawnObjects.ContainsKey(spawnObject.name)){
                spawnObjects[spawnObject.name] = spawnObjects[spawnObject.name] + 1;
            }else{
                spawnObjects.Add(spawnObject.name, 1);
            }
        }
        string logString = "";
        foreach (KeyValuePair<string, int> obj in spawnObjects){
            if (!obj.Key.Contains("Director")){
                if (logString != "") logString += ", ";
                logString += obj.Key + " x " + obj.Value;
            }
        }
        if(logString == "") logString = "No spawns or they were prevented for this level";

        SpawnConfig.Logger.LogInfo("Attempting to spawn: [" + enemySetup.name + "]   (" + logString.Replace("Enemy - ", "") + ")");
        if(SpawnConfig.configManager.preventSpawns.Value){
            // "Safe" way of doing it without having to skip the original PickEnemies logic
            enemySetup.spawnObjects.Clear();
            SpawnConfig.Logger.LogInfo("Forcibly prevented all spawns!");
            return;
        }
    }

}