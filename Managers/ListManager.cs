using System.Collections.Generic;
using SpawnConfig.ExtendedClasses;
using UnityEngine;

namespace SpawnConfig;

public class ListManager {
    public static Dictionary<string, EnemySetup> enemySetupsDict = [];
    public static Dictionary<string, GameObject> spawnObjectsDict = [];
    public static Dictionary<string, ExtendedEnemySetup> extendedSetups = [];
    public static Dictionary<string, ExtendedSpawnObject> extendedSpawnObjects = [];
    public static Dictionary<int, ExtendedGroupCounts> extendedGroupCounts = [];
    public static List<int> difficulty1Counts = [];
    public static List<int> difficulty2Counts = [];
    public static List<int> difficulty3Counts = [];
    public static List<int> levelNumbers = [];
}