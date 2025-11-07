using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
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
    internal static readonly string spawnGroupsPath = Path.Combine(exportPath, "SpawnGroups.json");
    internal static readonly string spawnGroupsBackupPath = Path.Combine(exportPath, "SpawnGroups_Backup.json");
    internal static readonly string defaultSpawnGroupsPath = Path.Combine(exportPath, "Defaults", "SpawnGroups-Readonly.json");
    internal static readonly string groupsPerLevelPath = Path.Combine(exportPath, "GroupsPerLevel.json");
    internal static readonly string defaultGroupsPerLevelPath = Path.Combine(exportPath, "Defaults", "GroupsPerLevel-Readonly.json");

    internal static readonly string groupsPerLevelExplainedPath = Path.Combine(exportPath, "GroupsPerLevel-Explained.json");    // Legacy
    internal static readonly string spawnGroupsExplainedPath = Path.Combine(exportPath, "SpawnGroups-Explained.json");    // Legacy

    public static bool missingProperties = false;

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

        // Read custom EnemySetup configs
        List<ExtendedEnemySetup> customSetupsList = JsonManager.GetSpawnGroupsFromJSON(spawnGroupsPath);
        // Read custom group counts config
        List<ExtendedGroupCounts> customGroupCountsList = JsonManager.GetGroupCountsFromJSON(groupsPerLevelPath);

        // Save default group counts to file
        List<ExtendedGroupCounts> extendedGroupCountsList = extendedGroupCounts.Select(obj => obj.Value).ToList();
        File.WriteAllText(defaultGroupsPerLevelPath, JsonManager.GroupCountsToJSON(extendedGroupCountsList));
        if(customGroupCountsList.Count < 1){
            Logger.LogInfo("No custom group count config found! Creating default file");
            File.WriteAllText(groupsPerLevelPath, JsonManager.GroupCountsToJSON(extendedGroupCountsList));
            customGroupCountsList = extendedGroupCountsList;
        }
        if(customGroupCountsList[0].level != 1){
            Logger.LogError("Your custom group count config must contain at least a valid level 1 entry!");
            customGroupCountsList = extendedGroupCountsList;
        }

        // Save default ExtendedEnemySetups to file for comparison purposes on next launch
        List<ExtendedEnemySetup> extendedSetupsList = extendedSetups.Select(obj => obj.Value).ToList();
        File.WriteAllText(defaultSpawnGroupsPath, JsonManager.SpawnGroupsToJSON(extendedSetupsList));
        if (customSetupsList.Count < 1) {
            Logger.LogInfo("No custom spawn groups config found! Creating default file");
            File.WriteAllText(spawnGroupsPath, JsonManager.SpawnGroupsToJSON(extendedSetupsList));
            customSetupsList = extendedSetupsList;
        }

        // Update custom setups with the default values from the source code where necessary
        bool updatedFile = false;
        foreach(ExtendedEnemySetup custom in customSetupsList){
            bool changedSomething = custom.Update();
            if (changedSomething) updatedFile = true;
            /*if (extendedSetups.ContainsKey(custom.name)) {
                custom.UpdateWithDefaults(extendedSetupsList.Where(objTemp => objTemp.name == custom.name).FirstOrDefault());
            }*/
        }

        // Add missing enemies from source into the custom config
        Dictionary<string, ExtendedEnemySetup> tempDict = customSetupsList.ToDictionary(obj => obj.name);
        foreach (KeyValuePair<string, ExtendedEnemySetup> source in extendedSetups) {
            if(!tempDict.ContainsKey(source.Value.name) && configManager.addMissingGroups.Value){
                Logger.LogInfo("Missing group entry found: " + source.Value.name);
                tempDict.Add(source.Value.name, source.Value);
                updatedFile = true;
            }
        }
        customSetupsList = tempDict.Values.ToList();

        // Update the file if something was changed
        if (updatedFile || missingProperties) {
            // Save a backup of the current config
            Logger.LogInfo("-----------------------------------------------------------");
            Logger.LogInfo("Automatic changes have been made to SpawnConfig.json!");
            //File.Copy(spawnGroupsPath, spawnGroupsBackupPath);
            File.WriteAllText(spawnGroupsPath, JsonManager.SpawnGroupsToJSON(customSetupsList));
        }

        // Replace vanilla values
        extendedSetups = customSetupsList.ToDictionary(obj => obj.name);
        extendedGroupCounts = customGroupCountsList.ToDictionary(obj => obj.level);
        
        // Update formatting
        File.WriteAllText(groupsPerLevelPath, JsonManager.GroupCountsToJSON(customGroupCountsList));
        
        // Delete legacy files
        File.Delete(spawnGroupsExplainedPath);
        File.Delete(groupsPerLevelExplainedPath);
        
    }
}