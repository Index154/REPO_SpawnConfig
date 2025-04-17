using BepInEx.Configuration;
using UnityEngine;

namespace SpawnConfig;

public class ConfigManager {
    internal ConfigEntry<bool> preventSpawns = null!;
    internal ConfigEntry<bool> addMissingGroups = null!;
    internal ConfigEntry<double> repeatMultiplier = null!;
    internal ConfigEntry<bool> ignoreInvalidGroups = null!;
    internal ConfigEntry<KeyCode> menuToggleKey = null!;
    internal ConfigEntry<KeyCode> quickSpawnKey = null!;
    internal ConfigEntry<double> globalSpawnMultiplier = null!;
    
    internal void Setup(ConfigFile configFile) {
        preventSpawns = configFile.Bind("General", "Prevent enemy spawning", false, new ConfigDescription("Prevent enemy spawning entirely, turning the game into a no-stakes gathering simulator or for when you want to test something in peace"));

        addMissingGroups = configFile.Bind("General", "Re-add missing groups", true, new ConfigDescription("Whether the mod should update your custom SpawnGroups config at launch by adding all loaded enemy groups that are missing from it"));

        repeatMultiplier = configFile.Bind("General", "Repeat spawn weight multiplier", 0.5, new ConfigDescription("All three weights of an enemy group will be multiplied by this value for the current level after having been selected once. Effectively reduces the chance of encountering multiple copies of the same group in one level. Set to 1.0 to \"disable\""));

        ignoreInvalidGroups = configFile.Bind("General", "Ignore groups with invalid spawnObjects", true, new ConfigDescription("If set to true, any group containing a single invalid spawn object will be ignored completely. If set to false, only the individual spawn object will be ignored and the group can still spawn as long as it contains at least one valid enemy"));

        menuToggleKey = configFile.Bind("Interface", "Menu toggle key", KeyCode.F8, new ConfigDescription("Key to toggle the in-game configuration menu"));
        
        quickSpawnKey = configFile.Bind("Interface", "Quick spawn key", KeyCode.F9, new ConfigDescription("Key to quickly spawn the currently selected enemy group"));
        
        globalSpawnMultiplier = configFile.Bind("General", "Global spawn multiplier", 1.0, new ConfigDescription("Multiplies the overall spawn rate of all enemies. Higher values = more enemies, lower values = fewer enemies", new AcceptableValueRange<double>(0.1, 10.0)));
    }
}