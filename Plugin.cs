using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using SpawnConfig.ExtendedClasses;
using static SpawnConfig.ListManager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpawnConfig;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class SpawnConfig : BaseUnityPlugin
{
    public static SpawnConfig Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static ConfigManager configManager = null!;
    internal static ConfigFile Conf = null!;
    internal static readonly string configVersion = "1.0";
    internal static readonly string exportPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    internal static readonly string spawnGroupsCfg = Path.Combine(exportPath, "SpawnGroups.json");
    internal static readonly string explanationCfg = Path.Combine(exportPath, "SpawnGroups-Explained.json");
    internal static readonly string defaultSpawnGroupsCfg = Path.Combine(exportPath, "Defaults", "SpawnGroups.json");
    internal static readonly string groupsPerLevelCfg = Path.Combine(exportPath, "GroupsPerLevel.json");
    internal static readonly string defaultGroupsPerLevelCfg = Path.Combine(exportPath, "Defaults", "GroupsPerLevel.json");

    private void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        configManager = new ConfigManager();
        Conf = Config;
        configManager.Setup(Config);
        Directory.CreateDirectory(exportPath);
        Directory.CreateDirectory(Path.Combine(exportPath, "Defaults"));

        Patch();

        InitializeUI();

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has loaded!");
    }

    private void InitializeUI()
    {
        try {
            GameObject existingUI = GameObject.Find("SpawnConfigUI");
            if (existingUI != null) {
                Logger.LogInfo("Destroying existing UI GameObject");
                Destroy(existingUI);
            }
            
            GameObject uiGo = new GameObject("SpawnConfigUI");
            Logger.LogInfo("Creating UI GameObject");
            
            var uiComponent = uiGo.AddComponent<Managers.SpawnConfigUI>();
            Logger.LogInfo("UI Component added successfully");
            
            DontDestroyOnLoad(uiGo);
            Logger.LogInfo("UI initialized and set to persist between scenes");
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            
        } catch (Exception ex) {
            Logger.LogError($"Error initializing UI: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
    {
        Logger.LogInfo($"Scene loaded: {scene.name}. Checking UI...");
        
        GameObject uiGo = GameObject.Find("SpawnConfigUI");
        if (uiGo == null) {
            Logger.LogWarning("UI GameObject was lost on scene change. Recreating...");
            InitializeUI();
        } else {
            Logger.LogInfo("UI GameObject still exists after scene change.");
        }
    }

    internal static void Patch()
    {
        Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

        Logger.LogDebug("Patching...");

        Harmony.PatchAll();

        Logger.LogDebug("Finished patching!");
    }

    internal static void Unpatch()
    {
        Logger.LogDebug("Unpatching...");

        Harmony?.UnpatchSelf();

        Logger.LogDebug("Finished unpatching!");
    }

    public static void ReadAndUpdateJSON(){
        List<ExtendedEnemyExplained> explained = [new ExtendedEnemyExplained()];
        File.WriteAllText(explanationCfg, JsonConvert.SerializeObject(explained, Formatting.Indented));

        List<ExtendedEnemySetup> customSetupsList = JsonManager.GetEESListFromJSON(spawnGroupsCfg);
        List<ExtendedGroupCounts> customGroupCounts = JsonManager.GetEGCListFromJSON(groupsPerLevelCfg);

        bool stopEarly = false;

        List<ExtendedEnemySetup> extendedSetupsList = extendedSetups.Select(obj => obj.Value).ToList();
        File.WriteAllText(defaultSpawnGroupsCfg, JsonManager.EESToJSON(extendedSetupsList));
        if (customSetupsList.Count < 1) {
            Logger.LogInfo("No custom spawn groups config found! Creating default file");
            File.WriteAllText(spawnGroupsCfg, JsonManager.EESToJSON(extendedSetupsList));
            stopEarly = true;
        }

        if (stopEarly) return;

        foreach(ExtendedEnemySetup custom in customSetupsList){
            custom.Update();
            if (extendedSetups.ContainsKey(custom.name)) {
            }
        }

        Dictionary<string, ExtendedEnemySetup> tempDict = customSetupsList.ToDictionary(obj => obj.name);
        foreach (KeyValuePair<string, ExtendedEnemySetup> source in extendedSetups) {
            if(!tempDict.ContainsKey(source.Value.name) && configManager.addMissingGroups.Value){
                Logger.LogInfo("Adding missing entry to custom config: " + source.Value.name);
                tempDict.Add(source.Value.name, source.Value);
            }
        }
        customSetupsList = tempDict.Values.ToList();

        File.WriteAllText(spawnGroupsCfg, JsonManager.EESToJSON(customSetupsList));

        extendedSetups = customSetupsList.ToDictionary(obj => obj.name);
    }
}