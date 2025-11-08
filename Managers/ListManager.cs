using System.Collections.Generic;
using SpawnConfig.ExtendedClasses;

namespace SpawnConfig;

public class ListManager {
    public static Dictionary<string, EnemySetup> enemySetupsDict = [];
    public static Dictionary<string, PrefabRef> spawnObjectsDict = [];
    public static Dictionary<string, ExtendedEnemySetup> extendedSetups = [];
    public static Dictionary<string, ExtendedSpawnObject> extendedSpawnObjects = [];
    public static Dictionary<int, ExtendedGroupCounts> extendedGroupCounts = [];
    public static List<List<string>> previousSpawns = [];
    public static List<int> difficulty1Counts = [];
    public static List<int> difficulty2Counts = [];
    public static List<int> difficulty3Counts = [];
    public static List<string> levelNames = [];
    public static List<int> levelNumbers = [];

    public static double GetLevelNumMultiplier(int levelsAgo){

        double multiplier = 1.0;

        if (levelsAgo == 1) {
            multiplier = SpawnConfig.configManager.consecutiveWeightMultiplierMin.Value;
        } else if (levelsAgo == SpawnConfig.configManager.consecutiveLevelCount.Value) {
            multiplier = SpawnConfig.configManager.consecutiveWeightMultiplierMax.Value;
        } else {
            double step = (SpawnConfig.configManager.consecutiveWeightMultiplierMax.Value - SpawnConfig.configManager.consecutiveWeightMultiplierMin.Value) / (SpawnConfig.configManager.consecutiveLevelCount.Value - 1);
            multiplier = SpawnConfig.configManager.consecutiveWeightMultiplierMin.Value + (levelsAgo - 1) * step;
        }
        
        return multiplier;
    }
}