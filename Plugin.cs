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

namespace SpawnConfig;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class SpawnConfig : BaseUnityPlugin
{
    public static SpawnConfig Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger { get; private set; } = null!;
    internal static Harmony? Harmony { get; set; }
    internal static ConfigManager configManager = null!;
    internal static ConfigFile Conf = null!;
    internal static readonly string configVersion = "1.0";
    internal static readonly string exportPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
    //internal static readonly string spawnObjectsCfg = Path.Combine(exportPath, "Enemies.json");
    //internal static readonly string defaultSpawnObjectsCfg = Path.Combine(exportPath, "Defaults", "Enemies.json");
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

        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} has loaded!");
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

        // Save config explanation file
        List<ExtendedEnemyExplained> explained = [new ExtendedEnemyExplained()];
        File.WriteAllText(explanationCfg, JsonConvert.SerializeObject(explained, Formatting.Indented));

        // Read custom EnemySetup configs
        List<ExtendedEnemySetup> customSetupsList = JsonManager.GetEESListFromJSON(spawnGroupsCfg);
        // Read custom group counts config
        List<ExtendedGroupCounts> customGroupCounts = JsonManager.GetEGCListFromJSON(groupsPerLevelCfg);

        // Save default group counts to file
        bool stopEarly = false;
        /*
        File.WriteAllText(defaultGroupsPerLevelCfg, JsonManager.GroupCountsToJSON(groupCountsList));
        if(customGroupCounts.Count < 1){
            Logger.LogInfo("No custom group count config found! Creating default file");
            File.WriteAllText(groupsPerLevelCfg, JsonManager.GroupCountsToJSON(groupCountsList));
            stopEarly = true;
        }
        */

        // Save default ExtendedEnemySetups to file for comparison purposes on next launch
        List<ExtendedEnemySetup> extendedSetupsList = extendedSetups.Select(obj => obj.Value).ToList();
        File.WriteAllText(defaultSpawnGroupsCfg, JsonManager.EESToJSON(extendedSetupsList));
        if (customSetupsList.Count < 1) {
            Logger.LogInfo("No custom spawn groups config found! Creating default file");
            File.WriteAllText(spawnGroupsCfg, JsonManager.EESToJSON(extendedSetupsList));
            stopEarly = true;
        }

        // Stop early check
        if (stopEarly) return;

        // Update custom setups with the default values from the source code where necessary
        foreach(ExtendedEnemySetup custom in customSetupsList){
            custom.Update();
            if (extendedSetups.ContainsKey(custom.name)) {
                //custom.UpdateWithDefaults(extendedSetupsList.Where(objTemp => objTemp.name == custom.name).FirstOrDefault());
            }
        }
        

        // Add missing enemies from source into the custom config
        Dictionary<string, ExtendedEnemySetup> tempDict = customSetupsList.ToDictionary(obj => obj.name);
        foreach (KeyValuePair<string, ExtendedEnemySetup> source in extendedSetups) {
            if(!tempDict.ContainsKey(source.Value.name) && configManager.addMissingGroups.Value){
                Logger.LogInfo("Adding missing entry to custom config: " + source.Value.name);
                tempDict.Add(source.Value.name, source.Value);
            }
        }
        customSetupsList = tempDict.Values.ToList();

        // Save custom setups with new updated default values to file
        File.WriteAllText(spawnGroupsCfg, JsonManager.EESToJSON(customSetupsList));

        // Replace vanilla extended setups with the custom ones so that the custom changes take effect ingame
        extendedSetups = customSetupsList.ToDictionary(obj => obj.name);
        
    }
}