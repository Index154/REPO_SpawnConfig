using System.Collections.Generic;
using UnityEngine;
using static SpawnConfig.ListManager;
using SpawnConfig.ExtendedClasses;

namespace SpawnConfig.Managers {
    public class SpawnHelper {
        public static EnemyDirector? currentEnemyDirector;
        private static float lastFindAttemptTime = 0f;
        private static float findCooldown = 2f;
        
        public static void RegisterEnemyDirector(EnemyDirector director) {
            currentEnemyDirector = director;
            SpawnConfig.Logger.LogInfo("EnemyDirector registered for manual spawning");
        }
        
        public static bool TryFindEnemyDirector() {
            if (Time.time - lastFindAttemptTime < findCooldown) {
                return currentEnemyDirector != null;
            }
            
            lastFindAttemptTime = Time.time;
            
            if (currentEnemyDirector != null) {
                if (currentEnemyDirector.gameObject != null && currentEnemyDirector.gameObject.activeInHierarchy) {
                    return true;
                } else {
                    currentEnemyDirector = null;
                }
            }
            
            EnemyDirector[] directors = GameObject.FindObjectsOfType<EnemyDirector>();
            if (directors != null && directors.Length > 0) {
                currentEnemyDirector = directors[0];
                SpawnConfig.Logger.LogInfo($"Found EnemyDirector in scene: {currentEnemyDirector.gameObject.name}");
                return true;
            }
            
            GameObject directorObj = GameObject.Find("EnemyDirector");
            if (directorObj != null) {
                currentEnemyDirector = directorObj.GetComponent<EnemyDirector>();
                if (currentEnemyDirector != null) {
                    SpawnConfig.Logger.LogInfo("Found EnemyDirector by name search");
                    return true;
                }
            }
            
            SpawnConfig.Logger.LogWarning("No active EnemyDirector found in scene");
            return false;
        }
        
        public static bool SpawnEnemyGroup(string groupName) {
            if (currentEnemyDirector == null && !TryFindEnemyDirector()) {
                SpawnConfig.Logger.LogError("No EnemyDirector found, can't spawn enemies");
                return false;
            }
            
            if (!extendedSetups.ContainsKey(groupName)) {
                SpawnConfig.Logger.LogError($"Enemy group '{groupName}' not found");
                return false;
            }
            
            try {
                ExtendedEnemySetup setup = extendedSetups[groupName];
                EnemySetup enemySetup = setup.GetEnemySetup();
                
                if (!currentEnemyDirector.enemyList.Contains(enemySetup)) {
                    currentEnemyDirector.enemyList.Add(enemySetup);
                }
                
                Vector3 spawnPosition = GetPlayerPosition() + new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                
                bool spawned = false;
                
                try {
                    currentEnemyDirector.PickEnemies(currentEnemyDirector.enemiesDifficulty3);
                    spawned = true;
                } catch (System.Exception e) {
                    SpawnConfig.Logger.LogWarning($"Method 1 failed: {e.Message}, trying alternative methods");
                }
                
                if (!spawned) {
                    try {
                        var spawnMethod = currentEnemyDirector.GetType().GetMethod("SpawnEnemy", 
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                        
                        if (spawnMethod != null) {
                            spawnMethod.Invoke(currentEnemyDirector, new object[] { spawnPosition, enemySetup });
                            spawned = true;
                        }
                    } catch (System.Exception e) {
                        SpawnConfig.Logger.LogWarning($"Method 2 failed: {e.Message}");
                    }
                }
                
                if (!spawned && enemySetup.spawnObjects.Count > 0) {
                    try {
                        foreach (GameObject prefab in enemySetup.spawnObjects) {
                            GameObject.Instantiate(prefab, spawnPosition, Quaternion.identity);
                        }
                        spawned = true;
                    } catch (System.Exception e) {
                        SpawnConfig.Logger.LogWarning($"Method 3 failed: {e.Message}");
                    }
                }
                
                if (spawned) {
                    SpawnConfig.Logger.LogInfo($"Manually spawned enemy group: {groupName}");
                    return true;
                } else {
                    SpawnConfig.Logger.LogError("All spawn methods failed");
                    return false;
                }
            }
            catch (System.Exception e) {
                SpawnConfig.Logger.LogError($"Error spawning enemy group: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        private static Vector3 GetPlayerPosition() {
            PlayerController player = GameObject.FindObjectOfType<PlayerController>();
            if (player != null && player.transform != null) {
                return player.transform.position;
            }
            
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null) {
                return playerObj.transform.position;
            }
            
            return Vector3.zero;
        }
        
        public static bool DisableAllEnemyGroups() {
            if (currentEnemyDirector == null && !TryFindEnemyDirector()) {
                SpawnConfig.Logger.LogWarning("No EnemyDirector found, but still updating group weights");
            }
            
            foreach (var group in extendedSetups.Values) {
                group.difficulty1Weight = 0;
                group.difficulty2Weight = 0;
                group.difficulty3Weight = 0;
            }
            
            SpawnConfig.Logger.LogInfo("All enemy groups disabled");
            return true;
        }
        
        public static bool EnableAllEnemyGroups(float baseWeight = 1.0f) {
            if (currentEnemyDirector == null && !TryFindEnemyDirector()) {
                SpawnConfig.Logger.LogWarning("No EnemyDirector found, but still updating group weights");
            }
            
            foreach (var group in extendedSetups.Values) {
                group.difficulty1Weight = baseWeight;
                group.difficulty2Weight = baseWeight;
                group.difficulty3Weight = baseWeight;
            }
            
            SpawnConfig.Logger.LogInfo("All enemy groups enabled with weight: " + baseWeight);
            return true;
        }
        
        public static bool SetEnemyGroupWeight(string groupName, float weight) {
            if (!extendedSetups.ContainsKey(groupName)) {
                SpawnConfig.Logger.LogError($"Enemy group '{groupName}' not found");
                return false;
            }
            
            var group = extendedSetups[groupName];
            group.difficulty1Weight = weight;
            group.difficulty2Weight = weight;
            group.difficulty3Weight = weight;
            
            SpawnConfig.Logger.LogInfo($"Set weight of '{groupName}' to {weight}");
            return true;
        }
    }
}